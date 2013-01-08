using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Animatroller.Simulator.Extensions
{
    public static class FormExtensions
    {
        public static void UIThread(this System.Windows.Forms.Control form, MethodInvoker code)
        {
            if (form.InvokeRequired)
            {
                form.Invoke(code);
                return;
            }
            code.Invoke();
        }
    }
}
