using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Akka.Actor;
using Animatroller.Framework.MonoExpanderMessages;
using Akka.Cluster;
using System.IO;

namespace Animatroller.MonoExpander
{
    public interface IMonoExpanderClientActor :
        IHandle<SetOutputRequest>,
        IHandle<AudioEffectCue>,
        IHandle<AudioEffectPlay>,
        IHandle<AudioEffectPause>,
        IHandle<AudioEffectResume>,
        IHandle<AudioEffectSetVolume>,
        IHandle<AudioBackgroundSetVolume>,
        IHandle<AudioBackgroundResume>,
        IHandle<AudioBackgroundPause>,
        IHandle<AudioBackgroundNext>,
        IHandle<AudioTrackPlay>,
        IHandle<AudioTrackCue>,
        IHandle<AudioTrackResume>,
        IHandle<AudioTrackPause>,
        IHandle<VideoPlay>
    {
    }

    public class DownloadInfo
    {
        public DateTime Start { get; private set; }

        public string TempFolder { get; set; }

        public string Id { get; private set; }

        public FileTypes FileType { get; set; }

        public string FileName { get; set; }

        public string FinalFilePath { get; set; }

        public byte[] SignatureSha1 { get; set; }

        public long FileSize { get; set; }

        public int Chunks { get; set; }

        public int RequestedChunks { get; set; }

        public int ReceivedChunks { get; set; }

        public object TriggerMessage { get; set; }

        public DownloadInfo()
        {
            Id = Guid.NewGuid().ToString("n");
            Start = DateTime.Now;
        }

        internal void Cleanup()
        {
            try
            {
                Directory.Delete(TempFolder, true);
            }
            catch
            {
            }
        }

        internal void Restart()
        {
            Start = DateTime.Now;
            FileSize = 0;
            ReceivedChunks = 0;
            RequestedChunks = 0;
            SignatureSha1 = null;
        }
    }

    public class MonoExpanderClientActor : TypedActor,
        ILogReceive,
        IHandle<ClusterEvent.IMemberEvent>,
        IHandle<WhoAreYouRequest>,
        IHandle<ActorIdentity>,
        IHandle<FileResponse>,
        IHandle<FileChunkResponse>,
        IMonoExpanderClientActor
    {
        protected Logger log = LogManager.GetCurrentClassLogger();
        private Main main;
        private Dictionary<string, DownloadInfo> downloadInfo;
        private const int ChunkSize = 16384;
        private const int BufferedChunks = 5;

        public MonoExpanderClientActor(Main main)
        {
            this.main = main;
            this.downloadInfo = new Dictionary<string, DownloadInfo>();

            // Subscribe to cluster events
            Cluster.Get(Context.System).Subscribe(Self, new[] {
                typeof(ClusterEvent.IMemberEvent) });
        }

        public void Handle(SetOutputRequest message)
        {
            this.main.Handle(message);
        }

        private void CheckMembers()
        {
            var currentUpMembers = Cluster.Get(Context.System).ReadView.Members
                .Where(x => x.Status == MemberStatus.Up && x.HasRole("animatroller"));

            foreach (var member in currentUpMembers)
            {
                // Found a master, ping it
                var sel = GetServerActorSelection(member.Address);
                sel.Tell(new Identify("animatroller"), Self);
            }
        }

        public void Handle(ClusterEvent.IMemberEvent message)
        {
            CheckMembers();

            if (message.Member.Status != MemberStatus.Up || !message.Member.Roles.Contains("animatroller"))
            {
                // Not valid
                this.main.RemoveServer(message.Member.Address);
            }
        }

        private ActorSelection GetServerActorSelection(Address address)
        {
            return Context.ActorSelection(string.Format("{0}/user/ExpanderServer", address));
        }

        public void Handle(WhoAreYouRequest message)
        {
            Sender.Tell(new WhoAreYouResponse
            {
                InstanceId = this.main.InstanceId
            }, Self);
        }

        public void Handle(ActorIdentity message)
        {
            if (message.MessageId is string && (string)message.MessageId == "animatroller")
            {
                // We expected this
                this.main.AddServer(Sender.Path.Address, Sender);
            }
        }

        public void Handle(AudioEffectCue message)
        {
            if (CheckFile(message, FileTypes.AudioEffect, message.FileName))
                this.main.Handle(message);
        }

        public void Handle(AudioEffectPlay message)
        {
            if (CheckFile(message, FileTypes.AudioEffect, message.FileName))
                this.main.Handle(message);
        }

        public void Handle(AudioEffectPause message)
        {
            this.main.Handle(message);
        }

        public void Handle(AudioEffectResume message)
        {
            this.main.Handle(message);
        }

        public void Handle(AudioEffectSetVolume message)
        {
            this.main.Handle(message);
        }

        public void Handle(AudioBackgroundSetVolume message)
        {
            this.main.Handle(message);
        }

        public void Handle(AudioBackgroundResume message)
        {
            this.main.Handle(message);
        }

        public void Handle(AudioBackgroundPause message)
        {
            this.main.Handle(message);
        }

        public void Handle(AudioBackgroundNext message)
        {
            this.main.Handle(message);
        }

        public void Handle(AudioTrackPlay message)
        {
            if (CheckFile(message, FileTypes.AudioTrack, message.FileName))
                this.main.Handle(message);
        }

        public void Handle(AudioTrackCue message)
        {
            if (CheckFile(message, FileTypes.AudioTrack, message.FileName))
                this.main.Handle(message);
        }

        public void Handle(AudioTrackResume message)
        {
            this.main.Handle(message);
        }

        public void Handle(AudioTrackPause message)
        {
            this.main.Handle(message);
        }

        public void Handle(VideoPlay message)
        {
            if (CheckFile(message, FileTypes.Video, message.FileName))
                this.main.Handle(message);
        }

        private bool CheckFile(object triggerMessage, FileTypes fileType, string fileName)
        {
            if (!string.IsNullOrEmpty(Path.GetDirectoryName(fileName)))
                throw new ArgumentException("FileName should be without path");

            string fileTypeFolder = Path.Combine(this.main.FileStoragePath, fileType.ToString());
            Directory.CreateDirectory(fileTypeFolder);

            string filePath = Path.Combine(fileTypeFolder, fileName);
            if (File.Exists(filePath))
                return true;

            this.log.Info("Missing file {0} of type {1}", fileName, fileType);

            if (this.downloadInfo.Any(x => x.Value.FileType == fileType && x.Value.FileName == fileName))
                // Already in the process to be downloaded
                return false;

            var downloadInfo = new DownloadInfo
            {
                TriggerMessage = triggerMessage,
                TempFolder = Path.Combine(this.main.FileStoragePath, "tmp", fileType.ToString(), "_chunks"),
                FileType = fileType,
                FileName = fileName,
                FinalFilePath = filePath
            };

            this.downloadInfo.Add(downloadInfo.Id, downloadInfo);

            if (Directory.Exists(downloadInfo.TempFolder))
                // Empty it
                Directory.Delete(downloadInfo.TempFolder, true);

            // Request the file
            Sender.Tell(new FileRequest
            {
                DownloadId = downloadInfo.Id,
                Type = fileType,
                FileName = fileName
            }, Self);

            return false;
        }

        private byte[] CalculateSignatureSha1(string fileName)
        {
            using (var fs = new FileStream(fileName, FileMode.Open))
            using (var bs = new BufferedStream(fs))
            {
                using (var sha1 = new System.Security.Cryptography.SHA1Managed())
                {
                    return sha1.ComputeHash(bs);
                }
            }
        }

        public void Handle(FileResponse message)
        {
            DownloadInfo downloadInfo;

            if (!this.downloadInfo.TryGetValue(message.DownloadId, out downloadInfo))
                // Unknown, ignore
                return;

            if (message.Size == 0)
            {
                // Doesn't exist
                this.downloadInfo.Remove(message.DownloadId);

                downloadInfo.Cleanup();

                return;
            }

            downloadInfo.FileSize = message.Size;
            downloadInfo.SignatureSha1 = message.SignatureSha1;

            // Prep temp folder
            Directory.CreateDirectory(downloadInfo.TempFolder);

            // Request chunks
            downloadInfo.Chunks = (int)(message.Size / ChunkSize) + 1;

            for (int chunkId = 0; chunkId <= downloadInfo.Chunks && chunkId < BufferedChunks; chunkId++)
            {
                Sender.Tell(new FileChunkRequest
                {
                    DownloadId = message.DownloadId,
                    FileName = downloadInfo.FileName,
                    Type = downloadInfo.FileType,
                    ChunkSize = ChunkSize,
                    ChunkStart = downloadInfo.RequestedChunks * ChunkSize
                }, Self);

                downloadInfo.RequestedChunks++;
            }
        }

        public void Handle(FileChunkResponse message)
        {
            DownloadInfo downloadInfo;

            if (!this.downloadInfo.TryGetValue(message.DownloadId, out downloadInfo))
                // Unknown, ignore
                return;

            string chunkFile = Path.Combine(downloadInfo.TempFolder, string.Format("chunk_{0}.bin", message.ChunkStart));

            File.WriteAllBytes(chunkFile, message.Chunk);

            downloadInfo.ReceivedChunks++;

            // Request next chunk
            if (downloadInfo.RequestedChunks < downloadInfo.Chunks)
            {
                Sender.Tell(new FileChunkRequest
                {
                    DownloadId = message.DownloadId,
                    FileName = downloadInfo.FileName,
                    Type = downloadInfo.FileType,
                    ChunkSize = ChunkSize,
                    ChunkStart = downloadInfo.RequestedChunks * ChunkSize
                }, Self);

                downloadInfo.RequestedChunks++;
            }
            else
            {
                if (downloadInfo.ReceivedChunks >= downloadInfo.Chunks)
                {
                    // Check if we have everything

                    long chunkStart = 0;
                    long fileSize = 0;
                    string assembleFile = Path.Combine(Path.GetDirectoryName(downloadInfo.TempFolder), downloadInfo.FileName);

                    try
                    {
                        using (var fsOutput = File.Create(assembleFile))
                        {
                            while (fileSize != downloadInfo.FileSize)
                            {
                                chunkFile = Path.Combine(downloadInfo.TempFolder, string.Format("chunk_{0}.bin", chunkStart));

                                long expectedChunkSize = Math.Min(downloadInfo.FileSize - fileSize, ChunkSize);

                                if (!File.Exists(chunkFile) || new FileInfo(chunkFile).Length != expectedChunkSize)
                                {
                                    // Re-request
                                    Sender.Tell(new FileChunkRequest
                                    {
                                        DownloadId = message.DownloadId,
                                        FileName = downloadInfo.FileName,
                                        Type = downloadInfo.FileType,
                                        ChunkSize = ChunkSize,
                                        ChunkStart = chunkStart
                                    }, Self);

                                    // Not done yet
                                    return;
                                }

                                var fi = new FileInfo(chunkFile);
                                chunkStart += fi.Length;
                                fileSize += fi.Length;

                                using (var fsInput = File.OpenRead(chunkFile))
                                    fsInput.CopyTo(fsOutput);
                            }

                            fsOutput.Flush();
                        }

                        // Delete all chunks
                        downloadInfo.Cleanup();

                        // Check signature
                        byte[] sign = CalculateSignatureSha1(assembleFile);
                        if (!sign.SequenceEqual(downloadInfo.SignatureSha1))
                        {
                            // Invalid signature, request the file again
                            downloadInfo.Restart();

                            Sender.Tell(new FileRequest
                            {
                                DownloadId = downloadInfo.Id,
                                Type = downloadInfo.FileType,
                                FileName = downloadInfo.FileName
                            }, Self);

                            return;
                        }

                        // Match, move to final directory
                        File.Move(assembleFile, downloadInfo.FinalFilePath);
                        assembleFile = null;
                        this.downloadInfo.Remove(downloadInfo.Id);

                        if (downloadInfo.TriggerMessage != null)
                            Self.Tell(downloadInfo.TriggerMessage);
                    }
                    finally
                    {
                        // Delete the temporary assemble file
                        if (!string.IsNullOrEmpty(assembleFile))
                            File.Delete(assembleFile);
                    }
                }
            }
        }
    }
}
