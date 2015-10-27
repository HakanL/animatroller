using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Animatroller.Framework;
using Animatroller.Framework.Extensions;
using Expander = Animatroller.Framework.Expander;
using Controller = Animatroller.Framework.Controller;
using Animatroller.Framework.LogicalDevice;
using Effect = Animatroller.Framework.Effect;
using Physical = Animatroller.Framework.PhysicalDevice;

namespace Animatroller.SceneRunner
{
    internal class HalloweenScene1 : BaseScene
    {
        private OperatingHours hours;
        private StrobeDimmer georgeStrobeLight;
        private StrobeColorDimmer spiderLight;
        private Dimmer skullsLight;
        private Dimmer cobWebLight;
        private Switch blinkyEyesLight;
        private StrobeColorDimmer rgbLightRight;
        private StrobeColorDimmer georgeLight;
        private StrobeColorDimmer leftSkeletonLight;
        private MotorWithFeedback georgeMotor;
        private StrobeColorDimmer candyLight;
        private Switch spiderLift;
        private Switch smokeMachine;
        private Switch spiderEyes;
        private DigitalInput pressureMat;
        private DigitalInput testButton;
        private Physical.NetworkAudioPlayer audioPlayer;

        private Effect.Pulsating pulsatingEffect1;
        private Effect.Pulsating pulsatingEffect2;
        private Effect.Pulsating candyPulse;
        private Effect.Flicker flickerEffect;


        public HalloweenScene1(IEnumerable<string> args, System.Collections.Specialized.NameValueCollection settings)
        {
            hours = new OperatingHours("Hours");
            georgeStrobeLight = new StrobeDimmer("George Strobe");
            spiderLight = new StrobeColorDimmer("Spider Light");
            skullsLight = new Dimmer("Skulls");
            cobWebLight = new Dimmer("Cob Web");
            blinkyEyesLight = new Switch("Blinky Eyes");
            rgbLightRight = new StrobeColorDimmer("Light Right");
            georgeLight = new StrobeColorDimmer("George Light");
            leftSkeletonLight = new StrobeColorDimmer("Skeleton Light");
            georgeMotor = new MotorWithFeedback("George Motor");
            candyLight = new StrobeColorDimmer("Candy Light");
            spiderLift = new Switch("Slider Lift");
            smokeMachine = new Switch("Smoke Machine");
            spiderEyes = new Switch("Spider Eyes");
            pressureMat = new DigitalInput("Pressure Mat");
            testButton = new DigitalInput("Test");

            pulsatingEffect1 = new Effect.Pulsating(S(2), 0.1, 0.4);
            pulsatingEffect2 = new Effect.Pulsating(S(2), 0.3, 0.5);
            candyPulse = new Effect.Pulsating(S(3), 0.01, 0.1);
            flickerEffect = new Effect.Flicker(0.4, 0.6);

            audioPlayer = new Physical.NetworkAudioPlayer(
                settings["NetworkAudioPlayerIP"],
                int.Parse(settings["NetworkAudioPlayerPort"]));

            hours.AddRange("6:00 pm", "10:00 pm");

            var testSequence = new Controller.Sequence("Test Sequence");
            testSequence
                .WhenExecuted
                .Execute(instance =>
                {
                    candyPulse.Start();
                    audioPlayer.PlayEffect("Laugh");
                    georgeMotor.SetVector(1.0, 160, S(5));
                    georgeStrobeLight.SetStrobe(0.55, 1.0);
                    georgeLight.SetStrobe(0.78, Color.Brown);

                    georgeMotor.WaitForVectorReached();
                    instance.WaitFor(S(2));
                    georgeStrobeLight.TurnOff();
                    georgeLight.TurnOff();

                    georgeMotor.SetVector(0.9, 0, S(6));
                    georgeMotor.WaitForVectorReached();
                    candyPulse.Stop();
                });

            var testSequence2 = new Controller.Sequence("Test Sequence 2");
            testSequence2.WhenExecuted
            .Execute(instance =>
            {
                audioPlayer.PlayEffect("348 Spider Hiss");
                spiderLight.SetStrobe(0.78, Color.Red);

                spiderLift.SetPower(true);

                instance.WaitFor(S(3));

                // Spider up
                audioPlayer.PlayEffect("Scream");
                spiderLift.SetPower(false);

                instance.WaitFor(S(2));
                spiderLight.TurnOff();
            });

            var testSequence3 = new Controller.Sequence("Test Sequence 3");
            testSequence3.WhenExecuted
                .Execute(instance =>
                {
                    spiderEyes.SetPower(true);
                    instance.WaitFor(S(10));
                    spiderEyes.SetPower(false);
                });

            var mainSequence = new Controller.Sequence("Main Sequence");
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
                    instance.WaitFor(S(2));


                    instance.WaitFor(S(2));
                    candyPulse.Stop();
                    candyLight.SetStrobe(1, Color.White);
                    instance.WaitFor(S(0.5));
                    candyLight.TurnOff();
                    instance.WaitFor(S(1));

                    audioPlayer.PlayEffect("348 Spider Hiss");
                    instance.WaitFor(S(0.5));
                    spiderLight.SetStrobe(0.78, Color.Red);

                    spiderLift.SetPower(true);
                    spiderEyes.SetPower(true);

                    instance.WaitFor(S(1));
                    audioPlayer.PlayEffect("348 Spider Hiss");
                    instance.WaitFor(S(2));

                    // Spider up
                    audioPlayer.PlayEffect("Scream");
                    spiderLift.SetPower(false);
                    spiderEyes.SetPower(false);
                    smokeMachine.SetPower(true);

                    instance.WaitFor(S(2));
                    spiderLight.TurnOff();
                    audioPlayer.PlayEffect("Violin screech");
                    instance.WaitFor(S(2));


                    // Skeleton to the right
                    //                    audioPlayer.PlayEffect("Ghostly");
                    rgbLightRight.SetStrobe(0.78, Color.Violet);
                    instance.WaitFor(MS(1000));
                    rgbLightRight.SetColor(Color.Red);
                    instance.WaitFor(MS(1000));
                    rgbLightRight.SetColor(Color.Blue);
                    instance.WaitFor(S(2));
                    rgbLightRight.TurnOff();
                    instance.WaitFor(S(1));


                    // Skeleton to the left
                    audioPlayer.PlayEffect("death-scream");
                    instance.WaitFor(S(0.5));
                    leftSkeletonLight.SetStrobe(0.78, Color.Pink);
                    instance.WaitFor(S(3));
                    smokeMachine.SetPower(false);
                    instance.WaitFor(S(1));
                    leftSkeletonLight.TurnOff();


                    // George
                    audioPlayer.PlayEffect("Laugh");
                    instance.WaitFor(MS(800));
                    georgeMotor.SetVector(1.0, 160, S(5));
                    georgeStrobeLight.SetStrobe(0.55, 1.0);
                    georgeLight.SetStrobe(0.78, Color.Brown);

                    georgeMotor.WaitForVectorReached();
                    instance.WaitFor(S(2));
                    georgeStrobeLight.TurnOff();
                    georgeLight.TurnOff();

                    candyPulse.MinBrightness = 0.05;
                    candyPulse.MaxBrightness = 1.0;
                    candyLight.SetColor(Color.Violet);
                    candyPulse.Start();
                    georgeMotor.SetVector(0.9, 0, S(6));
                    georgeMotor.WaitForVectorReached();


                    blinkyEyesLight.SetPower(true);

                    flickerEffect.Start();
                    pulsatingEffect1.Start();

                    instance.WaitFor(S(5));
                    smokeMachine.SetPower(true);

                    audioPlayer.PlayBackground();
                    // Wait for reset
                    instance.WaitFor(S(15));
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
                    if (hours.IsOpen)
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

        public void WireUp(Expander.IOExpander port)
        {
            port.Connect(new Physical.AmericanDJStrobe(georgeStrobeLight, 5));
            port.Connect(new Physical.RGBStrobe(spiderLight, 10));
            port.Connect(new Physical.GenericDimmer(skullsLight, 1));
            port.Connect(new Physical.GenericDimmer(cobWebLight, 3));
            port.Connect(new Physical.GenericDimmer(blinkyEyesLight, 4));
            port.Connect(new Physical.SmallRGBStrobe(candyLight, 16));
            port.Connect(new Physical.RGBStrobe(rgbLightRight, 20));
            port.Connect(new Physical.RGBStrobe(georgeLight, 30));
            port.Connect(new Physical.RGBStrobe(leftSkeletonLight, 40));

            port.Motor.Connect(georgeMotor);

            port.DigitalInputs[0].Connect(pressureMat);
            port.DigitalInputs[1].Connect(testButton);
            port.DigitalOutputs[0].Connect(spiderLift);
            port.DigitalOutputs[1].Connect(smokeMachine);
            port.DigitalOutputs[2].Connect(spiderEyes);
        }

        public void WireUp(Expander.DMXPro port)
        {
            port.Connect(new Physical.AmericanDJStrobe(georgeStrobeLight, 5));
            port.Connect(new Physical.RGBStrobe(spiderLight, 10));
            port.Connect(new Physical.GenericDimmer(skullsLight, 1));
            port.Connect(new Physical.GenericDimmer(cobWebLight, 3));
            port.Connect(new Physical.GenericDimmer(blinkyEyesLight, 4));
            port.Connect(new Physical.SmallRGBStrobe(candyLight, 16));
            port.Connect(new Physical.RGBStrobe(rgbLightRight, 20));
            port.Connect(new Physical.RGBStrobe(georgeLight, 30));
            port.Connect(new Physical.RGBStrobe(leftSkeletonLight, 40));
        }

        public override void Run()
        {
        }

        public override void Stop()
        {
        }
    }
}
