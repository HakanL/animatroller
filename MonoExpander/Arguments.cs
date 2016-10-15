using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerArgs;

namespace Animatroller.MonoExpander
{
    public class Arguments
    {
        [ArgShortcut("a")]
        [ArgDescription("Activate audio system")]
        public bool AudioSystem { get; set; }

        [ArgShortcut("v")]
        [ArgDescription("Activate video system")]
        public string VideoSystem { get; set; }

        [ArgShortcut("fs")]
        [ArgDescription("Path to file storage location")]
        public string FileStoragePath { get; set; }

        [ArgShortcut("bg")]
        [ArgDescription("Path to background tracks, relative or absolute")]
        public string BackgroundTracksPath { get; set; }

        [ArgShortcut("bgas")]
        [ArgDescription("Auto-start background tracks")]
        public bool BackgroundTrackAutoStart { get; set; }

        [ArgShortcut("sp0")]
        [ArgDescription("Serial port 0 port name")]
        public string SerialPort0 { get; set; }

        [ArgShortcut("sb0")]
        [ArgDescription("Serial port 0 baud rate (2400, 9600, etc)")]
        public int SerialPort0BaudRate { get; set; }

        [ArgShortcut("p")]
        [ArgDescription("Listen port")]
        public int ListenPort { get; set; }

        [ArgShortcut("s")]
        [ArgDescription("Animatroller Hostname:Port to connect to. Supports comma-separated entries")]
        public string[] Server
        {
            set
            {
                var servers = new List<System.Net.DnsEndPoint>();

                foreach (var entry in value)
                {
                    string[] parts = entry.Split(':');
                    if (parts.Length != 2)
                        throw new ArgumentException("Invalid server entry");

                    servers.Add(new System.Net.DnsEndPoint(parts[0], int.Parse(parts[1])));
                }

                Servers = servers.ToArray();
            }
        }

        [ArgIgnore]
        public System.Net.DnsEndPoint[] Servers { get; private set; }

        public Arguments()
        {
            Servers = new System.Net.DnsEndPoint[0];
        }
    }
}
