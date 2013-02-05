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
            return new Effect.EffectAction.Action((zeroToOne, negativeOneToOne, oneToZeroToOne, forced, totalTicks) =>
                {
                    double brightness = zeroToOne.ScaleToMinMax(this.startBrightness, this.endBrightness);

                    setBrightnessAction.Invoke(brightness);
                });
        }

        public int? Iterations
        {
            get { return 1; }
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
            return new Effect.EffectAction.Action((zeroToOne, negativeOneToOne, oneToZeroToOne, forced, totalTicks) =>
            {
                double brightness = easeTransform.Transform(oneToZeroToOne)
                    .ScaleToMinMax(this.minBrightness, this.maxBrightness);

                setBrightnessAction.Invoke(brightness);
            });
        }

        public int? Iterations
        {
            // Forever
            get { return null; }
        }
    }

    public class Twinkle : IMasterBrightnessEffect
    {
        private double minBrightness;
        private double maxBrightness;

        public Twinkle(double minBrightness, double maxBrightness)
        {
            this.minBrightness = minBrightness;
            this.maxBrightness = maxBrightness;
        }

        public Effect.EffectAction.Action GetEffectAction(Action<double> setBrightnessAction)
        {
            return new Effect.EffectAction.Action((zeroToOne, negativeOneToOne, oneToZeroToOne, forced, totalTicks) =>
            {
                double brightness = zeroToOne.ScaleToMinMax(this.minBrightness, this.maxBrightness);

                setBrightnessAction.Invoke(brightness);
            });
        }

        public int? Iterations
        {
            get { return 1; }
        }
    }

    public class Shimmer : IMasterBrightnessEffect
    {
        private double minBrightness;
        private double maxBrightness;

        public Shimmer(double minBrightness, double maxBrightness)
        {
            this.minBrightness = minBrightness;
            this.maxBrightness = maxBrightness;
        }

        public Effect.EffectAction.Action GetEffectAction(Action<double> setBrightnessAction)
        {
            return new Effect.EffectAction.Action((zeroToOne, negativeOneToOne, oneToZeroToOne, forced, totalTicks) =>
            {
                setBrightnessAction.Invoke((totalTicks % 2) == 0 ? this.minBrightness : this.maxBrightness);
            });
        }

        public int? Iterations
        {
            get { return null; }
        }
    }

}
