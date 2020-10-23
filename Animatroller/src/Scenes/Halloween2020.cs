using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using Animatroller.Framework;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;
using Animatroller.Framework.Simulator;
using Controller = Animatroller.Framework.Controller;
using Effect = Animatroller.Framework.Effect;
using Expander = Animatroller.Framework.Expander;
using Physical = Animatroller.Framework.PhysicalDevice;

namespace Animatroller.Scenes
{
    internal partial class Halloween2020 : BaseScene
    {
        const int SacnUniverseDMXFogA = 55;
        //const int SacnUniverseEdmx4A = 20;
        //const int SacnUniverseEdmx4B = 21;
        //const int SacnUniverseDMXCat = 4;
        const int SacnUniverseDMXLedmx = 10;
        const int SacnUniversePixel100 = 5;
        const int SacnUniversePixel50 = 6;
        const int SacnUniverseFrankGhost = 3;
        //const int SacnUniverseFire = 99;

        // E6804 12V - 192.168.240.247

        public enum States
        {
            Background,
            Setup
        }

        const int midiChannel = 0;

        Controller.EnumStateMachine<States> stateMachine = new Controller.EnumStateMachine<States>();
        Expander.MidiInput2 midiInput = new Expander.MidiInput2("LPD8", ignoreMissingDevice: true);
        Expander.OscServer oscServer = new Expander.OscServer(8000, forcedClientPort: 8000, registerAutoHandlers: true);
        AudioPlayer audioSpider = new AudioPlayer();
        AudioPlayer audioFrankGhost = new AudioPlayer();
        Expander.MonoExpanderServer expanderServer = new Expander.MonoExpanderServer(listenPort: 8899);
        Expander.MonoExpanderInstance expanderLedmx = new Expander.MonoExpanderInstance(hardware: Expander.MonoExpanderInstance.HardwareType.PiFace);
        Expander.MonoExpanderInstance expanderBigEye = new Expander.MonoExpanderInstance(hardware: Expander.MonoExpanderInstance.HardwareType.None);
        Expander.AcnStream acnOutput = new Expander.AcnStream();
        Expander.OscClient bigEyeSender = new Expander.OscClient("192.168.240.155", 8000); // rpi-ebc64d15

        VirtualPixel1D3 pixelsFrankGhost = new VirtualPixel1D3(5);
        DigitalOutput2 frankGhostAir = new DigitalOutput2();
        Modules.HalloweenFrankGhost frankGhost;
        Modules.HalloweenSpider spider;

        AnalogInput3 masterVolume = new AnalogInput3(persistState: true, defaultValue: 1.0);

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 buttonOverrideHours = new DigitalInput2(persistState: true);

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 testButton1 = new DigitalInput2();
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 testButton2 = new DigitalInput2();
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 testButton3 = new DigitalInput2();
        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 testButton4 = new DigitalInput2();

        [SimulatorButtonType(SimulatorButtonTypes.FlipFlop)]
        DigitalInput2 setupMode = new DigitalInput2(persistState: true);

        DigitalInput2 spiderMotion = new DigitalInput2();

        Effect.Flicker flickerEffect = new Effect.Flicker(0.4, 0.6, false);
        Effect.Pulsating pulsatingEffect1 = new Effect.Pulsating(S(2), 0.1, 1.0, false);
        Effect.Pulsating pulsatingGargoyle = new Effect.Pulsating(S(4), 0.1, 0.8, false);
        Effect.Pulsating pulsatingEffect2 = new Effect.Pulsating(S(2), 0.4, 1.0, false);
        Effect.PopOut2 popOut1 = new Effect.PopOut2(S(0.3));
        Effect.PopOut2 popOut2 = new Effect.PopOut2(S(0.3));
        Effect.PopOut2 popOutAll = new Effect.PopOut2(S(1.2));

        Dimmer3 bigSpiderEyes = new Dimmer3();

        OperatingHours2 mainSchedule = new OperatingHours2("Hours");

        StrobeDimmer3 flashUnderSpider = new StrobeDimmer3("Eliminator Flash");

        public Halloween2020(IEnumerable<string> args)
        {
            mainSchedule.AddRange("5:00 pm", "10:00 pm");
            mainSchedule.AddRange("6:30 am", "9:00 am");
            //    DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Sunday);
            //mainSchedule.AddRange("5:00 pm", "9:00 pm",
            //    DayOfWeek.Friday, DayOfWeek.Saturday);

            string expFilesParam = args.FirstOrDefault(x => x.StartsWith("EXPFILES"));
            if (!string.IsNullOrEmpty(expFilesParam))
            {
                string[] parts = expFilesParam.Split('=');
                if (parts.Length == 2)
                {
                    Exec.ExpanderSharedFiles = parts[1];
                }
            }
            //expanderServer.AddInstance("ec30b8eda95b4c5cab46bf630d74810e", expanderLocal);

            //expanderServer.AddInstance("4ea781ef257442edb524493da8f52220", expanderAudio2);     // rpi-eba6cbc7
            expanderServer.AddInstance("ed86c3dc166f41ee86626897ba039ed2", expanderLedmx);      // rpi-eb0092ca
            //expanderServer.AddInstance("1583f686014345888c15d7fc9c55ca3c", expanderSpider);        // rpi-eb81c94e
            //expanderServer.AddInstance("d6fc4e752af04022bf3c1a1166a557bb", expanderHifi);       // rpi-eb428ef1
            //expanderServer.AddInstance("60023fcde5b549b89fa828d31741dd0c", expanderPicture);    // rpi-eb91bc26
            //expanderServer.AddInstance("e41d2977931d4887a9417e8adcd87306", expanderRocking);    // rpi-eb6a047c
            //expanderServer.AddInstance("16e49fc1188e4310931a4e6d21b3e940", expanderBigEye);    // rpi-ebc64d15
            //expanderServer.AddInstance("999861affa294fd7bbf0601505e9ae09", expanderRocking); // rpi-ebd43a38
            //expanderServer.AddInstance("992f8db68e874248b5ee667d23d74ac3", expanderFlying);     // rpi-eb9b3145
            //expanderServer.AddInstance("db9b41a596cb4ed28e91f11a59afb95a", expanderHead);      // rpi-eb32e5f9
            //expanderServer.AddInstance("acbfada45c674077b9154f6a0e0df359", expanderMrPumpkin);     // rpi-eb35666e
            //expanderServer.AddInstance("2e105175a66549d4a0ab7f8d446c2e29", expanderPopper);     // rpi-eb997095
            //expanderServer.AddInstance("4fabc4931566424c870ccb83984b3ffb", expanderEeebox);     // videoplayer1

            masterVolume.ConnectTo(Exec.MasterVolume);

            pulsatingGargoyle.ConnectTo(bigSpiderEyes);

            frankGhost = new Modules.HalloweenFrankGhost(
                air: frankGhostAir,
                light: pixelsFrankGhost,
                audioPlayer: audioFrankGhost,
                name: nameof(frankGhost));
            stateMachine.WhenStates(States.Background).Controls(frankGhost.InputPower);

            spider = new Modules.HalloweenSpider(
                spiderEyes: bigSpiderEyes,
                strobeLight: flashUnderSpider,
                audioPlayer: audioSpider,
                name: nameof(spider));
            stateMachine.WhenStates(States.Background).Controls(spider.InputPower);

            spiderMotion.Controls(spider.InputTrigger);

            buttonOverrideHours.Output.Subscribe(x =>
            {
                if (x)
                    mainSchedule.SetForced(true);
                else
                    mainSchedule.SetForced(null);
            });


            testButton1.Output.Subscribe(x =>
            {
            });

            testButton2.Output.Subscribe(x =>
            {
            });

            testButton3.Output.Subscribe(x =>
            {
            });

            testButton4.Output.Subscribe(x =>
            {
            });

            setupMode.Output.Subscribe(x =>
            {
                if (x)
                    stateMachine.GoToState(States.Setup);
                else
                    stateMachine.GoToDefaultState();
            });

            mainSchedule.Output.Subscribe(x =>
            {
                if (x)
                {
                    stateMachine.SetDefaultState(States.Background);
                    stateMachine.GoToDefaultState();
                }
                else
                {
                    stateMachine.GoToIdle();
                    stateMachine.SetDefaultState(null);
                }
            });

            stateMachine.For(States.Setup)
                .Controls(1, flickerEffect, pulsatingGargoyle, pulsatingEffect1)
                .Execute(ins =>
                {
                    ins.WaitUntilCancel();
                });

            stateMachine.For(States.Background)
                .Controls(1, flickerEffect, pulsatingGargoyle, pulsatingEffect1)
                .SetUp(ins =>
                {
                })
                .Execute(ins =>
                    {
                        bigEyeSender.SendAndRepeat("/eyecontrol", 1);

                        ins.WaitUntilCancel();
                    })
                .TearDown(ins =>
                    {
                        bigEyeSender.SendAndRepeat("/eyecontrol", 0);
                    });

            acnOutput.Connect(new Physical.EliminatorFlash192(flashUnderSpider, 110), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(frankGhostAir, 10), SacnUniverseDMXLedmx);
            acnOutput.Connect(new Physical.GenericDimmer(bigSpiderEyes, 256), SacnUniverseDMXLedmx);

            expanderLedmx.DigitalInputs[4].Connect(spiderMotion);

            //expanderLedmx.Connect(audioCat);
            expanderLedmx.Connect(audioSpider, 1);
        }

        public override void Run()
        {
        }

        public override void Stop()
        {
        }
    }
}
