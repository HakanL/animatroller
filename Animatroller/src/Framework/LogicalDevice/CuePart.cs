using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.LogicalDevice
{
    public class CuePart<T>
    {
        private Cue parent;
        private int? fade;

        public T Destination { get; set; }

        public bool MoveInBlack { get; set; }

        public int FadeMs
        {
            get { return this.fade ?? this.parent.FadeMs; }
            set { this.fade = value; }
        }

        public double FadeS
        {
            get { return FadeMs * 1000; }
            set { FadeMs = (int)(value * 1000); }
        }

        public CuePart(Cue parent)
        {
            this.parent = parent;
        }
    }
}
