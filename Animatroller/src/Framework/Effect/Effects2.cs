using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Serilog;
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
            return new Effect.EffectAction.Action((zeroToOne, negativeOneToOne, zeroToOneToZero, forced, totalTicks, final) =>
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
        private int? iterations;

        public Pulse(double minBrightness, double maxBrightness, int? iterations)
        {
            this.minBrightness = minBrightness;
            this.maxBrightness = maxBrightness;
            this.iterations = iterations;
        }

        public Pulse(double minBrightness, double maxBrightness)
            : this(minBrightness, maxBrightness, null)
        {
        }

        public Effect.EffectAction.Action GetEffectAction(Action<double> setBrightnessAction)
        {
            return new Effect.EffectAction.Action((zeroToOne, negativeOneToOne, zeroToOneToZero, forced, totalTicks, final) =>
            {
                double brightness = easeTransform.Transform(zeroToOneToZero)
                    .ScaleToMinMax(this.minBrightness, this.maxBrightness);

                setBrightnessAction.Invoke(brightness);
            });
        }

        public int? Iterations
        {
            get { return this.iterations; }
        }
    }

    // Is this the same as Fader?
    public class Pop : IMasterBrightnessEffect
    {
        private Effect.Transformer.EaseOut easeTransform = new Effect.Transformer.EaseOut();
        private double startBrightness;
        private double endBrightness;
        private int? iterations;

        public Pop(double startBrightness, double endBrightness, int? iterations)
        {
            this.startBrightness = startBrightness;
            this.endBrightness = endBrightness;
            this.iterations = iterations;
        }

        public Pop(double startBrightness, double endBrightness)
            : this(startBrightness, endBrightness, null)
        {
        }

        public Effect.EffectAction.Action GetEffectAction(Action<double> setBrightnessAction)
        {
            return new Effect.EffectAction.Action((zeroToOne, negativeOneToOne, zeroToOneToZero, forced, totalTicks, final) =>
            {
                double brightness = easeTransform.Transform(zeroToOne)
                    .ScaleToMinMax(this.startBrightness, this.endBrightness);

                setBrightnessAction.Invoke(brightness);
            });
        }

        public int? Iterations
        {
            get { return this.iterations; }
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
            return new Effect.EffectAction.Action((zeroToOne, negativeOneToOne, zeroToOneToZero, forced, totalTicks, final) =>
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
            return new Effect.EffectAction.Action((zeroToOne, negativeOneToOne, zeroToOneToZero, forced, totalTicks, final) =>
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
