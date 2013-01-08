using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Animatroller.Simulator.Extensions;

namespace Animatroller.Simulator.Control
{
    public partial class StrobeBulb : Bulb.SimpleBulb
    {
        public StrobeBulb()
        {
            InitializeComponent();
        }

        public int StrobeDelayMS
        {
            get
            {
                if (timerStrobe.Enabled)
                    return timerStrobe.Interval;
                return 0;
            }
            set
            {
                if (value < 10)
                    timerStrobe.Interval = 10;
                else
                    timerStrobe.Interval = value;

                this.UIThread(delegate
                {
                    timerStrobe.Enabled = value > 0;
                });

                if (value == 0)
                    base.On = true;
            }
        }

        private void timerStrobe_Tick(object sender, EventArgs e)
        {
            base.On = !base.On;
        }
    }
}
