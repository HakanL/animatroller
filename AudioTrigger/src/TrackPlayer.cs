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
    public class TrackPlayer
    {
        protected class AudioPlayer
        {
            public AudioPlayerApp.AudioPlayer Player { get; private set; }
            public Stream FileStream { get; private set; }

            public AudioPlayer(XAudio2 xaudio2, Stream stream)
            {
                this.FileStream = stream;
                this.Player = new AudioPlayerApp.AudioPlayer(xaudio2, stream);
            }
        }

        private Random rnd;
        private string[] filenames;
        private string currentTrack;
        private List<AudioPlayer> players;
        private XAudio2 xaudio2;
        private bool playing;

        public TrackPlayer(XAudio2 xaudio2, string[] filenames)
        {
            if (filenames.Length == 0)
                throw new ArgumentException("No filenames specified");

            this.xaudio2 = xaudio2;
            this.filenames = filenames;

            this.rnd = new Random();
            this.players = new List<AudioPlayer>();
        }

        private string GetNextTrack()
        {
            if (this.filenames.Length == 1)
                return this.filenames[0];

            while (true)
            {
                int index = rnd.Next(this.filenames.Length);

                if (filenames[index].Equals(currentTrack))
                    continue;

                currentTrack = filenames[index];

                Console.WriteLine("Now playing {0}", Path.GetFileName(currentTrack));

                return currentTrack;
            }
        }

        private void CrossFadeStarting(object sender, EventArgs e)
        {
            PlayNextTrackInNewPlayer(true);
        }

        private void PlayNextTrackInNewPlayer(bool startPlaying)
        {
            if (!playing && startPlaying)
                return;

            if (players.Count > 1)
            {
                // Wait for 1st player
                var firstPlayer = players.FirstOrDefault();
                if (firstPlayer != null)
                {
                    firstPlayer.Player.Wait();
                }
            }

            string nextTrack = GetNextTrack();

            lock (players)
            {
                var newPlayer = new AudioPlayer(this.xaudio2, File.OpenRead(nextTrack));
                players.Add(newPlayer);
                newPlayer.Player.AutoCloseAtEndOfSong = true;
                newPlayer.Player.CrossFade = TimeSpan.FromSeconds(10);
                newPlayer.Player.CrossFadeStarting += new EventHandler(CrossFadeStarting);
                newPlayer.Player.StateChanged += new EventHandler<AudioPlayerApp.AudioPlayer.StateEventArgs>(StateChanged);
                if(startPlaying)
                    newPlayer.Player.Play();
            }
        }

        public float Volume
        {
            get
            {
                var player = players.LastOrDefault();
                if (player != null)
                    return player.Player.Volume;
                return 0f;
            }
            set
            {
                var player = players.LastOrDefault();
                if (player != null)
                    player.Player.Volume = value;
            }
        }

        public void Pause()
        {
            lock (players)
            {
                var playersCopy = players.ToList();
                foreach (var player in playersCopy)
                    player.Player.Pause();
            }
        }

        public void Resume()
        {
            if (!playing)
            {
                Play();
                return;
            }

            lock (players)
            {
                var playersCopy = players.ToList();
                foreach (var player in playersCopy)
                    player.Player.Play();
            }
        }

        public void Play()
        {
            if (players.Any())
            {
                lock (players)
                {
                    foreach (var player in players)
                    {
                        player.Player.Play();
                    }
                }
                return;
            }

            playing = true;
            PlayNextTrackInNewPlayer(true);
        }

        public void Prepare()
        {
            if (players.Any())
                return;

            PlayNextTrackInNewPlayer(false);
        }

        public void NextTrack()
        {
            if (!playing)
                return;

            lock (players)
            {
                var playersCopy = players.ToList();
                foreach (var player in playersCopy)
                    player.Player.Close();
            }
            PlayNextTrackInNewPlayer(true);
        }

        private void StateChanged(object sender, AudioPlayerApp.AudioPlayer.StateEventArgs e)
        {
            if (e.NewState == AudioPlayerApp.AudioPlayerState.Closed)
            {
                var player = players.FirstOrDefault(x => x.Player == sender);
                if (player != null)
                {
                    lock (players)
                    {
                        players.Remove(player);
                    }

                    player.FileStream.Close();
                }
            }
        }

        public void Stop()
        {
            playing = false;
            lock (players)
            {
                var playersCopy = players.ToList();
                foreach (var player in playersCopy)
                    player.Player.Close();
            }
        }
    }
}
