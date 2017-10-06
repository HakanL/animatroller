using System;
using System.Collections.Generic;
using Effect = Animatroller.Framework.Effect;
using Animatroller.Framework;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Scenes.Modules
{
    public class HalloweenGrumpyCat : TriggeredSequence, IDisposable
    {
        Effect.Pulsating pulsatingCatLow = new Effect.Pulsating(S(4), 0.2, 0.5, false);
        Effect.Pulsating pulsatingCatHigh = new Effect.Pulsating(S(2), 0.5, 1.0, false);
        GroupControlToken lockObject = null;

        public HalloweenGrumpyCat(
            Dimmer3 catLights,
            DigitalOutput2 catAir,
            AudioPlayer audioPlayer,
            [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.lockObject = new GroupControlToken(new List<IOwnedDevice>()
            {
                catAir,
                catLights
            }, null, nameof(HalloweenGrumpyCat));

            pulsatingCatLow.ConnectTo(catLights);
            pulsatingCatHigh.ConnectTo(catLights);

            OutputPower.Subscribe(x =>
            {
                catAir.SetValue(x, this.lockObject);

                if (x)
                {
                    pulsatingCatLow.Start(token: this.lockObject);
                }
                else
                {
                    pulsatingCatLow.Stop();
                }
            });

            Seq.WhenExecuted
                .Execute(instance =>
                {
                    Executor.Current.LogMasterStatus(Name, true);

                    var maxRuntime = System.Diagnostics.Stopwatch.StartNew();

                    pulsatingCatLow.Stop();
                    pulsatingCatHigh.Start(token: this.lockObject);

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
                    pulsatingCatHigh.Stop();
                    pulsatingCatLow.Start(token: this.lockObject);

                    Executor.Current.LogMasterStatus(Name, false);
                });
        }

        public void Dispose()
        {
            this.lockObject?.Dispose();
        }
    }
}
