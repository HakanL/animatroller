using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.Expander
{
    public class LineManager
    {
        private StringBuilder buffer;

        public class LineReceivedEventArgs : EventArgs
        {
            public string LineData { get; private set; }
            public LineReceivedEventArgs(string lineData)
            {
                this.LineData = lineData;
            }
        }

        public event EventHandler<LineReceivedEventArgs> LineReceived;

        public LineManager()
        {
            this.buffer = new StringBuilder();
        }

        public void WriteNewData(string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            int idx;
            while ((idx = value.IndexOfAny(new char[] { '\n', '\r' })) > -1)
            {
                string lineData = buffer.ToString() + value.Substring(0, idx);
                buffer.Clear();
                idx++;
                while (idx < value.Length)
                {
                    if (value[idx] == '\n' || value[idx] == '\r')
                        idx++;
                    else
                        break;
                }
                value = value.Substring(idx);

                if (!string.IsNullOrEmpty(lineData))
                    RaiseLineReceived(lineData);
            }

            if (!string.IsNullOrEmpty(value))
                buffer.Append(value);
        }

        protected void RaiseLineReceived(string lineData)
        {
            var handler = LineReceived;
            if (handler != null)
                handler(this, new LineReceivedEventArgs(lineData));
        }
    }
}
