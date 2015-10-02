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
        [ArgShortcut("bg")]
        [ArgDescription("Path to background tracks, relative or absolute")]
        public string BackgroundTracksPath { get; set; }

        [ArgShortcut("fx")]
        [ArgDescription("Path to sound effects, relative or absolute")]
        public string SoundEffectPath { get; set; }

        [ArgShortcut("trk")]
        [ArgDescription("Path to tracks, relative or absolute")]
        public string TrackPath { get; set; }

        [ArgRequired()]
        [ArgRange(1, 65535)]
        [ArgShortcut("ol")]
        [ArgDescription("Port to listen for OSC commands")]
        public int OscListenPort { get; set; }

        [ArgShortcut("bgas")]
        [ArgDescription("Auto-start background tracks")]
        public bool BackgroundTrackAutoStart { get; set; }

        [ArgShortcut("os")]
        [ArgDescription("OSC Hostname:Port to send OSC commands to. Supports comma-separated entries")]
        public string[] OscServer
        {
            set
            {
                var servers = new List<System.Net.IPEndPoint>();

                foreach (var entry in value)
                {
                    string[] parts = entry.Split(':');
                    if (parts.Length != 2)
                        throw new ArgumentException("Invalid server entry");

                    servers.Add(new System.Net.IPEndPoint(System.Net.IPAddress.Parse(parts[0]), int.Parse(parts[1])));
                }

                OscServers = servers.ToArray();
            }
        }

        [ArgIgnore]
        public System.Net.IPEndPoint[] OscServers { get; private set; }
    }
}
