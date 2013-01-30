using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.Effect
{
    public interface IEffect : IOwner
    {
        IEffect Start();
        IEffect Stop();
    }

    public interface ITransformer
    {
        double Transform(double input);
    }

    public interface IMasterEffect
    {
        bool OneShot { get; }
    }

    public interface IMasterBrightnessEffect : IMasterEffect
    {
        Effect.EffectAction.Action GetEffectAction(Action<double> setBrightnessAction);
    }
}
