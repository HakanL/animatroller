using System;
using System.Drawing;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
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
    internal class IceBucket : BaseScene, ISceneRequiresRaspExpander1, ISceneSupportsSimulator, ISceneRequiresDMXPro
    {
        public enum States
        {
            Idle,
            Armed,
            Dump,
            Dumped,
            Reset
        }

        private Controller.EnumStateMachine<States> stateMachine;
        private Expander.OscServer oscServer;
        private AudioPlayer audioPlayer;
        private DigitalInput inputArm;
        private DigitalInput inputDisarm;
        private DigitalInput inputReset;
        private DigitalInput inputDump;
        private DigitalInput inputNextSong;
        private Switch relayStart;
        private Switch relayDirA;
        private Switch relayDirB;
        private StrobeColorDimmer lightSpot;
        private Effect.Pulsating pulsatingEffect1;


        public IceBucket(IEnumerable<string> args)
        {
            stateMachine = new Controller.EnumStateMachine<States>("Main");
            pulsatingEffect1 = new Effect.Pulsating("Pulse FX 1", S(2), 0.05, 1.0, false);
            lightSpot = new StrobeColorDimmer("Spotlight");

            inputArm = new DigitalInput("Arm");
            inputDisarm = new DigitalInput("Disarm");
            inputDump = new DigitalInput("Dump");
            inputNextSong = new DigitalInput("Next Song");
            inputReset = new DigitalInput("Reset");
            relayStart = new Switch("Relay Start");
            relayDirA = new Switch("Relay Dir A");
            relayDirB = new Switch("Relay Dir B");
            
            audioPlayer = new AudioPlayer("Audio Player");

            this.oscServer = new Expander.OscServer(9999);

            stateMachine.For(States.Armed)
                .SetUp(() =>
                {
                    pulsatingEffect1.Start();
                    audioPlayer.PlayBackground();
                })
                .Execute(instance =>
                {
                    while (!instance.IsCancellationRequested)
                    {
                        instance.WaitFor(S(1));
                    }
                })
                .TearDown(() =>
                {
                    audioPlayer.PauseBackground();
                    pulsatingEffect1.Stop();
                });

            stateMachine.For(States.Dumped)
                .Execute(instance =>
                {
                    while (!instance.IsCancellationRequested)
                    {
                        instance.WaitFor(S(1));
                    }
                });
;

            stateMachine.SetBackgroundState(States.Idle);
        }

        public void WireUp(Animatroller.Simulator.SimulatorForm sim)
        {
            sim.AddDigitalInput_Momentarily(inputArm);
            sim.AddDigitalInput_Momentarily(inputDisarm);
            sim.AddDigitalInput_Momentarily(inputDump);
            sim.AddDigitalInput_Momentarily(inputReset);

            sim.AddDigitalInput_Momentarily(inputNextSong);

            sim.AutoWireUsingReflection(this);
        }

        public void WireUp1(Expander.Raspberry port)
        {
            port.DigitalOutputs[7].Connect(relayDirA);
            port.DigitalOutputs[6].Connect(relayDirB);
            port.DigitalOutputs[5].Connect(relayStart);

            port.Connect(audioPlayer);
        }

        public void WireUp(Expander.DMXPro port)
        {
            port.Connect(new Physical.SmallRGBStrobe(lightSpot, 1));
        }

        public override void Start()
        {
            var popSeq = new Controller.Sequence("Pop Sequence");
            popSeq.WhenExecuted
                .Execute(instance =>
                    {
                        //                        audioPlayer.PlayEffect("laugh");
                        //                        instance.WaitFor(TimeSpan.FromSeconds(1));
                        relayDirA.SetPower(true);
                        relayDirB.SetPower(false);
                        relayStart.SetPower(true);
                        instance.WaitFor(TimeSpan.FromSeconds(0.9));
                        relayStart.SetPower(false);
                        relayDirA.SetPower(false);
                        relayDirB.SetPower(false);
                    });

            var resetSeq = new Controller.Sequence("Reset Sequence");
            resetSeq.WhenExecuted
                .Execute(instance =>
                {
                    relayDirA.SetPower(false);
                    relayDirB.SetPower(true);
                    //                    relayStart.SetPower(true);
                    //                    instance.WaitFor(TimeSpan.FromSeconds(0.5));
                    //                    relayStart.SetPower(false);

                    instance.WaitFor(TimeSpan.FromSeconds(3));
                    relayDirA.SetPower(false);
                    relayDirB.SetPower(false);
                });
            this.oscServer.RegisterAction<int>("/osc/arm", (msg, data) =>
                {
                    if (data.Any())
                    {
                        if (data.First() != 0)
                            stateMachine.SetState(States.Armed);
                    }
                });

            this.oscServer.RegisterAction<int>("/osc/disarm", (msg, data) =>
            {
                if (data.Any())
                {
                    if (data.First() != 0)
                        stateMachine.SetState(States.Idle);
                }
            });

            this.oscServer.RegisterAction<int>("/osc/dump", (msg, data) =>
            {
                if (data.Any())
                {
                    if (data.First() != 0)
                    {
                        if(stateMachine.CurrentState == States.Armed)
                            stateMachine.SetState(States.Dump);
                    }
                }
            });

            this.oscServer.RegisterAction<int>("/osc/reset", (msg, data) =>
            {
                if (data.Any())
                {
                    if (data.First() != 0)
                        stateMachine.SetState(States.Reset);
                }
            });

            inputNextSong.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                {
                    audioPlayer.NextBackgroundTrack();
                }
            };

            inputArm.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                {
                    stateMachine.SetState(States.Armed);
                }
            };

            inputDisarm.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                {
                    stateMachine.SetState(States.Idle);
                }
            };

            inputDump.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                {
                    if (stateMachine.CurrentState == States.Armed)
                        stateMachine.SetState(States.Dump);
                }
            };

            inputReset.ActiveChanged += (sender, e) =>
            {
                stateMachine.SetState(States.Reset);
            };

            stateMachine.ForFromSequence(States.Dump, popSeq);
            stateMachine.ForFromSequence(States.Reset, resetSeq);

            lightSpot.SetColor(Color.Red, 0);
            pulsatingEffect1.AddDevice(lightSpot);

            stateMachine.SetState(States.Idle);
        }

        public override void Run()
        {
            Thread.Sleep(S(0.3));
            relayDirA.SetPower(true);
            relayDirB.SetPower(true);
            relayStart.SetPower(true);
            Thread.Sleep(S(0.3));
            relayStart.SetPower(false);
            relayDirA.SetPower(false);
            relayDirB.SetPower(false);
            //            audioPlayer.PlayEffect("Laugh");
        }

        public override void Stop()
        {
        }
    }
}
