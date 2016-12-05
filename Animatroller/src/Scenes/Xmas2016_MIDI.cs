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

namespace Animatroller.Scenes
{
    internal partial class Xmas2016 : BaseScene
    {
        public void ConfigureMIDI()
        {
            midiAkai.Controller(midiChannel, 1).Subscribe(x => blackOut.Value = x.Value);
            midiAkai.Controller(midiChannel, 2).Subscribe(x => whiteOut.Value = x.Value);

            midiAkai.Note(midiChannel, 36).Subscribe(x =>
            {
                if (x)
                    subOlaf.Run();
            });

            midiAkai.Note(midiChannel, 37).Subscribe(x =>
            {
                lightNet5.SetBrightness(x ? 1 : 0);
                //if (x)
                //    audioHiFi.PlayEffect("WarmHugs.wav");
                //                subR2D2.Run();
            });

            midiAkai.Note(midiChannel, 38).Subscribe(x =>
            {
                lightNet6.SetBrightness(x ? 1 : 0);
                //if (x)
                //    stateMachine.GoToState(States.Music1);
                //                    audio2.PlayTrack("08 Feel the Light.wav");
            });

            midiAkai.Note(midiChannel, 39).Subscribe(x =>
            {
               lightNet7.SetBrightness(x ? 1 : 0);
                //if (x)
                //{
                //    lorFeelTheLight.Stop();
                //    audioHiFi.PauseTrack();
                //}
            });

            midiAkai.Note(midiChannel, 40).Subscribe(x =>
            {
                lightNet8.SetBrightness(x ? 1 : 0);
                //if (x)
                //{
                //    stateMachine.GoToState(States.SantaVideo);
                //}
            });

            midiAkai.Note(midiChannel, 41).Subscribe(x =>
            {
//                lightHangingStar.SetBrightness(x ? 1 : 0);
            });
        }
    }
}
