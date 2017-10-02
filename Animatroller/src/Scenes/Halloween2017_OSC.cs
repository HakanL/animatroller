using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Scenes
{
    internal partial class Halloween2017
    {
        public void ConfigureOSC()
        {
            oscServer.RegisterActionSimple<double>("/HazerFan/x", (msg, data) =>
            {
                hazerFanSpeed.SetBrightness(data);
            });

            oscServer.RegisterActionSimple<double>("/HazerHaze/x", (msg, data) =>
            {
                hazerHazeOutput.SetBrightness(data);
            });

            oscServer.RegisterAction<bool>("/Triggers/x", (msg, data) =>
            {
                if (data[0])
                    expanderPicture.SendSerial(0, new byte[] { 0x01 });

                if (data[1])
                    expanderPicture.SendSerial(0, new byte[] { 0x02 });

                if (data[2])
                    expanderGhost.SendSerial(0, new byte[] { 0x01 });

                if (data[3])
                    subFog.Run();

                if (data[4])
                {
                    floodLights.Value = !floodLights.Value;
                }

                bigSpiderEyes.SetBrightness(data[5] ? 1.0 : 0.0);

                if (data[6])
                    audio2.PlayEffect("sixthsense-deadpeople.wav");

                //                flyingSkeletonEyes.SetBrightness(data[7] ? 1.0 : 0.0);

                if (data[7])
                    subSpiderJump.Run();

                if (data[8])
                    audioFlying.PlayEffect("162 Blood Curdling Scream of Terror.wav");

                if (data[9])
                    sub3dfxRandom.Run();

                //wall8Light.SetBrightness(data[10] ? 1 : 0);
                //wall9Light.SetBrightness(data[11] ? 1 : 0);

                if (data[12])
                    subLast.Run();

                if (data[13])
                    sub3dfxLady.Run();

                if (data[14])
                    sub3dfxMan.Run();

                if (data[15])
                    sub3dfxKids.Run();
            }, 25);

            oscServer.RegisterAction<bool>("/SoundBoard/x", (msg, data) =>
            {
                string fileName = null;

                if (data[0])
                    fileName = "Happy Halloween.wav";

                if (data[1])
                    fileName = "sixthsense-deadpeople.wav";

                if (data[2])
                    fileName = "Leave Now.wav";

                if (data[3])
                    fileName = "scream.wav";

                if (data[4])
                    fileName = "424 Coyote Howling.wav";

                if (data[5])
                    fileName = "125919__klankbeeld__horror-what-are-you-doing-here-cathedral.wav";

                if (data[6])
                    fileName = "348 Spider Hiss.wav";

                if (data[7])
                    fileName = "Thriller2.wav";

                if (data[8])
                    fileName = "266 Monster Growl 7.wav";

                if (data[9])
                    fileName = "285 Monster Snarl 2.wav";

                if (data[10])
                    fileName = "286 Monster Snarl 3.wav";

                if (data[11])
                    fileName = "287 Monster Snarl 4.wav";

                if (data[12])
                    fileName = "Short Laugh.wav";

                if (data[13])
                    fileName = "Evil-Laugh.wav";

                if (data[14])
                    fileName = "Who is that knocking.wav";

                if (data[15])
                    fileName = "05 I'm a Little Teapot.wav";

                if (data[16])
                    fileName = "162 Blood Curdling Scream of Terror.wav";

                if (data[17])
                    fileName = "180 Babbling Lunatic.wav";

                if (data[18])
                    fileName = "386 Demon Creature Growls.wav";

                if (data[19])
                    fileName = "242004__junkfood2121__fart-01.wav";

                if (data[20])
                    fileName = "death-scream.wav";

                if (data[21])
                    fileName = "gollum_precious1.wav";

                if (data[22])
                    fileName = "laugh.wav";

                if (data[23])
                    fileName = "violin screech.wav";

                if (data[24])
                    fileName = "WarmHugs.wav";

                if (string.IsNullOrEmpty(fileName))
                    // Ignore
                    return;

                switch (soundBoardOutputIndex)
                {
                    case 0:
                        audioHifi.PlayNewEffect(fileName);
                        break;

                    case 1:
                        audio2.PlayNewEffect(fileName);
                        break;

                    case 2:
                        audioFlying.PlayNewEffect(fileName);
                        break;

                    case 3:
                        audioPop.PlayNewEffect(fileName);
                        break;

                    case 4:
                        audioCat.PlayNewEffect(fileName);
                        break;

                    case 5:
                        audioPumpkin.PlayNewEffect(fileName);
                        break;
                }
            }, 25);

            oscServer.RegisterAction<bool>("/Blocks/x", (msg, data) =>
            {
                blockMaster.Value = data[0];
                blockCat.Value = data[1];
                blockFirst.Value = data[2];
                blockPicture.Value = data[3];
                blockGhost.Value = data[4];
                blockLast.Value = data[5];
                blockPumpkin.Value = data[6];
            }, 7);

            oscServer.RegisterActionSimple<int>("/AudioOutput/selection", (msg, data) =>
            {
                soundBoardOutputIndex = data;
            });

            oscServer.RegisterAction<int>("/3/multipush1/6/1", d => d.First() != 0, (msg, data) =>
            {
                audio2.PlayEffect("sixthsense-deadpeople.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/6/2", d => d.First() != 0, (msg, data) =>
            {
                audio2.PlayEffect("162 Blood Curdling Scream of Terror.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/6/3", d => d.First() != 0, (msg, data) =>
            {
                audio2.PlayEffect("424 Coyote Howling.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/6/4", d => d.First() != 0, (msg, data) =>
            {
                audio2.PlayEffect("125919__klankbeeld__horror-what-are-you-doing-here-cathedral.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/6/5", d => d.First() != 0, (msg, data) =>
            {
                audio2.PlayEffect("242004__junkfood2121__fart-01.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/5/1", d => d.First() != 0, (msg, data) =>
            {
                audio2.PlayEffect("death-scream.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/5/2", d => d.First() != 0, (msg, data) =>
            {
                audio2.PlayEffect("scream.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/5/3", d => d.First() != 0, (msg, data) =>
            {
                audio2.PlayEffect("door-creak.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/5/4", d => d.First() != 0, (msg, data) =>
            {
                audio2.PlayEffect("violin screech.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/5/5", d => d.First() != 0, (msg, data) =>
            {
                audio2.PlayEffect("gollum_precious1.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/4/1", d => d.First() != 0, (msg, data) =>
            {
                audio2.PlayNewEffect("640 The Demon Exorcised.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/4/2", d => d.First() != 0, (msg, data) =>
            {
                audioCat.PlayEffect("266 Monster Growl 7.wav", 1.0, 1.0);
            });

            oscServer.RegisterAction<int>("/3/multipush1/4/3", d => d.First() != 0, (msg, data) =>
            {
                audioCat.PlayEffect("285 Monster Snarl 2.wav", 1.0, 1.0);
            });

            oscServer.RegisterAction<int>("/3/multipush1/4/4", d => d.First() != 0, (msg, data) =>
            {
                audioCat.PlayEffect("286 Monster Snarl 3.wav", 1.0, 1.0);
            });

            oscServer.RegisterAction<int>("/3/multipush1/4/5", d => d.First() != 0, (msg, data) =>
            {
                audioCat.PlayEffect("287 Monster Snarl 4.wav", 1.0, 1.0);
            });

            oscServer.RegisterAction<int>("/3/multipush1/3/1", d => d.First() != 0, (msg, data) =>
            {
                audio2.PlayEffect("180 Babbling Lunatic.wav");
            });

            oscServer.RegisterAction<int>("/1/eStop", (msg, data) =>
            {
                emergencyStop.Control.OnNext(data.First() != 0);
            });

            oscServer.RegisterAction<int>("/1/push2", d => d.First() != 0, (msg, data) =>
            {
                audioCat.PlayEffect("285 Monster Snarl 2.wav", 1.0, 1.0);
            });

            oscServer.RegisterAction<int>("/1/push3", d => d.First() != 0, (msg, data) =>
            {
                audioCat.PlayEffect("286 Monster Snarl 3.wav", 1.0, 1.0);
            });

            oscServer.RegisterActionSimple<bool>("/ManualFader/x", (msg, data) =>
            {
                manualFader.Value = data;
            });

            oscServer.RegisterActionSimple<double>("/FaderR/x", (msg, data) =>
            {
                faderR.Value = data;
            });

            oscServer.RegisterActionSimple<double>("/FaderG/x", (msg, data) =>
            {
                faderG.Value = data;
            });

            oscServer.RegisterActionSimple<double>("/FaderB/x", (msg, data) =>
            {
                faderB.Value = data;
            });

            oscServer.RegisterActionSimple<double>("/FaderBrightness/x", (msg, data) =>
            {
                faderBright.Value = data;
            });

            oscServer.RegisterActionSimple<double>("/MasterVolume/x", (msg, data) =>
            {
                masterVolume.Value = data;
            });

            oscServer.RegisterActionSimple<int>("/FlashBaby/x", (msg, data) =>
            {
                // Flash
                if (data != 0)
                {
                    allLights.TakeAndHoldControl(100);
                    allLights.SetBrightness(1.0, new Data(DataElements.Color, Color.White));
                }
                else
                    allLights.ReleaseControl();
            });

            oscServer.RegisterAction<int>("/1/special1", (msg, data) =>
            {
                if (data.First() != 0)
                    stateMachine.GoToMomentaryState(States.Special1);
                else
                    stateMachine.StopCurrentJob();
            });

            oscServer.RegisterAction<int>("/1/push13", d => d.First() != 0, (msg, data) =>
            {
                //                Exec.MasterEffect.Fade(stairs1Light, 1.0, 0.0, 2000, token: testToken);
                //popOut1.Pop();
                //popOut2.Pop();
                //popOut3.Pop();
                popOutAll.Pop(color: Color.White);
            });

            oscServer.RegisterAction<int>("/1/toggle1", (msg, data) =>
            {
                //                candyEyes.Value = data.First() != 0;
                if (data.First() != 0)
                    audioHifi.PlayBackground();
                else
                    audioHifi.PauseBackground();
            });

            oscServer.RegisterAction<int>("/1/toggle2", (msg, data) =>
            {
                //                pinSpot.SetBrightness(data.First());
            });

            oscServer.RegisterAction<int>("/1/toggle3", (msg, data) =>
            {
                if (data.First() != 0)
                    audio2.PlayBackground();
                else
                    audio2.PauseBackground();
            });

            oscServer.RegisterAction<int>("/1/toggle4", (msg, data) =>
            {
                blockMaster.Value = data.First() != 0;
                //                treeGhosts.SetBrightness(data.First() != 0 ? 1.0 : 0.0);
            });

            oscServer.RegisterAction<int>("/1/push14", (msg, data) =>
            {
                //                flickerEffect.Start();
                //double brightness = data.First();

                //spiderLight.SetColor(Color.Red, brightness);
                //pinSpot.SetColor(Color.Purple, brightness);
                //underGeorge.SetBrightness(brightness);
                //wall1Light.SetColor(Color.Purple, brightness);
                //wall2Light.SetColor(Color.Purple, brightness);
                //wall3Light.SetColor(Color.Purple, brightness);
                //wall4Light.SetColor(Color.Purple, brightness);
                //                audioDIN.PlayEffect("gollum_precious1.wav");
            });

            oscServer.RegisterAction("/1", msg =>
            {
                log.Info("Page 1");
                //                manualFader.Value = false;

                SetManualColor();
            });

            oscServer.RegisterAction("/2", msg =>
            {
                log.Info("Page 2");
                //                manualFader.Value = true;

                SetManualColor();
            });

            oscServer.RegisterAction<float>("/2/faderBright", (msg, data) =>
            {
                faderBright.Value = data.First();

                SetManualColor();
            });

            oscServer.RegisterAction<float>("/2/faderR", (msg, data) =>
            {
                faderR.Value = data.First();

                SetManualColor();
            });

            oscServer.RegisterAction<float>("/2/faderG", (msg, data) =>
            {
                faderG.Value = data.First();

                SetManualColor();
            });

            oscServer.RegisterAction<float>("/2/faderB", (msg, data) =>
            {
                faderB.Value = data.First();

                SetManualColor();
            });
        }
    }
}
