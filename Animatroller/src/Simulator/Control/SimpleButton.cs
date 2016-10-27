using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Animatroller.Simulator.Control
{
    public class SimpleButton : CheckBox
    {
        public SimpleButton()
        {
            Appearance = Appearance.Button;
            FlatStyle = FlatStyle.System;
        }

        protected override void InitLayout()
        {
            base.InitLayout();
        }

        protected override bool ShowFocusCues
        {
            get
            {
                return false;
            }
        }
    }
}
