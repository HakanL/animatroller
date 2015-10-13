using System;
using System.Drawing;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using Animatroller.Framework;
using Animatroller.Framework.Extensions;
using Expander = Animatroller.Framework.Expander;
using Controller = Animatroller.Framework.Controller;
using Animatroller.Framework.LogicalDevice;
using Effect = Animatroller.Framework.Effect;
using Effect2 = Animatroller.Framework.Effect2;
using Physical = Animatroller.Framework.PhysicalDevice;

namespace Animatroller.SceneRunner
{
    internal class Halloween2015Manual : BaseScene
    {
        private const int midiChannel = 0;

        private Expander.MidiInput2 midiInput = new Expander.MidiInput2("LPD8");
        private Expander.OscServer oscServer = new Expander.OscServer();
        private AudioPlayer audioCat = new AudioPlayer();
        private AudioPlayer audioMain = new AudioPlayer();
        private AudioPlayer audioPop = new AudioPlayer();
        private AudioPlayer audioDIN = new AudioPlayer();
        private VideoPlayer video3dfx = new VideoPlayer();
        private VideoPlayer video2 = new VideoPlayer();
        private Expander.Raspberry raspberryCat = new Expander.Raspberry("192.168.240.115:5005", 3333);
        private Expander.Raspberry raspberry3dfx = new Expander.Raspberry("192.168.240.226:5005", 3334);
        private Expander.Raspberry raspberryLocal = new Expander.Raspberry("127.0.0.1:5005", 3339);
        private Expander.Raspberry raspberryPop = new Expander.Raspberry("192.168.240.123:5005", 3335);
        private Expander.Raspberry raspberryDIN = new Expander.Raspberry("192.168.240.127:5005", 3337);
        private Expander.Raspberry raspberryVideo2 = new Expander.Raspberry("192.168.240.124:5005", 3336);
        private Expander.OscClient touchOSC = new Expander.OscClient("192.168.240.163", 9000);

        private DigitalOutput2 spiderCeiling = new DigitalOutput2("Spider Ceiling");
        private DigitalOutput2 spiderCeilingDrop = new DigitalOutput2("Spider Ceiling Drop");
        private DigitalInput2 catMotion = new DigitalInput2();
        private DigitalInput2 firstBeam = new DigitalInput2();
        private DigitalInput2 finalBeam = new DigitalInput2();
        private DigitalInput2 motion2 = new DigitalInput2();
        private Expander.AcnStream acnOutput = new Expander.AcnStream();
        private DigitalOutput2 catAir = new DigitalOutput2(/*initial: true*/);
        private DigitalOutput2 fog = new DigitalOutput2();
        private DigitalOutput2 catLights = new DigitalOutput2();
        private DigitalOutput2 george1 = new DigitalOutput2();
        private DigitalOutput2 george2 = new DigitalOutput2();
        private DigitalOutput2 popper = new DigitalOutput2();
        private DigitalOutput2 dropSpiderEyes = new DigitalOutput2();

        private OperatingHours2 hoursSmall = new OperatingHours2("Hours Small");

        private Controller.Sequence catSeq = new Controller.Sequence();

        public Halloween2015Manual(IEnumerable<string> args)
        {
            hoursSmall.AddRange("5:00 pm", "9:00 pm");

            // Logging
            hoursSmall.Output.Log("Hours small");

            hoursSmall
                .ControlsMasterPower(catAir);
            //                .ControlsMasterPower(eyes);

            raspberryCat.DigitalInputs[4].Connect(catMotion, false);
            raspberryCat.DigitalInputs[5].Connect(firstBeam, false);
            raspberryCat.DigitalInputs[6].Connect(finalBeam, false);
            raspberryCat.DigitalOutputs[7].Connect(spiderCeilingDrop);
            raspberryCat.Connect(audioCat);
            raspberryLocal.Connect(audioMain);
            raspberry3dfx.Connect(video3dfx);
            raspberryVideo2.Connect(video2);
            raspberryPop.Connect(audioPop);
            raspberryDIN.Connect(audioDIN);
            raspberryDIN.DigitalInputs[7].Connect(motion2);
            raspberryDIN.DigitalOutputs[0].Connect(fog);
            raspberryPop.DigitalOutputs[7].Connect(george1);
            raspberryPop.DigitalOutputs[6].Connect(george2);
            raspberryPop.DigitalOutputs[5].Connect(popper);
            raspberryPop.DigitalOutputs[2].Connect(dropSpiderEyes);

            acnOutput.Connect(new Physical.GenericDimmer(catAir, 10), 1);
            acnOutput.Connect(new Physical.GenericDimmer(catLights, 11), 1);

            oscServer.RegisterAction<int>("/1/push1", d => d.First() != 0, (msg, data) =>
            {
                audioCat.PlayEffect("266 Monster Growl 7", 1.0, 1.0);
            });

            oscServer.RegisterAction<int>("/1/push2", d => d.First() != 0, (msg, data) =>
            {
                audioCat.PlayEffect("285 Monster Snarl 2", 1.0, 1.0);
            });

            oscServer.RegisterAction<int>("/1/push3", d => d.First() != 0, (msg, data) =>
            {
                audioCat.PlayEffect("286 Monster Snarl 3", 1.0, 1.0);
            });

            oscServer.RegisterAction<int>("/1/push4", d => d.First() != 0, (msg, data) =>
            {
                audioCat.PlayEffect("287 Monster Snarl 4", 1.0, 1.0);
            });


            oscServer.RegisterAction<int>("/1/push5", (msg, data) =>
            {
                george1.Value = data.First() != 0;
            });

            oscServer.RegisterAction<int>("/1/push6", (msg, data) =>
            {
                george2.Value = data.First() != 0;
            });

            oscServer.RegisterAction<int>("/1/push7", (msg, data) =>
            {
                popper.Value = data.First() != 0;
            });

            oscServer.RegisterAction<int>("/1/push8", d => d.First() != 0, (msg, data) =>
            {
                audioPop.PlayEffect("laugh.wav", 1.0, 0.0);
            });

            oscServer.RegisterAction<int>("/1/spiderEyes", (msg, data) =>
            {
                dropSpiderEyes.Value = data.First() != 0;
            });

            oscServer.RegisterAction<int>("/1/push10", d => d.First() != 0, (msg, data) =>
            {
                audioPop.PlayEffect("348 Spider Hiss.wav", 0.0, 1.0);
            });

            oscServer.RegisterAction<int>("/1/push11", (msg, data) =>
            {
                spiderCeilingDrop.Value = data.First() != 0;
            });

            oscServer.RegisterAction<int>("/1/push12", (msg, data) =>
            {
                fog.Value = data.First() != 0;
            });

            oscServer.RegisterAction<int>("/1/push20", d => d.First() != 0, (msg, data) =>
            {
                video3dfx.PlayVideo("PHA_Siren_ComeHither_3DFX_H.mp4");
            });

            oscServer.RegisterAction<int>("/1/push21", d => d.First() != 0, (msg, data) =>
            {
                //               video2.PlayVideo("SkeletonSurprise_Door_Horz_HD.mp4");
                video2.PlayVideo("Beauty_Startler_TVHolo_Hor_HD.mp4");
            });

            oscServer.RegisterAction<int>("/1/push22", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("FearTheReaper_Door_Horz_HD.mp4");
            });

            oscServer.RegisterAction<int>("/1/push23", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("SkeletonSurprise_Door_Horz_HD.mp4");
            });

            oscServer.RegisterAction<int>("/1/push24", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("PHA_Wraith_StartleScare_Holl_H.mp4");
            });

            midiInput.Note(midiChannel, 36).Subscribe(x =>
            {
                if (x)
                    audioCat.PlayEffect("266 Monster Growl 7", 1.0, 1.0);
            });
            midiInput.Note(midiChannel, 37).Subscribe(x =>
            {
                if (x)
                    audioCat.PlayEffect("285 Monster Snarl 2", 1.0, 1.0);
            });
            midiInput.Note(midiChannel, 38).Subscribe(x =>
            {
                if (x)
                    audioCat.PlayEffect("286 Monster Snarl 3", 1.0, 1.0);
            });
            midiInput.Note(midiChannel, 39).Subscribe(x =>
            {
                if (x)
                    audioCat.PlayEffect("287 Monster Snarl 4", 1.0, 1.0);
            });
            midiInput.Note(midiChannel, 40).Subscribe(x =>
            {
                if (x)
                    audioMain.PlayBackground();
            });
            midiInput.Note(midiChannel, 41).Subscribe(x =>
            {
                if (x)
                    audioMain.PauseBackground();
            });
            midiInput.Note(midiChannel, 42).Subscribe(x =>
            {
                spiderCeilingDrop.Value = x;
            });

            //            catMotion.Output.Subscribe(catLights.Control);

            catMotion.Output.Subscribe(x =>
            {
                if (x && hoursSmall.IsOpen)
                    Executor.Current.Execute(catSeq);

                touchOSC.Send("/1/led1", x ? 1 : 0);
            });

            firstBeam.Output.Subscribe(x =>
            {
                touchOSC.Send("/1/led2", x ? 1 : 0);
            });

            finalBeam.Output.Subscribe(x =>
            {
                touchOSC.Send("/1/led3", x ? 1 : 0);
            });

            motion2.Output.Subscribe(x =>
            {
                touchOSC.Send("/1/led4", x ? 1 : 0);
            });

            catSeq.WhenExecuted
                .Execute(instance =>
                {
                    var maxRuntime = System.Diagnostics.Stopwatch.StartNew();

                    catLights.Value = true;

                    while (true)
                    {
                        switch (random.Next(4))
                        {
                            case 0:
                                audioCat.PlayEffect("266 Monster Growl 7", 1.0, 1.0);
                                instance.WaitFor(TimeSpan.FromSeconds(2.0));
                                break;
                            case 1:
                                audioCat.PlayEffect("285 Monster Snarl 2", 1.0, 1.0);
                                instance.WaitFor(TimeSpan.FromSeconds(3.0));
                                break;
                            case 2:
                                audioCat.PlayEffect("286 Monster Snarl 3", 1.0, 1.0);
                                instance.WaitFor(TimeSpan.FromSeconds(2.5));
                                break;
                            case 3:
                                audioCat.PlayEffect("287 Monster Snarl 4", 1.0, 1.0);
                                instance.WaitFor(TimeSpan.FromSeconds(1.5));
                                break;
                            default:
                                instance.WaitFor(TimeSpan.FromSeconds(3.0));
                                break;
                        }

                        instance.CancelToken.ThrowIfCancellationRequested();

                        if (maxRuntime.Elapsed.TotalSeconds > 10)
                            break;
                    }
                })
                .TearDown(() =>
                {
                    catLights.Value = false;
                });
        }

        public override void Start()
        {
        }

        public override void Run()
        {
        }

        public override void Stop()
        {
        }
    }
}
