using System;
using System.Collections.Generic;
using Effect = Animatroller.Framework.Effect;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;
using System.Drawing;

namespace Animatroller.Scenes.Modules
{
    public class HalloweenFrankGhost : TriggeredSequence, IDisposable
    {
        Effect.Pulsating pulsatingLow = new Effect.Pulsating(S(4), 0.2, 0.5, false);
        Framework.Import.LevelsPlayback levelsPlayback = new Framework.Import.LevelsPlayback();
        GroupControlToken lockObject = null;

        public HalloweenFrankGhost(
            IReceivesColor light,
            DigitalOutput2 air,
            AudioPlayer audioPlayer,
            [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            pulsatingLow.ConnectTo(light);
            levelsPlayback.SetOutput(light);

            OutputPower.Subscribe(x =>
            {
                if (x)
                {
                    this.lockObject?.Dispose();
                    this.lockObject = new GroupControlToken(new List<IOwnedDevice>()
                    {
                        air,
                        light
                    }, null, nameof(HalloweenFrankGhost));

                    air.SetValue(true, this.lockObject);
                    light.SetColor(Color.Red, null, this.lockObject);
                    pulsatingLow.Start(token: this.lockObject);
                }
                else
                {
                    air.SetValue(false, this.lockObject);
                    pulsatingLow.Stop();
                    this.lockObject?.Dispose();
                }
            });

            PowerOnSeq.WhenExecuted
                .Execute(instance =>
                {
                    Executor.Current.LogMasterStatus(Name, true);

                    pulsatingLow.Stop();

                    audioPlayer.PlayEffect("Thriller2.wav", levelsPlayback);
                    light.SetColor(Color.Purple, null, this.lockObject);
                    var cts = levelsPlayback.Start(this.lockObject);

                    instance.CancelToken.WaitHandle.WaitOne(45000);
                    cts.Cancel();
                })
                .TearDown(instance =>
                {
                    light.SetColor(Color.Red, null, this.lockObject);
                    pulsatingLow.Start(token: this.lockObject);

                    Executor.Current.LogMasterStatus(Name, false);
                });

            PowerOffSeq.WhenExecuted
                .Execute(instance =>
                {
                    audioPlayer.PlayEffect("Happy Halloween.wav", 0.15);
                    instance.CancelToken.WaitHandle.WaitOne(5000);
                });
        }

        public void Dispose()
        {
            this.lockObject?.Dispose();
        }
    }
}
