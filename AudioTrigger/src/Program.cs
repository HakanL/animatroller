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
    public class Program
    {
        private static TrackPlayer backgroundPlayer;
        private static TrackPlayer trackPlayer;
        private static float backgroundVolume = 0.5f;
        private static EffectManager effectManager;
        private static bool autoMuteBackground = true;
        private static XAudio2 xaudio2;


        private static void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                var client = result.AsyncState as UdpClient;

                var endpoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.EndReceive(result, ref endpoint);
                if (data != null && data.Length > 0)
                {
                    string text = Encoding.ASCII.GetString(data);
                    if (text.StartsWith("!AUD:0,"))
                    {
                        text = text.Substring(7).Trim();

                        string[] parts = text.Split(',');
                        if (parts.Length >= 2)
                        {
                            switch (parts[0])
                            {
                                case "B":
                                    // Background music
                                    if (int.Parse(parts[1]) == 1)
                                        backgroundPlayer.Resume();
                                    else
                                        backgroundPlayer.Pause();
                                    break;
                                case "BV":
                                    // Background volume
                                    int vol = int.Parse(parts[1]);
                                    if (vol >= 0 && vol <= 255)
                                        backgroundVolume = (float)vol / 255.0f;
                                    break;
                                case "FX":
                                    try
                                    {
                                        effectManager.Play(parts[1] + ".wav");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("Exception when playing FX: " + ex.ToString());
                                        // Ignore
                                    }
                                    break;

                                case "TC":
                                    // Cue track
                                    if (trackPlayer != null)
                                    {
                                        trackPlayer.Stop();
                                        trackPlayer = null;
                                    }
                                    try
                                    {
                                        string filename = Path.Combine(Properties.Settings.Default.TracksPath,
                                            parts[1] + ".wav");

                                        trackPlayer = new TrackPlayer(xaudio2, new string[] { filename });
                                        trackPlayer.Prepare();
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("Exception when playing Track: " + ex.ToString());
                                        // Ignore
                                    }
                                    break;

                                case "T":
                                    // Track
                                    if (trackPlayer != null)
                                    {
                                        if (int.Parse(parts[1]) == 1)
                                            trackPlayer.Resume();
                                        else
                                            trackPlayer.Pause();
                                    }
                                    break;
                            }
                        }
                    }
                }

                client.BeginReceive(new AsyncCallback(ReceiveCallback), client);
            }
            catch
            {
                // Ignore
            }
        }

        public static void Main(string[] args)
        {
            xaudio2 = new XAudio2();
            xaudio2.StartEngine();
            var masteringVoice = new MasteringVoice(xaudio2);

            if (!string.IsNullOrEmpty(Properties.Settings.Default.BackgroundMusicPath) &&
                Directory.Exists(Properties.Settings.Default.BackgroundMusicPath))
            {
                var musicFiles = Directory.GetFiles(Properties.Settings.Default.BackgroundMusicPath, "*.wav");
                if(musicFiles.Length > 0)
                    backgroundPlayer = new TrackPlayer(xaudio2, musicFiles);
            }

            var listener = new UdpClient(10009);
            listener.BeginReceive(new AsyncCallback(ReceiveCallback), listener);

            effectManager = new EffectManager(xaudio2, 4, Properties.Settings.Default.FXPath);

            // Wait until its done
            int count = 1;
            while (true)
            {
                Thread.Sleep(10);

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey();
                    if (key.Key == ConsoleKey.Escape)
                        break;

                    switch (key.Key)
                    {
                        case ConsoleKey.A:
                            effectManager.Play("Scream.wav");
                            break;
                        case ConsoleKey.B:
                            effectManager.Play("Violin screech.wav");
                            break;
                        case ConsoleKey.N:
                            if(backgroundPlayer != null)
                                backgroundPlayer.NextTrack();
                            break;
                        case ConsoleKey.V:
                            if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
                                backgroundVolume -= 0.1f;
                            else
                                backgroundVolume += 0.1f;

                            if (backgroundVolume < 0f)
                                backgroundVolume = 0f;
                            if (backgroundVolume > 1f)
                                backgroundVolume = 1f;
                            break;
                    }
                }

                var muteMusic = effectManager.AreAnyPlaying && autoMuteBackground ? 0.2f : 0f;
                if (backgroundPlayer != null)
                    backgroundPlayer.Volume = backgroundVolume - muteMusic;

                if (count % 50 == 0)
                {
                    Console.Write(".");
                    Console.Out.Flush();
                }

                Thread.Sleep(10);
                count++;
            }

            listener.Close();

            if (backgroundPlayer != null)
                backgroundPlayer.Stop();
            if (trackPlayer != null)
                trackPlayer.Stop();

            effectManager.Dispose();

            Thread.Sleep(500);

            masteringVoice.Dispose();
            xaudio2.StopEngine();
            xaudio2.Dispose();
        }
    }
}
