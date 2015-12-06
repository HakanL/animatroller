using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;
using Expander = Animatroller.Framework.Expander;
using Controller = Animatroller.Framework.Controller;
using Physical = Animatroller.Framework.PhysicalDevice;
using System.Reactive.Subjects;

namespace Animatroller.SceneRunner
{
    internal class Xmas2015 : BaseScene
    {
        const int SacnUniverseDMX = 1;
        const int SacnUniverseRenardBig = 20;
        const int SacnUniverseRenardSmall = 21;
        const int midiChannel = 0;

        public enum States
        {
            Background,
            Music1,
            Music2,
            DarthVader
        }

        OperatingHours2 hours = new OperatingHours2();
        Controller.EnumStateMachine<States> stateMachine = new Controller.EnumStateMachine<States>();

        Expander.MonoExpanderInstance expanderLocal = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expander1 = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderInstance expander2 = new Expander.MonoExpanderInstance();
        Expander.MonoExpanderServer expanderServer = new Expander.MonoExpanderServer(8088);
        AudioPlayer audio1 = new AudioPlayer();
        AudioPlayer audio2 = new AudioPlayer();

        Expander.AcnStream acnOutput = new Expander.AcnStream(priority: 150);

        DigitalInput2 inOlaf = new DigitalInput2();

        DigitalInput2 in1 = new DigitalInput2();
        DigitalOutput2 out1 = new DigitalOutput2();

        DigitalOutput2 laser = new DigitalOutput2();
        DigitalOutput2 airR2D2 = new DigitalOutput2();
        DigitalOutput2 airOlaf = new DigitalOutput2();
        DigitalOutput2 airReindeer = new DigitalOutput2();

        Dimmer3 lightOlaf = new Dimmer3();
        VirtualPixel1D2 pixelsRoofEdge = new VirtualPixel1D2(150);
        VirtualPixel1D2 pixelsMatrix = new VirtualPixel1D2(200);
        Expander.MidiInput2 midiAkai = new Expander.MidiInput2("LPD8", true);
        Subject<bool> inflatablesRunning = new Subject<bool>();
        AnalogInput3 blackOut = new AnalogInput3();
        AnalogInput3 whiteOut = new AnalogInput3();
        DigitalInput2 buttonStartInflatables = new DigitalInput2();

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        private DigitalInput2 buttonOverrideHours = new DigitalInput2(persistState: true);

        Controller.Subroutine subOlaf = new Controller.Subroutine();

        public Xmas2015(IEnumerable<string> args)
        {
            hours.AddRange("5:00 pm", "10:00 pm");

            string expFilesParam = args.FirstOrDefault(x => x.StartsWith("EXPFILES"));
            if (!string.IsNullOrEmpty(expFilesParam))
            {
                string[] parts = expFilesParam.Split('=');
                if (parts.Length == 2)
                    expanderServer.ExpanderSharedFiles = parts[1];
            }

            expanderServer.AddInstance("ec30b8eda95b4c5cab46bf630d74810e", expanderLocal);
            expanderServer.AddInstance("ed86c3dc166f41ee86626897ba039ed2", expander1);
            expanderServer.AddInstance("10520fdcf14d47cba31da8b6e05d01d8", expander2);

            expander1.DigitalInputs[4].Connect(inOlaf);
            expander1.DigitalInputs[6].Connect(in1);
            expander1.DigitalOutputs[7].Connect(out1);
            expander1.Connect(audio1);
            expander2.Connect(audio2);

            blackOut.ConnectTo(Exec.Blackout);
            whiteOut.ConnectTo(Exec.Whiteout);

            midiAkai.Controller(midiChannel, 1).Subscribe(x => blackOut.Value = x.Value);
            midiAkai.Controller(midiChannel, 2).Subscribe(x => whiteOut.Value = x.Value);

            buttonOverrideHours.Output.Subscribe(x =>
            {
                if (x)
                    hours.SetForced(true);
                else
                    hours.SetForced(false);
            });

            inflatablesRunning.Subscribe(x =>
            {
                airReindeer.Value = x;

                Exec.SetKey("InflatablesRunning", x.ToString());
            });

            // Read from storage
            inflatablesRunning.OnNext(Exec.GetSetKey("InflatablesRunning", false));

            //            hours.Output.Log("Hours inside");

            airR2D2.Value = true;
            airOlaf.Value = true;
            laser.Value = true;

            hours
            //    .ControlsMasterPower(packages)
            //    .ControlsMasterPower(airSnowman)
                .ControlsMasterPower(airOlaf)
                .ControlsMasterPower(laser)
                .ControlsMasterPower(airR2D2);
            //    .ControlsMasterPower(airSanta);

            buttonStartInflatables.Output.Subscribe(x =>
            {
                if (x && hours.IsOpen)
                {
                    inflatablesRunning.OnNext(true);
                }
            });

            acnOutput.Connect(new Physical.PixelRope(pixelsRoofEdge, 0, 50), 6, 1);
            acnOutput.Connect(new Physical.PixelRope(pixelsRoofEdge, 50, 100), 5, 1);

            acnOutput.Connect(new Physical.PixelRope(pixelsMatrix, 0, 170), 10, 1);
            acnOutput.Connect(new Physical.PixelRope(pixelsMatrix, 170, 30), 11, 1);

            acnOutput.Connect(new Physical.GenericDimmer(airOlaf, 10), SacnUniverseDMX);
            acnOutput.Connect(new Physical.GenericDimmer(airReindeer, 12), SacnUniverseDMX);
            acnOutput.Connect(new Physical.GenericDimmer(airR2D2, 11), SacnUniverseDMX);
            acnOutput.Connect(new Physical.GenericDimmer(laser, 4), SacnUniverseRenardBig);

            acnOutput.Connect(new Physical.GenericDimmer(lightOlaf, 128), SacnUniverseDMX);

            hours.Output.Subscribe(x =>
            {
                if (x)
                {
                    stateMachine.SetBackgroundState(States.Background);
                    stateMachine.SetState(States.Background);
                    //lightTreeUp.SetColor(Color.Red, 1.0);
                    //lightSnow1.SetBrightness(1.0);
                    //lightSnow2.SetBrightness(1.0);
                }
                else
                {
                    //if (buttonOverrideHours.Active)
                    //    return;

                    stateMachine.Hold();
                    stateMachine.SetBackgroundState(null);
                    //EverythingOff();
                    System.Threading.Thread.Sleep(200);
                    /*
                                        airR2D2.Power = false;
                                        airSanta.Power = false;
                                        airSnowman.Power = false;
                                        airReindeer.Power = false;*/

                    //switchDeerHuge.TurnOff();
                    //switchSanta.TurnOff();
                    inflatablesRunning.OnNext(false);
                }
            });

            subOlaf
                .LockWhenRunning(lightOlaf)
                .RunAction(i =>
                {
                    lightOlaf.SetBrightness(1.0, i.Token);
                    audio1.PlayEffect("WarmHugs.wav");
                    i.WaitFor(S(10));
                });



            midiAkai.Note(midiChannel, 36).Subscribe(x =>
            {
                if (x)
                    subOlaf.Run();
                //                lightOlaf.SetBrightness(x ? 1.0 : 0.0);
            });

            midiAkai.Note(midiChannel, 37).Subscribe(x =>
            {
                if (x)
                {
                    airR2D2.Value = !airR2D2.Value;
                }
            });

            inOlaf.Output.Subscribe(x =>
            {
                if (x && hours.IsOpen)
                    subOlaf.Run();
            });

            in1.Output.Subscribe(x =>
            {
                if (x)
                    //                    audio2.PlayTrack("02. Frozen - Do You Want to Build a Snowman.wav");
                    audio1.PlayEffect("WarmHugs.wav");
                //                    audio2.PlayTrack("08 Feel the Light.wav");
                //                    audioLocal.PlayEffect("WarmHugs.wav");

                out1.Value = x;
            });
        }

        public override void Run()
        {
        }

        public override void Stop()
        {
        }
    }
}
