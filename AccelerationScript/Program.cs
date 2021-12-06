using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
  partial class Program : MyGridProgram
  {
    /*
     * [settings]
     * Acceleration=0.1
     * MaxSpeed=5
     */
    float acceleration;
    float maxVelocity;
    MyIni ini = new MyIni();
    IMyMotorStator motor;
    float totalAngle = 0;
    int timer = 0;
    int direction;
    bool wait = false;
    bool underHalf = false;
    bool running;
    IMyTextSurface lcd;

    public Program()
    {
      motor = GridTerminalSystem.GetBlockWithName("Rotor") as IMyMotorStator;
      lcd = Me.GetSurface(0);
      lcd.ContentType = ContentType.TEXT_AND_IMAGE;

      MyIniParseResult result;
      if (!ini.TryParse(Me.CustomData, out result))
        throw new Exception(result.ToString());

      if (ini.ContainsSection("settings"))
      {
        acceleration = ini.Get("settings", "Acceleration").ToSingle();
        maxVelocity = ini.Get("settings", "MaxSpeed").ToSingle();
      }
      else
      {
        Me.CustomData = 
          "; Recompile after changing settings!\n" +
          "[settings]\n" +
          "Acceleration=0.02\n" +
          "MaxSpeed=5";
      }
      Log($"Acceleration: {acceleration} Max speed: {maxVelocity}\n");
    }

    public void Save()
    {
    }

    public void Main(string argument, UpdateType updateSource)
    {
      if (updateSource != UpdateType.Update1)
      {
        if (!running)
        {
          totalAngle = motor.UpperLimitDeg - motor.LowerLimitDeg;
          Runtime.UpdateFrequency = UpdateFrequency.Update1;
          motor.TargetVelocityRPM = 0;
          timer = 0;
          wait = false;
          underHalf = true;
          running = true;

          if ((int)Math.Round(RadToDeg(motor.Angle)) == (int)Math.Round(motor.LowerLimitDeg))
            direction = 1;
          else if ((int)Math.Round(RadToDeg(motor.Angle)) == (int)Math.Round(motor.UpperLimitDeg))
            direction = -1;
          else
          {
            direction = -1;
            totalAngle = RadToDeg(motor.Angle);
          }
        }
      }
      else
      {
        if (direction == 1)
        {
          if (RadToDeg(motor.Angle) < totalAngle / 2)
            underHalf = true;
          else
            underHalf = false;
        }
        else
        {
          if (RadToDeg(motor.Angle) > totalAngle / 2)
            underHalf = true;
          else
            underHalf = false;
        }

        if (underHalf)
        {
          if (Math.Abs(motor.TargetVelocityRPM) < Math.Abs(maxVelocity))
          {
            motor.TargetVelocityRPM += acceleration * direction;
          }
          else
          {
            timer++;
            wait = true;
          }
        }
        else
        {
          if (wait)
          {
            if (timer == 0)
              wait = false;
            else
              timer--;
          }
          else
          {
            motor.TargetVelocityRPM -= acceleration * direction;
          }

          if ((int)Math.Floor(RadToDeg(motor.Angle)) == (int)Math.Round(motor.UpperLimitDeg) ||
              (int)Math.Ceiling(RadToDeg(motor.Angle)) == (int)Math.Round(motor.LowerLimitDeg))
          {
            Runtime.UpdateFrequency = UpdateFrequency.None;
            motor.TargetVelocityRPM = 0;
            running = false;
          }
        }
        Log($"Acceleration: {acceleration} Max speed: {maxVelocity}\n" +
            $"Direction: {direction} Under halfway: {underHalf}\n" +
            $"Rotor speed: {motor.TargetVelocityRPM}");
      }
    }

    public float RadToDeg(float Rad)
    {
      return Rad * 57.2957795131f;
    }

    public void Log(string l)
    {
      lcd.WriteText(l);
    }
  }
}
