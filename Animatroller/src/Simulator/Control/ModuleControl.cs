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
    public partial class ModuleControl : UserControl
    {
        public ModuleControl()
        {
            InitializeComponent();
        }

        [Bindable(true)]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        public override string Text
        {
            get
            {
                return label.Text;
            }
            set
            {
                label.Text = value;
                OnTextChanged(EventArgs.Empty);
            }
        }

        public System.Windows.Forms.Control ChildControl
        {
            get
            {
                return tableLayoutPanel.GetControlFromPosition(0, 0);
            }
            set
            {
                if(ChildControl != null)
                    throw new InvalidOperationException("Can only set once");

                tableLayoutPanel.Controls.Add(value);
                tableLayoutPanel.SetRow(value, 0);
                tableLayoutPanel.SetColumn(value, 0);

                value.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            }
        }

    }
}
