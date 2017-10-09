using System;
using System.Collections.Generic;
using Effect = Animatroller.Framework.Effect;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Scenes.Modules
{
    public class HalloweenMrPumpkin : TriggeredSequence, IDisposable
    {
        Effect.Pulsating pulsatingLow = new Effect.Pulsating(S(4), 0.2, 0.5, false);
        Framework.Import.LevelsPlayback levelsPlayback = new Framework.Import.LevelsPlayback();
        GroupControlToken lockObject = null;

        public HalloweenMrPumpkin(
            Dimmer3 light,
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
                    }, null, nameof(HalloweenGrumpyCat));

                    air.SetValue(true, this.lockObject);
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

                    audioPlayer.PlayEffect("125919__klankbeeld__horror-what-are-you-doing-here-cathedral.wav", levelsPlayback);
                    levelsPlayback.Start(this.lockObject);

                    instance.CancelToken.WaitHandle.WaitOne(10000);
                })
                .TearDown(instance =>
                {
                    pulsatingLow.Start(token: this.lockObject);

                    Executor.Current.LogMasterStatus(Name, false);
                });
        }

        public void Dispose()
        {
            this.lockObject?.Dispose();
        }
    }
}
