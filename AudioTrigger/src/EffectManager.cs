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
    public class EffectManager
    {
        private XAudio2 xaudio2;
        private Dictionary<WaveFormat, List<SourceVoice>> instances;
        private int maxInstances;
        private string soundPath;

        public EffectManager(XAudio2 xaudio2, int maxInstances, string soundPath)
        {
            this.xaudio2 = xaudio2;
            this.maxInstances = maxInstances;
            this.soundPath = soundPath;

            this.instances = new Dictionary<WaveFormat, List<SourceVoice>>();
        }

        public SourceVoice Play(string filename)
        {
            var effectSound = new EffectSound(Path.Combine(this.soundPath, filename));
            return Play(effectSound);
        }

        public SourceVoice Play(EffectSound sound)
        {
            var waveFormat = sound.Stream.Format;
            List<SourceVoice> voices;
            lock (instances)
            {
                if (!instances.TryGetValue(waveFormat, out voices))
                {
                    voices = new List<SourceVoice>();
                    instances.Add(waveFormat, voices);
                }

                // Clean non-playing source
                var voiceToDelete = new List<SourceVoice>();
                foreach (var voice in voices)
                {
                    if (voice.State.BuffersQueued == 0)
                        voiceToDelete.Add(voice);
                }
                voiceToDelete.ForEach(x =>
                    {
                        voices.Remove(x);
                        x.Stop();
                        x.DestroyVoice();
                        x.Dispose();
                    });

                if (voices.Count >= this.maxInstances)
                    // Too many instances
                    return null;

                var newVoice = new SourceVoice(this.xaudio2, waveFormat, true);
                newVoice.BufferEnd += newVoice_BufferEnd;
                voices.Add(newVoice);

                newVoice.SubmitSourceBuffer(sound.Buffer, sound.Stream.DecodedPacketsInfo);
                newVoice.Start();

                return newVoice;
            }
        }

        public bool AreAnyPlaying
        {
            get
            {
                lock (instances)
                {
                    foreach (var kvp in instances)
                    {
                        foreach (var voice in kvp.Value)
                        {
                            if (voice.State.BuffersQueued > 0)
                                return true;
                        }
                    }
                }

                return false;
            }
        }

        public void Dispose()
        {
            EffectSound.DisposeAll();
        }

        private void newVoice_BufferEnd(IntPtr obj)
        {
            Console.WriteLine(" => event received: end of buffer");
        }
    }
}
