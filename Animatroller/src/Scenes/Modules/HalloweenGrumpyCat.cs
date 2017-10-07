using System;
using System.Collections.Generic;
using Effect = Animatroller.Framework.Effect;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Scenes.Modules
{
    public class HalloweenGrumpyCat : TriggeredSequence, IDisposable
    {
        Effect.Pulsating pulsatingLow = new Effect.Pulsating(S(4), 0.2, 0.5, false);
        Effect.Pulsating pulsatingHigh = new Effect.Pulsating(S(2), 0.5, 1.0, false);
        GroupControlToken lockObject = null;

        public HalloweenGrumpyCat(
            Dimmer3 light,
            DigitalOutput2 air,
            AudioPlayer audioPlayer,
            [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            pulsatingLow.ConnectTo(light);
            pulsatingHigh.ConnectTo(light);

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

                    var maxRuntime = System.Diagnostics.Stopwatch.StartNew();

                    pulsatingLow.Stop();
                    pulsatingHigh.Start(token: this.lockObject);

                    while (true)
                    {
                        switch (random.Next(4))
                        {
                            case 0:
                                audioPlayer.PlayEffect("266 Monster Growl 7.wav", 1.0, 1.0);
                                instance.WaitFor(TimeSpan.FromSeconds(2.0));
                                break;
                            case 1:
                                audioPlayer.PlayEffect("285 Monster Snarl 2.wav", 1.0, 1.0);
                                instance.WaitFor(TimeSpan.FromSeconds(3.0));
                                break;
                            case 2:
                                audioPlayer.PlayEffect("286 Monster Snarl 3.wav", 1.0, 1.0);
                                instance.WaitFor(TimeSpan.FromSeconds(2.5));
                                break;
                            case 3:
                                audioPlayer.PlayEffect("287 Monster Snarl 4.wav", 1.0, 1.0);
                                instance.WaitFor(TimeSpan.FromSeconds(1.5));
                                break;
                            default:
                                instance.WaitFor(TimeSpan.FromSeconds(3.0));
                                break;
                        }

                        instance.CancelToken.ThrowIfCancellationRequested();

                        if (maxRuntime.Elapsed.TotalSeconds > 10)
                            break;
                    }
                })
                .TearDown(instance =>
                {
                    //TODO: Fade out
                    pulsatingHigh.Stop();
                    pulsatingLow.Start(token: this.lockObject);

                    Executor.Current.LogMasterStatus(Name, false);
                });

            PowerOffSeq.WhenExecuted
                .Execute(instance =>
                {
                    audioPlayer.PlayEffect("How you doing.wav", 0.15);
                    instance.CancelToken.WaitHandle.WaitOne(5000);
                });
        }

        public void Dispose()
        {
            this.lockObject?.Dispose();
        }
    }
}
