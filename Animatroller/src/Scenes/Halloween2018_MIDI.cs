using Animatroller.Framework;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Scenes
{
    internal partial class Halloween2018
    {
        public void ConfigureMIDI()
        {
            midiInput.Controller(midiChannel, 1).Controls(faderR.Control);
            midiInput.Controller(midiChannel, 2).Controls(faderG.Control);
            midiInput.Controller(midiChannel, 3).Controls(faderB.Control);
            midiInput.Controller(midiChannel, 4).Controls(faderBright.Control);

            midiInput.Controller(midiChannel, 8).Controls(Exec.MasterVolume);

            midiInput.Note(midiChannel, 36).Subscribe(x =>
            {
                bigEyeSender.Send("/eyecontrol", x ? 1 : 0);
//                if (x)
//                    audioCat.PlayEffect("266 Monster Growl 7.wav", 1.0, 1.0);
            });

            midiInput.Note(midiChannel, 37).Subscribe(x =>
            {
                if (x)
                    audioCat.PlayEffect("285 Monster Snarl 2.wav", 1.0, 1.0);
            });

            midiInput.Note(midiChannel, 38).Subscribe(x =>
            {
                if (x)
                    audioCat.PlayEffect("286 Monster Snarl 3.wav", 1.0, 1.0);
            });

            midiInput.Note(midiChannel, 39).Subscribe(x =>
            {
                if (x)
                    audioCat.PlayEffect("287 Monster Snarl 4.wav", 1.0, 1.0);
            });

            midiInput.Note(midiChannel, 40).Subscribe(x =>
            {
                if (x)
                    audioHifi.PlayEffect("violin screech.wav");
                    //expanderPicture.SendSerial(0, new byte[] { 0x02 });

                //if (x)
                //{
                //    allLights.TakeAndHoldControl();
                //    allLights.SetBrightness(1.0, new Data(DataElements.Color, Color.White));
                //}
                //else
                //    allLights.ReleaseControl();
            });

            midiInput.Note(midiChannel, 41).Subscribe(x =>
            {
                //if (x)
                //    //                    audioHifi.PlayEffect("125919__klankbeeld__horror-what-are-you-doing-here-cathedral.wav");
                //    expanderPicture.SendSerial(0, new byte[] { 0x01 });
            });

            midiInput.Note(midiChannel, 42).Subscribe(x =>
            {
                if (x)
                {
                    manualFader.Value = !manualFader.Value;
                    SetManualColor();
                }
                //                    audioEeebox.PlayEffect("180 Babbling Lunatic.wav");
            });

            midiInput.Note(midiChannel, 43).Subscribe(x =>
            {
                //if (x)
                //    pictureFrame1.SendCommand(null, 0x01);
            });
        }
    }
}
