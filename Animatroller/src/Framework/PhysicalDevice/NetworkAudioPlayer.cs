using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Animatroller.Framework.PhysicalDevice
{
    public class NetworkAudioPlayer : IPhysicalDevice
    {
        private Socket socket;
        private IPEndPoint sendToEndpoint;

        public NetworkAudioPlayer(string ip, int port)
        {
            Executor.Current.Register(this);

            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            sendToEndpoint = new IPEndPoint(IPAddress.Parse(ip), port);
        }

        private void SendCommand(string data)
        {
            string sendData = "!AUD:0," + data + "\r";

            var bytes = Encoding.ASCII.GetBytes(sendData);
            try
            {
                socket.SendTo(bytes, sendToEndpoint);
            }
            catch
            {
                // Ignore errors
            }
        }

        public NetworkAudioPlayer PlayBackground()
        {
            SendCommand("B,1");

            return this;
        }

        public NetworkAudioPlayer PauseBackground()
        {
            SendCommand("B,0");

            return this;
        }

        public NetworkAudioPlayer SetBackgroundVolume(byte volume)
        {
            SendCommand(string.Format("BV,{0}", volume));

            return this;
        }

        public NetworkAudioPlayer PlayEffect(string name)
        {
            SendCommand(string.Format("FX,{0}", name));

            return this;
        }

        public NetworkAudioPlayer CueTrack(string name)
        {
            SendCommand(string.Format("TC,{0}", name));

            return this;
        }

        public NetworkAudioPlayer PlayTrack()
        {
            SendCommand(string.Format("T,1"));

            return this;
        }

        public NetworkAudioPlayer PauseTrack()
        {
            SendCommand(string.Format("T,0"));

            return this;
        }

        public void SetInitialState()
        {
        }

        public string Name
        {
            get { return string.Empty; }
        }
    }
}
