using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Animatroller.Simulator.Control
{
    public partial class CenterControl : UserControl
    {
        public CenterControl()
        {
            InitializeComponent();
        }

        public System.Windows.Forms.Control ChildControl
        {
            get
            {
                return tableLayoutPanel.GetControlFromPosition(1, 1);
            }
            set
            {
                if(ChildControl != null)
                    throw new InvalidOperationException("Can only set once");

                tableLayoutPanel.Controls.Add(value);
                tableLayoutPanel.SetRow(value, 1);
                tableLayoutPanel.SetColumn(value, 1);
            }
        }

    }
}
