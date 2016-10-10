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
    internal partial class Halloween2016
    {
        public void ConfigureOSC()
        {
            oscServer.RegisterAction<int>("/3/multipush1/6/1", d => d.First() != 0, (msg, data) =>
            {
                audioEeebox.PlayEffect("sixthsense-deadpeople.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/6/2", d => d.First() != 0, (msg, data) =>
            {
                audioEeebox.PlayEffect("162 Blood Curdling Scream of Terror.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/6/3", d => d.First() != 0, (msg, data) =>
            {
                audioEeebox.PlayEffect("424 Coyote Howling.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/6/4", d => d.First() != 0, (msg, data) =>
            {
                audioEeebox.PlayEffect("125919__klankbeeld__horror-what-are-you-doing-here-cathedral.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/6/5", d => d.First() != 0, (msg, data) =>
            {
                audioEeebox.PlayEffect("242004__junkfood2121__fart-01.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/5/1", d => d.First() != 0, (msg, data) =>
            {
                audioEeebox.PlayEffect("death-scream.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/5/2", d => d.First() != 0, (msg, data) =>
            {
                audioEeebox.PlayEffect("scream.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/5/3", d => d.First() != 0, (msg, data) =>
            {
                audioEeebox.PlayEffect("door-creak.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/5/4", d => d.First() != 0, (msg, data) =>
            {
                audioEeebox.PlayEffect("violin screech.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/5/5", d => d.First() != 0, (msg, data) =>
            {
                audioEeebox.PlayEffect("gollum_precious1.wav");
            });

            oscServer.RegisterAction<int>("/3/multipush1/4/1", d => d.First() != 0, (msg, data) =>
            {
                audioEeebox.PlayNewEffect("640 The Demon Exorcised.wav");
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
                audioEeebox.PlayEffect("180 Babbling Lunatic.wav");
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

            oscServer.RegisterAction<int>("/1/push4", (msg, data) =>
            {
                // Flash
                if (data.First() != 0)
                {
                    allLights.TakeAndHoldControl();
                    allLights.SetBrightness(1.0, new Data(DataElements.Color, Color.White));
                }
                else
                    allLights.ReleaseControl();
            });

            oscServer.RegisterAction<int>("/1/push5", (msg, data) =>
            {
                george1.Value = data.First() != 0;
            });

            oscServer.RegisterAction<int>("/1/push6", (msg, data) =>
            {
                george2.Value = data.First() != 0;
            });

            oscServer.RegisterAction<int>("/1/push7", (msg, data) =>
            {
                popper.Value = data.First() != 0;
            });

            oscServer.RegisterAction<int>("/1/push8", d => d.First() != 0, (msg, data) =>
            {
                audioPop.PlayEffect("laugh.wav", 1.0, 0.0);
            });

            oscServer.RegisterAction<int>("/1/spiderEyes", (msg, data) =>
            {
                dropSpiderEyes.Value = data.First() != 0;
            });

            oscServer.RegisterAction<int>("/1/push10", d => d.First() != 0, (msg, data) =>
            {
                audioPop.PlayEffect("348 Spider Hiss.wav", 0.0, 1.0);
            });

            oscServer.RegisterAction<int>("/1/push11", (msg, data) =>
            {
                spiderCeilingDrop.Value = data.First() != 0;
            });

            oscServer.RegisterAction<int>("/1/push12", (msg, data) =>
            {
                fog.Value = data.First() != 0;
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
                    audioEeebox.PlayBackground();
                else
                    audioEeebox.PauseBackground();
            });

            oscServer.RegisterAction<int>("/1/toggle4", (msg, data) =>
            {
                block.Value = data.First() != 0;
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

            oscServer.RegisterAction<int>("/1/push20", d => d.First() != 0, (msg, data) =>
            {
                video3dfx.PlayVideo("PHA_Wraith_StartleScare_3DFX_H.mp4");
            });

            oscServer.RegisterAction<int>("/1/push21", d => d.First() != 0, (msg, data) =>
            {
                //               video2.PlayVideo("SkeletonSurprise_Door_Horz_HD.mp4");
                video2.PlayVideo("Beauty_Startler_TVHolo_Hor_HD.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/5/1", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("Beauty_Startler_TVHolo_Hor_HD.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/5/2", d => d.First() != 0, (msg, data) =>
            {
            });

            oscServer.RegisterAction<int>("/4/multipush2/5/3", d => d.First() != 0, (msg, data) =>
            {
            });

            oscServer.RegisterAction<int>("/4/multipush2/5/4", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("FearTheReaper_Door_Horz_HD.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/4/1", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("GatheringGhouls_Door_Horz_HD.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/4/2", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("Girl_Startler_TVHolo_Hor_HD.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/4/3", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("HeadOfHouse_Startler_TVHolo_Hor_HD.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/4/4", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("JitteryBones_Door_Horz_HD.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/3/1", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("PHA_Poltergeist_StartleScare_Holl_H.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/3/2", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("PHA_Siren_StartleScare_Holl_H.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/3/3", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("PHA_Spinster_StartleScare_Holl_H.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/3/4", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("PHA_Wraith_StartleScare_Holl_H.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/2/1", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("PopUpPanic_Door_Horz_HD.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/2/2", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("SkeletonSurprise_Door_Horz_HD.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/2/3", d => d.First() != 0, (msg, data) =>
            {
                video2.PlayVideo("Wraith_Startler_TVHolo_Hor_HD.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/2/4", d => d.First() != 0, (msg, data) =>
            {
            });

            oscServer.RegisterAction<int>("/4/multipush2/1/1", d => d.First() != 0, (msg, data) =>
            {
                video3dfx.PlayVideo("PHA_Wraith_StartleScare_3DFX_H.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/1/2", d => d.First() != 0, (msg, data) =>
            {
                video3dfx.PlayVideo("PHA_Spinster_StartleScare_3DFX_H.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/1/3", d => d.First() != 0, (msg, data) =>
            {
                video3dfx.PlayVideo("PHA_Siren_StartleScare_3DFX_H.mp4");
            });

            oscServer.RegisterAction<int>("/4/multipush2/1/4", d => d.First() != 0, (msg, data) =>
            {
                video3dfx.PlayVideo("PHA_Poltergeist_StartleScare_3DFX_H.mp4");
            });


            oscServer.RegisterAction("/1", msg =>
            {
                log.Info("Page 1");
                //                manualFader.Value = false;

                SetPixelColor();
            });

            oscServer.RegisterAction("/2", msg =>
            {
                log.Info("Page 2");
                //                manualFader.Value = true;

                SetPixelColor();
            });

            oscServer.RegisterAction<float>("/2/faderBright", (msg, data) =>
            {
                faderBright.Value = data.First();

                SetPixelColor();
            });

            oscServer.RegisterAction<float>("/2/faderR", (msg, data) =>
            {
                faderR.Value = data.First();

                SetPixelColor();
            });

            oscServer.RegisterAction<float>("/2/faderG", (msg, data) =>
            {
                faderG.Value = data.First();

                SetPixelColor();
            });

            oscServer.RegisterAction<float>("/2/faderB", (msg, data) =>
            {
                faderB.Value = data.First();

                SetPixelColor();
            });
        }
    }
}
