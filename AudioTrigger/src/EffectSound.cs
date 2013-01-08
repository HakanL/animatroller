using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharpDX.Multimedia;
using SharpDX.XAudio2;
using System.Net;
using System.Net.Sockets;

namespace Animatroller.AudioTrigger
{
    public class EffectSound
    {
        private static Dictionary<string, EffectSound> loadedSounds = new Dictionary<string, EffectSound>(StringComparer.OrdinalIgnoreCase);

        public SoundStream Stream { get; private set; }
        public AudioBuffer Buffer { get; private set; }

        public EffectSound(string filename)
        {
            lock (loadedSounds)
            {
                EffectSound existingSound;
                if (loadedSounds.TryGetValue(filename, out existingSound))
                {
                    Stream = existingSound.Stream;
                    Buffer = existingSound.Buffer;
                    return;
                }
            }

            using (var fileStream = File.OpenRead(filename))
            {
                Stream = new SoundStream(fileStream);
                Buffer = new AudioBuffer
                {
                    Stream = Stream.ToDataStream(),
                    AudioBytes = (int)Stream.Length,
                    Flags = BufferFlags.EndOfStream
                };
                Stream.Close();
            }

            lock (loadedSounds)
            {
                loadedSounds[filename] = this;
            }
        }

        public static void DisposeAll()
        {
            lock (loadedSounds)
            {
                foreach (var kvp in loadedSounds)
                {
                    kvp.Value.Stream.Dispose();
                }
                loadedSounds.Clear();
            }
        }
    }
}
