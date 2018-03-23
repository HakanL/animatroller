using System;
using System.Drawing;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Reactive;
using Animatroller.Framework;
using Animatroller.Framework.Extensions;
using Expander = Animatroller.Framework.Expander;
using Controller = Animatroller.Framework.Controller;
using Animatroller.Framework.LogicalDevice;
using Effect = Animatroller.Framework.Effect;
using Effect2 = Animatroller.Framework.Effect2;
using Physical = Animatroller.Framework.PhysicalDevice;

namespace Animatroller.Scenes
{
    internal class TestMidi1 : BaseScene
    {
        public class PhysicalDevices
        {
            public Physical.MFL7x10WPar MFL;
            public Physical.MonopriceMovingHeadLight12chn MovingHead;
            public Physical.SmallRGBStrobe Small;
            public Physical.MonopriceRGBWPinSpot Spot;
        }

        Color[] colorPresets = new Color[]
        {
            Color.Purple,
            Color.DarkSalmon,
            Color.Yellow,
            Color.Turquoise
        };

        (double Pan, double Tilt)[] panTiltPresets = new(double Pan, double Tilt)[8];

        private PhysicalDevices p;

        private Expander.MidiInput2 midiInput = new Expander.MidiInput2("BCF2000", true);
        private Expander.MidiOutput midiOutput = new Expander.MidiOutput("BCF2000", true);
        Expander.MidiInput2 midiInput2 = new Expander.MidiInput2("LPD8", ignoreMissingDevice: true);

        private StrobeColorDimmer3 testLight1 = new StrobeColorDimmer3("Test 1");
        private MovingHead testLight2 = new MovingHead("Test 2");
        private StrobeColorDimmer3 testLight3 = new StrobeColorDimmer3("Test 3");
        private StrobeColorDimmer3 testLight4 = new StrobeColorDimmer3("Test 4");

        AnalogInput3 faderR1 = new AnalogInput3(persistState: true);
        AnalogInput3 faderG1 = new AnalogInput3(persistState: true);
        AnalogInput3 faderB1 = new AnalogInput3(persistState: true);
        AnalogInput3 faderBright1 = new AnalogInput3(persistState: true);

        AnalogInput3 faderR2 = new AnalogInput3(persistState: true);
        AnalogInput3 faderG2 = new AnalogInput3(persistState: true);
        AnalogInput3 faderB2 = new AnalogInput3(persistState: true);
        AnalogInput3 faderBright2 = new AnalogInput3(persistState: true);
        AnalogInput3 pan = new AnalogInput3(persistState: true);
        AnalogInput3 tilt = new AnalogInput3(persistState: true);

        AnalogInput3 faderR3 = new AnalogInput3(persistState: true);
        AnalogInput3 faderG3 = new AnalogInput3(persistState: true);
        AnalogInput3 faderB3 = new AnalogInput3(persistState: true);
        AnalogInput3 faderBright3 = new AnalogInput3(persistState: true);

        AnalogInput3 faderR4 = new AnalogInput3(persistState: true);
        AnalogInput3 faderG4 = new AnalogInput3(persistState: true);
        AnalogInput3 faderB4 = new AnalogInput3(persistState: true);
        AnalogInput3 faderBright4 = new AnalogInput3(persistState: true);

        Effect.Pulsating pulsatingEffect1 = new Effect.Pulsating(S(2), 0.05, 1.0, false);
        Effect.Pulsating pulsatingEffect2 = new Effect.Pulsating(S(2), 0.05, 1.0, false);
        Effect.Pulsating pulsatingEffect3 = new Effect.Pulsating(S(2), 0.05, 1.0, false);
        Effect.Pulsating pulsatingEffect4 = new Effect.Pulsating(S(2), 0.05, 1.0, false);

        private Expander.AcnStream acnOutput = new Expander.AcnStream();

        public TestMidi1(IEnumerable<string> args)
        {
            p = new PhysicalDevices
            {
                MFL = new Physical.MFL7x10WPar(testLight1, 320),
                MovingHead = new Physical.MonopriceMovingHeadLight12chn(testLight2, 200),
                Small = new Physical.SmallRGBStrobe(testLight3, 1),
                Spot = new Physical.MonopriceRGBWPinSpot(testLight4, 20)
            };

            // Generate pan/tilt presets
            for (int i = 0; i < panTiltPresets.Length; i++)
            {
                panTiltPresets[i] = (random.NextDouble(), random.NextDouble());
            }

            pulsatingEffect1.ConnectTo(testLight1);
            pulsatingEffect2.ConnectTo(testLight2);
            pulsatingEffect3.ConnectTo(testLight3);
            pulsatingEffect4.ConnectTo(testLight4);

            acnOutput.Connect(p.MFL, 20);
            acnOutput.Connect(p.MovingHead, 20);
            acnOutput.Connect(p.Small, 20);
            acnOutput.Connect(p.Spot, 20);

            midiInput.Controller(0, 81).Controls(faderR1.Control);
            midiInput.Controller(0, 82).Controls(faderG1.Control);
            midiInput.Controller(0, 83).Controls(faderB1.Control);
            midiInput.Controller(0, 1).Controls(faderBright1.Control);

            midiInput.Controller(0, 85).Controls(faderR2.Control);
            midiInput.Controller(0, 86).Controls(faderG2.Control);
            midiInput.Controller(0, 87).Controls(faderB2.Control);
            midiInput.Controller(0, 5).Controls(faderBright2.Control);
            midiInput.Controller(0, 7).Controls(pan.Control);
            midiInput.Controller(0, 8).Controls(tilt.Control);

            midiInput.Controller(0, 65).Controls(x =>
            {
                GoToColor1(colorPresets[0]);
            });

            midiInput.Controller(0, 66).Controls(x =>
            {
                GoToColor1(colorPresets[1]);
            });

            midiInput.Controller(0, 67).Controls(x =>
            {
                GoToColor1(colorPresets[2]);
            });

            midiInput.Controller(0, 68).Controls(x =>
            {
                GoToColor1(colorPresets[3]);
            });

            midiInput.Controller(0, 69).Controls(x =>
            {
                GoToColor2(colorPresets[0]);
            });

            midiInput.Controller(0, 70).Controls(x =>
            {
                GoToColor2(colorPresets[1]);
            });

            midiInput.Controller(0, 71).Controls(x =>
            {
                GoToColor2(colorPresets[2]);
            });

            midiInput.Controller(0, 72).Controls(x =>
            {
                GoToColor2(colorPresets[3]);
            });

            midiInput.Controller(0, 89).Controls(x =>
            {
                if (x.Value == 1)
                    pulsatingEffect1.Start();
                else
                {
                    pulsatingEffect1.Stop();
                    testLight1.SetBrightness(faderBright1.Value);
                }
            });

            midiInput.Controller(0, 90).Controls(x =>
            {
                if (x.Value == 1)
                    pulsatingEffect2.Start();
                else
                {
                    pulsatingEffect2.Stop();
                    testLight1.SetBrightness(faderBright2.Value);
                }
            });

            midiInput.Controller(0, 91).Controls(x =>
            {
                if (x.Value == 1)
                    pulsatingEffect3.Start();
                else
                {
                    pulsatingEffect3.Stop();
                    testLight3.SetBrightness(faderBright3.Value);
                }
            });

            midiInput.Controller(0, 92).Controls(x =>
            {
                if (x.Value == 1)
                    pulsatingEffect4.Start();
                else
                {
                    pulsatingEffect4.Stop();
                    testLight4.SetBrightness(faderBright4.Value);
                }
            });

            midiInput.Controller(0, 73).Controls(x => GoToPanTilt(panTiltPresets[0]));
            midiInput.Controller(0, 74).Controls(x => GoToPanTilt(panTiltPresets[1]));
            midiInput.Controller(0, 75).Controls(x => GoToPanTilt(panTiltPresets[2]));
            midiInput.Controller(0, 76).Controls(x => GoToPanTilt(panTiltPresets[3]));
            midiInput.Controller(0, 77).Controls(x => GoToPanTilt(panTiltPresets[4]));
            midiInput.Controller(0, 78).Controls(x => GoToPanTilt(panTiltPresets[5]));
            midiInput.Controller(0, 79).Controls(x => GoToPanTilt(panTiltPresets[6]));
            midiInput.Controller(0, 80).Controls(x => GoToPanTilt(panTiltPresets[7]));

            // LPD8
            midiInput2.Controller(0, 1).Controls(faderR3.Control);
            midiInput2.Controller(0, 2).Controls(faderG3.Control);
            midiInput2.Controller(0, 3).Controls(faderB3.Control);
            midiInput2.Controller(0, 4).Controls(faderBright3.Control);

            midiInput2.Controller(0, 5).Controls(faderR4.Control);
            midiInput2.Controller(0, 6).Controls(faderG4.Control);
            midiInput2.Controller(0, 7).Controls(faderB4.Control);
            midiInput2.Controller(0, 8).Controls(faderBright4.Control);

            midiInput2.Note(0, 36).Controls(x =>
            {
                if (x)
                    GoToColor4(colorPresets[0]);
            });
            midiInput2.Note(0, 37).Controls(x =>
            {
                if (x)
                    GoToColor4(colorPresets[1]);
            });
            midiInput2.Note(0, 38).Controls(x =>
            {
                if (x)
                    GoToColor4(colorPresets[2]);
            });
            midiInput2.Note(0, 39).Controls(x =>
            {
                if (x)
                    GoToColor4(colorPresets[3]);
            });

            midiInput2.Note(0, 40).Controls(x =>
            {
                if (x)
                    GoToColor3(colorPresets[0]);
            });
            midiInput2.Note(0, 41).Controls(x =>
            {
                if (x)
                    GoToColor3(colorPresets[1]);
            });
            midiInput2.Note(0, 42).Controls(x =>
            {
                if (x)
                    GoToColor3(colorPresets[2]);
            });
            midiInput2.Note(0, 43).Controls(x =>
            {
                if (x)
                    GoToColor3(colorPresets[3]);
            });

            faderR1.WhenOutputChanges(v => { SetManualColor(); });
            faderG1.WhenOutputChanges(v => { SetManualColor(); });
            faderB1.WhenOutputChanges(v => { SetManualColor(); });
            faderBright1.WhenOutputChanges(v => { testLight1.SetBrightness(v); });

            faderR2.WhenOutputChanges(v => { SetManualColor(); });
            faderG2.WhenOutputChanges(v => { SetManualColor(); });
            faderB2.WhenOutputChanges(v => { SetManualColor(); });
            faderBright2.WhenOutputChanges(v => { testLight2.SetBrightness(v); });

            pan.WhenOutputChanges(v => { testLight2.SetPan(v * 540); });
            tilt.WhenOutputChanges(v => { testLight2.SetTilt(v * 270); });

            faderR3.WhenOutputChanges(v => { SetManualColor(); });
            faderG3.WhenOutputChanges(v => { SetManualColor(); });
            faderB3.WhenOutputChanges(v => { SetManualColor(); });
            faderBright3.WhenOutputChanges(v => { testLight3.SetBrightness(v); });

            faderR4.WhenOutputChanges(v => { SetManualColor(); });
            faderG4.WhenOutputChanges(v => { SetManualColor(); });
            faderB4.WhenOutputChanges(v => { SetManualColor(); });
            faderBright4.WhenOutputChanges(v => { testLight4.SetBrightness(v); });
        }

        void GoToColor1(Color input)
        {
            midiOutput.Send(0, 81, input.R.GetDouble().GetByteScale(127));
            midiOutput.Send(0, 82, input.G.GetDouble().GetByteScale(127));
            midiOutput.Send(0, 83, input.B.GetDouble().GetByteScale(127));

            faderR1.Value = input.R.GetDouble();
            faderG1.Value = input.G.GetDouble();
            faderB1.Value = input.B.GetDouble();
        }

        void GoToColor2(Color input)
        {
            midiOutput.Send(0, 85, input.R.GetDouble().GetByteScale(127));
            midiOutput.Send(0, 86, input.G.GetDouble().GetByteScale(127));
            midiOutput.Send(0, 87, input.B.GetDouble().GetByteScale(127));

            faderR2.Value = input.R.GetDouble();
            faderG2.Value = input.G.GetDouble();
            faderB2.Value = input.B.GetDouble();
        }

        void GoToColor3(Color input)
        {
            faderR3.Value = input.R.GetDouble();
            faderG3.Value = input.G.GetDouble();
            faderB3.Value = input.B.GetDouble();
        }

        void GoToColor4(Color input)
        {
            faderR4.Value = input.R.GetDouble();
            faderG4.Value = input.G.GetDouble();
            faderB4.Value = input.B.GetDouble();
        }

        void GoToPanTilt((double Pan, double Tilt) panTilt)
        {
            GoToPanTilt(panTilt.Pan, panTilt.Tilt);
        }

        void GoToPanTilt(double pan, double tilt)
        {
            this.pan.Value = pan;
            this.tilt.Value = tilt;

            midiOutput.Send(0, 7, this.pan.Value.GetByteScale(127));
            midiOutput.Send(0, 8, this.tilt.Value.GetByteScale(127));
        }

        Color GetFaderColor1()
        {
            return HSV.ColorFromRGB(faderR1.Value, faderG1.Value, faderB1.Value);
        }

        Color GetFaderColor2()
        {
            return HSV.ColorFromRGB(faderR2.Value, faderG2.Value, faderB2.Value);
        }

        Color GetFaderColor3()
        {
            return HSV.ColorFromRGB(faderR3.Value, faderG3.Value, faderB3.Value);
        }

        Color GetFaderColor4()
        {
            return HSV.ColorFromRGB(faderR4.Value, faderG4.Value, faderB4.Value);
        }

        void SetManualColor()
        {
            testLight1.SetColor(GetFaderColor1());
            testLight2.SetColor(GetFaderColor2());
            testLight3.SetColor(GetFaderColor3());
            testLight4.SetColor(GetFaderColor4());
        }

        public override void Run()
        {
        }

        public override void Stop()
        {
        }
    }
}
