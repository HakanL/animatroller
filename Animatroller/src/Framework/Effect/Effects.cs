using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reactive.Subjects;
using Animatroller.Framework.Extensions;
using NLog;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.Effect
{
    public class Pulsating : BaseSweeperEffect
    {
        private Transformer.EaseInOut easeTransform = new Transformer.EaseInOut();
        private double minBrightness;
        private double maxBrightness;

        public Pulsating(TimeSpan sweepDuration, double minBrightness, double maxBrightness, bool startRunning = true, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(sweepDuration, startRunning, name)
        {
            this.minBrightness = minBrightness;
            this.maxBrightness = maxBrightness;
        }

        public Pulsating(TimeSpan sweepDuration, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : this(sweepDuration, 0, 1, true, name)
        {
        }

        public double MinBrightness
        {
            get { return this.minBrightness; }
            set { this.minBrightness = value.Limit(0, 1); }
        }

        public double MaxBrightness
        {
            get { return this.maxBrightness; }
            set { this.maxBrightness = value.Limit(0, 1); }
        }

        protected override double GetValue(double zeroToOne, double negativeOneToOne, double zeroToOneToZero, bool final)
        {
            if (final)
                return 0;

            return easeTransform.Transform(zeroToOneToZero)
                .ScaleToMinMax(this.minBrightness, this.maxBrightness);
        }
    }

    public class Fader : BaseSweeperEffect
    {
        private Transformer.EaseInOut easeTransform = new Transformer.EaseInOut();
        private double minBrightness;
        private double maxBrightness;

        public Fader(TimeSpan sweepDuration, double minBrightness, double maxBrightness, bool startRunning = true, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(sweepDuration, startRunning, name)
        {
            this.minBrightness = minBrightness;
            this.maxBrightness = maxBrightness;
            base.sweeper.OneShot();
        }

        public Fader(TimeSpan sweepDuration, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : this(sweepDuration, 0, 1, true, name)
        {
        }

        public double MinBrightness
        {
            get { return this.minBrightness; }
            set { this.minBrightness = value; }
        }

        public double MaxBrightness
        {
            get { return this.maxBrightness; }
            set { this.maxBrightness = value; }
        }

        protected override double GetValue(double zeroToOne, double negativeOneToOne, double zeroToOneToZero, bool final)
        {
            return zeroToOne.ScaleToMinMax(this.minBrightness, this.maxBrightness);
        }
    }

    public class PopOut : BaseSweeperEffect
    {
        private double startBrightness;

        public PopOut(TimeSpan sweepDuration, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(sweepDuration, false, name)
        {
            base.sweeper.OneShot();
        }

        public PopOut(TimeSpan sweepDuration, int dataPoints, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(sweepDuration, dataPoints, false, name)
        {
            base.sweeper.OneShot();
        }

        public PopOut Pop(double startBrightness)
        {
            this.startBrightness = startBrightness;

            base.sweeper.Reset();

            return this;
        }

        protected override double GetValue(double zeroToOne, double negativeOneToOne, double zeroToOneToZero, bool final)
        {
            if (final)
                return 0;

            double brightness = this.startBrightness * (1 - zeroToOne);

            if (brightness < 0.1)
                brightness = 0;

            return brightness;
        }
    }
}
