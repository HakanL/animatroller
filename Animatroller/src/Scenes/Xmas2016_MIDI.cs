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
    internal partial class Xmas2016 : BaseScene
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
                    stateMachine.GoToState(States.Music1);
            });

            midiAkai.Note(midiChannel, 38).Subscribe(x =>
            {
                if (x)
                    stateMachine.GoToState(States.Music2);
            });

            midiAkai.Note(midiChannel, 39).Subscribe(x =>
            {
                if (x)
                    stateMachine.GoToState(States.Music3);
                //               lightNet7.SetBrightness(x ? 1 : 0);
                //if (x)
                //{
                //    lorFeelTheLight.Stop();
                //    audioHiFi.PauseTrack();
                //}
            });

            midiAkai.Note(midiChannel, 40).Subscribe(x =>
            {
                if (x)
                {
                    if (Exec.IsRunning(subRandomSantaVideo))
                        Exec.Cancel(subRandomSantaVideo);
                    else
                        subRandomSantaVideo.Run();
                }
            });

            midiAkai.Note(midiChannel, 41).Subscribe(x =>
            {
                if (x)
                    stateMachine.GoToState(States.SantaVideo);
            });

            midiAkai.Controller(midiChannel, 1).Controls(faderR.Control);
            midiAkai.Controller(midiChannel, 2).Controls(faderG.Control);
            midiAkai.Controller(midiChannel, 3).Controls(faderB.Control);
            midiAkai.Controller(midiChannel, 4).Controls(faderBright.Control);
            midiAkai.Controller(midiChannel, 5).Controls(faderPan.Control);
            midiAkai.Controller(midiChannel, 6).Controls(faderTilt.Control);
        }
    }
}
