using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.LogicalDevice
{
    public class Cue
    {
        public enum CueParts
        {
            Intensity,
            Color,
            Pan,
            Tilt
        }

        public enum Triggers
        {
            Go,
            Follow,
            CueListTime
        }

        public Preset Preset
        {
            set
            {
                if (value.Brightness.HasValue)
                {
                    this.PartIntensity = new CuePart<double>(this)
                    {
                        Destination = value.Brightness.Value
                    };
                }

                if (value.Color.HasValue)
                {
                    this.PartColor = new CuePart<Color>(this)
                    {
                        Destination = value.Color.Value
                    };
                }

                if (value.Pan.HasValue)
                {
                    this.PartPan = new CuePart<double>(this)
                    {
                        Destination = value.Pan.Value
                    };
                }

                if (value.Tilt.HasValue)
                {
                    this.PartTilt = new CuePart<double>(this)
                    {
                        Destination = value.Tilt.Value
                    };
                }
            }
        }

        public Triggers Trigger { get; set; }

        public int TriggerTimeMs { get; set; }

        public double TriggerTimeS
        {
            get { return TriggerTimeMs * 1000; }
            set { TriggerTimeMs = (int)(value * 1000); }
        }


        public int FadeMs { get; set; }

        public double FadeS
        {
            get { return FadeMs * 1000; }
            set { FadeMs = (int)(value * 1000); }
        }

        public double Intensity
        {
            get
            {
                if (this.PartIntensity == null)
                    return 0.0;

                return this.PartIntensity.Destination;
            }
            set
            {
                this.PartIntensity = new CuePart<double>(this)
                {
                    Destination = value
                };
            }
        }

        public Color Color
        {
            get
            {
                if (this.PartColor == null)
                    return Color.Black;

                return this.PartColor.Destination;
            }
            set
            {
                this.PartColor = new CuePart<Color>(this)
                {
                    Destination = value
                };
            }
        }

        public double Pan
        {
            get
            {
                if (this.PartPan == null)
                    return 0.0;

                return this.PartPan.Destination;
            }
            set
            {
                this.PartPan = new CuePart<double>(this)
                {
                    Destination = value
                };
            }
        }

        public double Tilt
        {
            get
            {
                if (this.PartTilt == null)
                    return 0.0;

                return this.PartTilt.Destination;
            }
            set
            {
                this.PartTilt = new CuePart<double>(this)
                {
                    Destination = value
                };
            }
        }

        public CuePart<double> PartIntensity { get; set; }

        public CuePart<Color> PartColor { get; set; }

        public CuePart<double> PartPan { get; set; }

        public CuePart<double> PartTilt { get; set; }

        public List<ILogicalDevice> Devices { get; set; }

        public ILogicalDevice AddDevice
        {
            set
            {
                Devices.Add(value);
            }
        }

        public Cue()
        {
            Devices = new List<ILogicalDevice>();
            Trigger = Triggers.Go;
        }
    }
}
