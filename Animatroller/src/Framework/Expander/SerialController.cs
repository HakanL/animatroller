using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Threading.Tasks;
using NLog;

namespace Animatroller.Framework.Expander
{
    public abstract class SerialController : IRunnable
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();
        private object lockObject = new object();
        private int counter = 0;
        private LineManager lineManager;
        private SerialPort serialPort;
        private byte address;

        public SerialController(string portName, byte address)
        {
            this.address = address;
            this.serialPort = new SerialPort(portName, 38400);

            this.lineManager = new LineManager();
            this.lineManager.LineReceived += lineManager_LineReceived;

            this.serialPort.DataReceived += serialPort_DataReceived;
        }

        protected abstract void CommandReceived(string data);
 
        private void lineManager_LineReceived(object sender, LineManager.LineReceivedEventArgs e)
        {
            if (e.LineData.StartsWith(string.Format("!IOX:{0}", address)))
            {
                // Matches
                var data = e.LineData.Substring(7);
                if (data.Length > 0)
                {
                    CommandReceived(data);
                }
            }
            else
                log.Warn("Received unknown data: {0}", e.LineData);
        }

        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                var sp = (SerialPort)sender;

                if (sp.BytesToRead == 0)
                    return;

                this.lineManager.WriteNewData(sp.ReadExisting());
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception in serialPort_DataReceived: {0}", ex.Message);
            }
        }

        protected void SendRaw(byte[] rawData)
        {
            counter++;
//            string debugData = string.Join(",", rawData.Select(x => x.ToString()));
//            log.Info("Sending ({0}): {1}", counter, debugData);

            lock (lockObject)
            {
                try
                {
                    serialPort.Write(rawData, 0, rawData.Length);
                }
                catch (Exception ex)
                {
                    log.Info("SendRaw exception: " + ex.Message);
                    // Ignore
                }
            }
        }

        protected void SendSerialCommand(string data)
        {
            SendSerialCommand(Encoding.ASCII.GetBytes(data));
        }

        protected void SendSerialCommand(byte[] data)
        {
            byte[] fullData = new byte[data.Length + 2];
            Buffer.BlockCopy(data, 0, fullData, 1, data.Length);
            fullData[0] = 33;
            fullData[fullData.Length - 1] = 13;

            SendRaw(fullData);
        }

        protected void SendSerialCommand(byte cmd, byte[] data)
        {
            byte[] fullData = new byte[data.Length + 3];
            Buffer.BlockCopy(data, 0, fullData, 2, data.Length);
            fullData[0] = 33;
            fullData[1] = cmd;
            fullData[fullData.Length - 1] = 13;

            SendRaw(fullData);
        }

        protected void SendSerialCommand(byte cmd, byte channel, byte[] data)
        {
            byte[] fullData = new byte[data.Length + 4];
            Buffer.BlockCopy(data, 0, fullData, 3, data.Length);
            fullData[0] = 33;
            fullData[1] = cmd;
            fullData[2] = channel;
            fullData[fullData.Length - 1] = 13;

            SendRaw(fullData);
        }

        public virtual void Start()
        {
            serialPort.Open();

            SendSerialCommand("!");
        }

        public virtual void Run()
        {
        }

        public virtual void Stop()
        {
            serialPort.Close();
        }
    }
}
