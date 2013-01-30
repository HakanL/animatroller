using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using Animatroller.Framework.Effect;
using Animatroller.Framework.Extensions;

namespace Animatroller.Framework.Effect2
{
    public class Fader : IMasterBrightnessEffect
    {
        private double startBrightness;
        private double endBrightness;

        public Fader(double startBrightness, double endBrightness)
        {
            this.startBrightness = startBrightness;
            this.endBrightness = endBrightness;
        }

        public Effect.EffectAction.Action GetEffectAction(Action<double> setBrightnessAction)
        {
            return new Effect.EffectAction.Action((zeroToOne, negativeOneToOne, oneToZeroToOne, forced) =>
                {
                    double brightness = zeroToOne.ScaleToMinMax(this.startBrightness, this.endBrightness);

                    setBrightnessAction.Invoke(brightness);
                });
        }

        public bool OneShot
        {
            get { return true; }
        }
    }

    public class Pulse : IMasterBrightnessEffect
    {
        private Effect.Transformer.EaseInOut easeTransform = new Effect.Transformer.EaseInOut();
        private double minBrightness;
        private double maxBrightness;

        public Pulse(double minBrightness, double maxBrightness)
        {
            this.minBrightness = minBrightness;
            this.maxBrightness = maxBrightness;
        }

        public Effect.EffectAction.Action GetEffectAction(Action<double> setBrightnessAction)
        {
            return new Effect.EffectAction.Action((zeroToOne, negativeOneToOne, oneToZeroToOne, forced) =>
            {
                double brightness = easeTransform.Transform(oneToZeroToOne)
                    .ScaleToMinMax(this.minBrightness, this.maxBrightness);

                setBrightnessAction.Invoke(brightness);
            });
        }

        public bool OneShot
        {
            get { return false; }
        }
    }
}
