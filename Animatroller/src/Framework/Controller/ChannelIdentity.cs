using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Animatroller.Framework.Controller
{
    public interface IChannelIdentity : IComparable
    {
        bool Equals(object obj);
        int GetHashCode();
        string ToString();
    }

    public class RGBChannelIdentity
    {
        public IChannelIdentity R { get; set; }
        public IChannelIdentity G { get; set; }
        public IChannelIdentity B { get; set; }

        public RGBChannelIdentity(
            IChannelIdentity r,
            IChannelIdentity g,
            IChannelIdentity b)
        {
            this.R = r;
            this.G = g;
            this.B = b;
        }
    }
}
