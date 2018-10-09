using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.Effect
{
    public interface IEffect : IOwner
    {
        IEffect Start(IChannel channel = null, int priority = 1, IControlToken token = null);

        IEffect Stop();
    }

    public interface ITransformer
    {
        double Transform(double input);
    }

    public interface IMasterEffect
    {
        int? Iterations { get; }
    }

    public interface IMasterBrightnessEffect : IMasterEffect
    {
        Effect.EffectAction.Action GetEffectAction(Action<double> setBrightnessAction);
    }
}
