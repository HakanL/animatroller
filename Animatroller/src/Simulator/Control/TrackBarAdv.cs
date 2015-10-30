using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Animatroller.Simulator.Control
{
    public class TrackBarAdv : TrackBar
    {
        public bool SuspendChangedEvents { get; set; }

        protected override void OnValueChanged(EventArgs e)
        {
            if (!SuspendChangedEvents)
                base.OnValueChanged(e);
        }
    }
}
