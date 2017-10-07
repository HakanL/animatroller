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
        Framework.Import.LevelsPlayback pumpkinPlayback = new Framework.Import.LevelsPlayback();
        GroupControlToken lockObject = null;

        public HalloweenMrPumpkin(
            Dimmer3 light,
            DigitalOutput2 air,
            AudioPlayer audioPlayer,
            [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            pulsatingLow.ConnectTo(light);
            pumpkinPlayback.SetOutput(light);

            OutputPower.Subscribe(x =>
            {
                air.SetValue(x, this.lockObject);

                if (x)
                {
                    this.lockObject?.Dispose();
                    this.lockObject = new GroupControlToken(new List<IOwnedDevice>()
                    {
                        air,
                        light
                    }, null, nameof(HalloweenGrumpyCat));

                    pulsatingLow.Start(token: this.lockObject);
                }
                else
                {
                    pulsatingLow.Stop();
                    this.lockObject?.Dispose();
                }
            });

            Seq.WhenExecuted
                .Execute(instance =>
                {
                    Executor.Current.LogMasterStatus(Name, true);

                    pulsatingLow.Stop();

                    audioPlayer.PlayEffect("Thriller2.wav", pumpkinPlayback);
                    pumpkinPlayback.Start();

                    instance.CancelToken.WaitHandle.WaitOne(40000);
                })
                .TearDown(instance =>
                {
                    pulsatingLow.Start();

                    Executor.Current.LogMasterStatus(Name, false);
                });
        }

        public void Dispose()
        {
            this.lockObject?.Dispose();
        }
    }
}
