using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Reactive.Subjects;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;
using Expander = Animatroller.Framework.Expander;
using Controller = Animatroller.Framework.Controller;
using Physical = Animatroller.Framework.PhysicalDevice;
using Effect = Animatroller.Framework.Effect;
using Import = Animatroller.Framework.Import;
using System.IO;
using Animatroller.Framework.Extensions;

namespace Animatroller.Scenes
{
    internal partial class Xmas2017 : BaseScene
    {
        public void ConfigureMIDI()
        {
            //midiAkai.Controller(midiChannel, 1).Subscribe(x => blackOut.Value = x.Value);
            //midiAkai.Controller(midiChannel, 2).Subscribe(x => whiteOut.Value = x.Value);

            midiAkai.Note(midiChannel, 36).Subscribe(x =>
            {
                if (x)
                    stateMachine.GoToState(States.DarthVader);
                //                lightFlood6.SetColor(Color.Red, x ? 1 : 0);
            });

            midiAkai.Note(midiChannel, 37).Subscribe(x =>
            {
                if (x)
                    stateMachine.GoToState(States.MusicChristmasCanon);
            });

            midiAkai.Note(midiChannel, 38).Subscribe(x =>
            {
                if (x)
                    stateMachine.GoToState(States.MusicBelieve);
            });

            midiAkai.Note(midiChannel, 39).Subscribe(x =>
            {
                if (x)
                    stateMachine.GoToState(States.MusicSarajevo);
            });

            midiAkai.Note(midiChannel, 42).Subscribe(x =>
            {
                if (x)
                    stateMachine.GoToState(States.MusicHolyNight);
            });

            midiAkai.Note(midiChannel, 43).Subscribe(x =>
            {
                if (x)
                    stateMachine.GoToState(States.MusicCarol);
            });


            midiAkai.Controller(midiChannel, 1).Controls(faderR.Control);
            midiAkai.Controller(midiChannel, 2).Controls(faderG.Control);
            midiAkai.Controller(midiChannel, 3).Controls(faderB.Control);
            midiAkai.Controller(midiChannel, 4).Controls(faderBright.Control);
        }
    }
}
