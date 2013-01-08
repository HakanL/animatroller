using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.Expander;

namespace Animatroller.SceneRunner
{
    internal class HalloweenScene : IScene
    {
        protected OperatingHours hours = new OperatingHours("Hours");
        protected StrobeDimmer georgeStrobeLight = new StrobeDimmer("George Strobe");
        protected StrobeColorDimmer spiderLight = new StrobeColorDimmer("Spider Light");
        protected Dimmer skullsLight = new Dimmer("Skulls");
        protected Dimmer cobWebLight = new Dimmer("Cob Web");
        protected Switch blinkyEyesLight = new Switch("Blinky Eyes");
        protected StrobeColorDimmer rgbLightRight = new StrobeColorDimmer("Light Right");
        protected StrobeColorDimmer georgeLight = new StrobeColorDimmer("George Light");
        protected StrobeColorDimmer leftSkeletonLight = new StrobeColorDimmer("Skeleton Light");
        protected MotorWithFeedback georgeMotor = new MotorWithFeedback("George Motor");
        protected StrobeColorDimmer candyLight = new StrobeColorDimmer("Candy Light");
        protected Switch spiderLift = new Switch("Slider Lift");
        protected Switch smokeMachine = new Switch("Smoke Machine");
        protected Switch spiderEyes = new Switch("Spider Eyes");
        protected Animatroller.Framework.LogicalDevice.DigitalInput pressureMat = new Animatroller.Framework.LogicalDevice.DigitalInput("Pressure Mat");
        protected Animatroller.Framework.LogicalDevice.DigitalInput testButton = new Animatroller.Framework.LogicalDevice.DigitalInput("Test");
        protected Animatroller.Framework.PhysicalDevice.NetworkAudioPlayer audioPlayer;

        protected Animatroller.Framework.Effect.Pulsating pulsatingEffect1;
        protected Animatroller.Framework.Effect.Pulsating pulsatingEffect2;
        protected Animatroller.Framework.Effect.Pulsating candyPulse;
        protected Animatroller.Framework.Effect.Flicker flickerEffect;

        public HalloweenScene()
        {
            hours.AddRange("6:00 pm", "10:00 pm");

            audioPlayer = new Animatroller.Framework.PhysicalDevice.NetworkAudioPlayer(
                Properties.Settings.Default.NetworkAudioPlayerIP,
                Properties.Settings.Default.NetworkAudioPlayerPort);

            pulsatingEffect1 = new Animatroller.Framework.Effect.Pulsating("Pulse FX 1", TimeSpan.FromSeconds(2), 0.1, 0.4);
            pulsatingEffect2 = new Animatroller.Framework.Effect.Pulsating("Pulse FX 2", TimeSpan.FromSeconds(2), 0.3, 0.5);
            candyPulse = new Animatroller.Framework.Effect.Pulsating("Candy Pulse", TimeSpan.FromSeconds(3), 0.01, 0.1);
            flickerEffect = new Animatroller.Framework.Effect.Flicker("Flicker", 0.4, 0.6);

            Executor.Current.Register(this);
        }

        public void WireUp(Animatroller.Simulator.SimulatorForm sim)
        {
            sim.Connect(new Animatroller.Simulator.TestLight(georgeStrobeLight), "George Strobe");
            sim.Connect(new Animatroller.Simulator.TestLight(spiderLight), "Spider Light");
            sim.Connect(new Animatroller.Simulator.TestLight(skullsLight), "Skulls Lights");
            sim.Connect(new Animatroller.Simulator.TestLight(cobWebLight), "Cobweb");
            sim.Connect(new Animatroller.Simulator.TestLight(blinkyEyesLight), "Blinky Eyes");
            sim.Connect(new Animatroller.Simulator.TestLight(rgbLightRight), "Right Skeleton");
            sim.Connect(new Animatroller.Simulator.TestLight(georgeLight), "George Light");
            sim.Connect(new Animatroller.Simulator.TestLight(leftSkeletonLight), "Left Skeleton");
            sim.Connect(new Animatroller.Simulator.TestLight(candyLight), "Candy");

            sim.AddDigitalInput_Momentarily("Pressure Mat").Connect(pressureMat);
            sim.AddDigitalInput_Momentarily("Test Button").Connect(testButton);
            sim.AddDigitalOutput("Spider Lift").Connect(spiderLift);
            sim.AddDigitalOutput("Smoke Machine").Connect(smokeMachine);
            sim.AddDigitalOutput("Spider Eyes").Connect(spiderEyes);
            sim.AddMotor("George").Connect(georgeMotor);
        }

        public void WireUp(IOExpander port)
        {
            port.Connect(new Animatroller.Framework.PhysicalDevice.AmericanDJStrobe(georgeStrobeLight, 5));
            port.Connect(new Animatroller.Framework.PhysicalDevice.RGBStrobe(spiderLight, 10));
            port.Connect(new Animatroller.Framework.PhysicalDevice.GenericDimmer(skullsLight, 1));
            port.Connect(new Animatroller.Framework.PhysicalDevice.GenericDimmer(cobWebLight, 3));
            port.Connect(new Animatroller.Framework.PhysicalDevice.GenericDimmer(blinkyEyesLight, 4));
            port.Connect(new Animatroller.Framework.PhysicalDevice.SmallRGBStrobe(candyLight, 16));
            port.Connect(new Animatroller.Framework.PhysicalDevice.RGBStrobe(rgbLightRight, 20));
            port.Connect(new Animatroller.Framework.PhysicalDevice.RGBStrobe(georgeLight, 30));
            port.Connect(new Animatroller.Framework.PhysicalDevice.RGBStrobe(leftSkeletonLight, 40));

            port.Motor.Connect(georgeMotor);

            port.DigitalInputs[0].Connect(pressureMat);
            port.DigitalInputs[1].Connect(testButton);
            port.DigitalOutputs[0].Connect(spiderLift);
            port.DigitalOutputs[1].Connect(smokeMachine);
            port.DigitalOutputs[2].Connect(spiderEyes);
        }

        public void WireUp(DMXPro port)
        {
            port.Connect(new Animatroller.Framework.PhysicalDevice.AmericanDJStrobe(georgeStrobeLight, 5));
            port.Connect(new Animatroller.Framework.PhysicalDevice.RGBStrobe(spiderLight, 10));
            port.Connect(new Animatroller.Framework.PhysicalDevice.GenericDimmer(skullsLight, 1));
            port.Connect(new Animatroller.Framework.PhysicalDevice.GenericDimmer(cobWebLight, 3));
            port.Connect(new Animatroller.Framework.PhysicalDevice.GenericDimmer(blinkyEyesLight, 4));
            port.Connect(new Animatroller.Framework.PhysicalDevice.SmallRGBStrobe(candyLight, 16));
            port.Connect(new Animatroller.Framework.PhysicalDevice.RGBStrobe(rgbLightRight, 20));
            port.Connect(new Animatroller.Framework.PhysicalDevice.RGBStrobe(georgeLight, 30));
            port.Connect(new Animatroller.Framework.PhysicalDevice.RGBStrobe(leftSkeletonLight, 40));
        }

        public void Start()
        {
            var testSequence = new Sequence("Test Sequence");
            testSequence
                .WhenExecuted
                .Execute(instance =>
                {
                    candyPulse.Start();
                    audioPlayer.PlayEffect("Laugh");
                    georgeMotor.SetVector(1.0, 160, TimeSpan.FromSeconds(5));
                    georgeStrobeLight.SetStrobe(0.55, 1.0);
                    georgeLight.SetStrobe(0.78, Color.Brown);

                    georgeMotor.WaitForVectorReached();
                    instance.WaitFor(TimeSpan.FromSeconds(2));
                    georgeStrobeLight.TurnOff();
                    georgeLight.TurnOff();

                    georgeMotor.SetVector(0.9, 0, TimeSpan.FromSeconds(6));
                    georgeMotor.WaitForVectorReached();
                    candyPulse.Stop();
                });

            var testSequence2 = new Sequence("Test Sequence 2");
                testSequence2.WhenExecuted
                .Execute(instance =>
                {
                    audioPlayer.PlayEffect("348 Spider Hiss");
                    spiderLight.SetStrobe(0.78, Color.Red);

                    spiderLift.SetPower(true);

                    instance.WaitFor(TimeSpan.FromSeconds(3));

                    // Spider up
                    audioPlayer.PlayEffect("Scream");
                    spiderLift.SetPower(false);

                    instance.WaitFor(TimeSpan.FromSeconds(2));
                    spiderLight.TurnOff();
                });

            var testSequence3 = new Sequence("Test Sequence 3");
            testSequence3.WhenExecuted
                .Execute(instance =>
                {
                    spiderEyes.SetPower(true);
                    instance.WaitFor(TimeSpan.FromSeconds(10));
                    spiderEyes.SetPower(false);
                });

            var mainSequence = new Sequence("Main Sequence");
            mainSequence.WhenExecuted
                .Execute(instance =>
                {
                    pulsatingEffect1.Stop();
                    pulsatingEffect2.Stop();
                    flickerEffect.Stop();
                    blinkyEyesLight.SetPower(false);
                    candyLight.SetColor(Color.Red);

                    audioPlayer.PauseBackground();
                    audioPlayer.PlayEffect("Door-creak");
                    instance.WaitFor(TimeSpan.FromSeconds(2));


                    instance.WaitFor(TimeSpan.FromSeconds(2));
                    candyPulse.Stop();
                    candyLight.SetStrobe(1, Color.White);
                    instance.WaitFor(TimeSpan.FromSeconds(0.5));
                    candyLight.TurnOff();
                    instance.WaitFor(TimeSpan.FromSeconds(1));

                    audioPlayer.PlayEffect("348 Spider Hiss");
                    instance.WaitFor(TimeSpan.FromSeconds(0.5));
                    spiderLight.SetStrobe(0.78, Color.Red);

                    spiderLift.SetPower(true);
                    spiderEyes.SetPower(true);

                    instance.WaitFor(TimeSpan.FromSeconds(1));
                    audioPlayer.PlayEffect("348 Spider Hiss");
                    instance.WaitFor(TimeSpan.FromSeconds(2));

                    // Spider up
                    audioPlayer.PlayEffect("Scream");
                    spiderLift.SetPower(false);
                    spiderEyes.SetPower(false);
                    smokeMachine.SetPower(true);

                    instance.WaitFor(TimeSpan.FromSeconds(2));
                    spiderLight.TurnOff();
                    audioPlayer.PlayEffect("Violin screech");
                    instance.WaitFor(TimeSpan.FromSeconds(2));


                    // Skeleton to the right
//                    audioPlayer.PlayEffect("Ghostly");
                    rgbLightRight.SetStrobe(0.78, Color.Violet);
                    instance.WaitFor(TimeSpan.FromMilliseconds(1000));
                    rgbLightRight.SetColor(Color.Red);
                    instance.WaitFor(TimeSpan.FromMilliseconds(1000));
                    rgbLightRight.SetColor(Color.Blue);
                    instance.WaitFor(TimeSpan.FromSeconds(2));
                    rgbLightRight.TurnOff();
                    instance.WaitFor(TimeSpan.FromSeconds(1));


                    // Skeleton to the left
                    audioPlayer.PlayEffect("death-scream");
                    instance.WaitFor(TimeSpan.FromSeconds(0.5));
                    leftSkeletonLight.SetStrobe(0.78, Color.Pink);
                    instance.WaitFor(TimeSpan.FromSeconds(3));
                    smokeMachine.SetPower(false);
                    instance.WaitFor(TimeSpan.FromSeconds(1));
                    leftSkeletonLight.TurnOff();


                    // George
                    audioPlayer.PlayEffect("Laugh");
                    instance.WaitFor(TimeSpan.FromMilliseconds(800));
                    georgeMotor.SetVector(1.0, 160, TimeSpan.FromSeconds(5));
                    georgeStrobeLight.SetStrobe(0.55, 1.0);
                    georgeLight.SetStrobe(0.78, Color.Brown);

                    georgeMotor.WaitForVectorReached();
                    instance.WaitFor(TimeSpan.FromSeconds(2));
                    georgeStrobeLight.TurnOff();
                    georgeLight.TurnOff();

                    candyPulse.MinBrightness = 0.05;
                    candyPulse.MaxBrightness = 1.0;
                    candyLight.SetColor(Color.Violet);
                    candyPulse.Start();
                    georgeMotor.SetVector(0.9, 0, TimeSpan.FromSeconds(6));
                    georgeMotor.WaitForVectorReached();


                    blinkyEyesLight.SetPower(true);

                    flickerEffect.Start();
                    pulsatingEffect1.Start();

                    instance.WaitFor(TimeSpan.FromSeconds(5));
                    smokeMachine.SetPower(true);

                    audioPlayer.PlayBackground();
                    // Wait for reset
                    instance.WaitFor(TimeSpan.FromSeconds(15));
                    pulsatingEffect2.Start();
                    candyPulse.MinBrightness = 0.01;
                    candyPulse.MaxBrightness = 0.1;
                    candyLight.SetColor(Color.Green);
                    smokeMachine.SetPower(false);
                });

            pressureMat.ActiveChanged += (sender, e) =>
            {
                if (e.NewState)
                {
                    if(hours.IsOpen)
                        Executor.Current.Execute(mainSequence);
                    else
                        audioPlayer.PlayEffect("Laugh");
                }
            };

            testButton.ActiveChanged += (sender, e) =>
                {
                    if (e.NewState)
                        Executor.Current.Execute(mainSequence);
                };

            hours.OpenHoursChanged += (sender, e) =>
                {
                    if (e.IsOpenNow)
                    {
                        pulsatingEffect1.Start();
                        pulsatingEffect2.Start();
                        flickerEffect.Start();
                        candyPulse.Start();
                        blinkyEyesLight.SetPower(true);
                        audioPlayer.PlayBackground();
                    }
                    else
                    {
                        pulsatingEffect1.Stop();
                        pulsatingEffect2.Stop();
                        flickerEffect.Stop();
                        candyPulse.Stop();
                        blinkyEyesLight.SetPower(false);
                        audioPlayer.PauseBackground();
                    }
                };

            // Have it turned off, but prepare it with blue color for the effect
            rgbLightRight.SetColor(Color.Blue, 0);
            candyLight.SetColor(Color.Green, 0);
            pulsatingEffect1.AddDevice(rgbLightRight);
            pulsatingEffect2.AddDevice(cobWebLight);
            candyPulse.AddDevice(candyLight);

            flickerEffect.AddDevice(skullsLight);
        }

        public void Run()
        {
        }

        public void Stop()
        {
        }
    }
}
