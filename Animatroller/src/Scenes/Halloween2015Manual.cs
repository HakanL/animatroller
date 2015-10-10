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
        private Expander.Raspberry raspberryCat = new Expander.Raspberry("192.168.240.115:5005", 3333);
        private Expander.Raspberry raspberryLocal = new Expander.Raspberry("127.0.0.1:5005", 3339);

        private DigitalOutput2 spiderCeiling = new DigitalOutput2("Spider Ceiling");
        private DigitalOutput2 spiderCeilingDrop = new DigitalOutput2("Spider Ceiling Drop");
        private DigitalInput2 catMotion = new DigitalInput2();
        private DigitalInput2 firstBeam = new DigitalInput2();
        private Expander.AcnStream acnOutput = new Expander.AcnStream();
        private DigitalOutput2 catAir = new DigitalOutput2(initial: true);
        private DigitalOutput2 catLights = new DigitalOutput2();

        public Halloween2015Manual(IEnumerable<string> args)
        {
            raspberryCat.DigitalInputs[4].Connect(catMotion, true);
            raspberryCat.DigitalInputs[5].Connect(firstBeam, false);
            raspberryLocal.DigitalOutputs[7].Connect(spiderCeilingDrop);
            raspberryCat.Connect(audioCat);
            raspberryLocal.Connect(audioMain);

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


            //            midiInput.Note(midiChannel, 40).Controls(catMotion.Control);
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

            catMotion.Output.Subscribe(catLights.Control);
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
