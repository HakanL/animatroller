using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Animatroller.Framework.MonoExpanderMessages;
using System.IO;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json.Linq;

namespace Animatroller.MonoExpander
{
    public class MonoExpanderClient
    {
        protected class DownloadInfo
        {
            public DateTime Start { get; private set; }

            public DateTime? LastReceive { get; set; }

            public string TempFolder { get; private set; }

            public string Id { get; private set; }

            public FileTypes FileType { get; private set; }

            public string FileName { get; set; }

            public string FinalFilePath { get; set; }

            public byte[] SignatureSha1 { get; set; }

            public long FileSize { get; set; }

            public int Chunks { get; set; }

            public int RequestedChunks { get; set; }

            public int ReceivedChunks { get; set; }

            public object TriggerMessage { get; set; }

            public DownloadInfo(string fileStoragePath, FileTypes fileType)
            {
                Id = Guid.NewGuid().ToString("n");
                Start = DateTime.Now;
                FileType = fileType;
                TempFolder = Path.Combine(fileStoragePath, "tmp", fileType.ToString(), Id, "_chunks");
            }

            public bool IsZombie
            {
                get
                {
                    if (LastReceive.HasValue)
                    {
                        return (DateTime.Now - LastReceive.Value).TotalSeconds > 30;
                    }
                    else
                        return (DateTime.Now - Start).TotalSeconds > 30;
                }
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

        protected Logger log = LogManager.GetCurrentClassLogger();
        private Main main;
        private Dictionary<string, DownloadInfo> downloadInfos;
        private const int ChunkSize = 16384;
        private const int BufferedChunks = 5;
        private IHubProxy hub;

        public MonoExpanderClient(Main main, IHubProxy hub)
        {
            this.main = main;
            this.hub = hub;
            this.downloadInfos = new Dictionary<string, DownloadInfo>();
        }

        public void HandleMessage(object message)
        {

        }

        public void HandleMessage(Type messageType, object message)
        {
            var jobject = message as JObject;
            if (jobject != null)
            {
                var messageObject = jobject.ToObject(messageType);

                var method = typeof(MonoExpanderClient).GetMethods()
                    .Where(x => x.Name == "Handle" && x.GetParameters().Any(p => p.ParameterType == messageType))
                    .ToList();

                method.SingleOrDefault()?.Invoke(this, new object[] { messageObject });
            }
        }

        private void SendMessage(object message)
        {
            try
            {
                this.hub.Invoke("HandleMessage", message.GetType(), message);
            }
            catch (InvalidOperationException ex)
            {
                this.log.Error(ex, "Unable to send in SendMessage");
            }
        }

        public void Handle(Ping message)
        {
            log.Debug("Ping from server");
        }

        public void Handle(SetOutputRequest message)
        {
            this.main.Handle(message);
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

        private void CleanUpDownloadInfo()
        {
            lock (this.downloadInfos)
            {
                foreach (var kvp in this.downloadInfos.ToList())
                {
                    var downloadInfo = kvp.Value;

                    if (downloadInfo.IsZombie)
                    {
                        downloadInfo.Cleanup();
                        this.downloadInfos.Remove(kvp.Key);
                    }
                }
            }
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

            CleanUpDownloadInfo();

            this.log.Info("Missing file {0} of type {1}", fileName, fileType);

            string downloadId;

            lock (this.downloadInfos)
            {
                if (this.downloadInfos.Any(x => x.Value.FileType == fileType && x.Value.FileName == fileName))
                {
                    // Already in the process to be downloaded
                    this.log.Info("Already in the process of being downloaded");

                    return false;
                }

                var downloadInfo = new DownloadInfo(this.main.FileStoragePath, fileType)
                {
                    TriggerMessage = triggerMessage,
                    FileName = fileName,
                    FinalFilePath = filePath
                };

                this.downloadInfos.Add(downloadInfo.Id, downloadInfo);

                if (Directory.Exists(downloadInfo.TempFolder))
                    // Empty it
                    Directory.Delete(downloadInfo.TempFolder, true);

                downloadId = downloadInfo.Id;
            }

            // Request the file
            SendMessage(new FileRequest
            {
                DownloadId = downloadId,
                Type = fileType,
                FileName = fileName
            });

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

            lock (this.downloadInfos)
            {
                if (!this.downloadInfos.TryGetValue(message.DownloadId, out downloadInfo))
                    // Unknown, ignore
                    return;

                if (message.Size == 0)
                {
                    // Doesn't exist
                    this.downloadInfos.Remove(message.DownloadId);

                    downloadInfo.Cleanup();

                    return;
                }

                downloadInfo.FileSize = message.Size;
                downloadInfo.SignatureSha1 = message.SignatureSha1;

                // Prep temp folder
                Directory.CreateDirectory(downloadInfo.TempFolder);

                // Request chunks
                downloadInfo.Chunks = (int)(message.Size / ChunkSize) + 1;

                for (int chunkId = 0; chunkId < downloadInfo.Chunks && chunkId < BufferedChunks; chunkId++)
                {
                    this.log.Trace("Requesting chunk {0} of file {1}", chunkId, downloadInfo.FileName);

                    SendMessage(new FileChunkRequest
                    {
                        DownloadId = message.DownloadId,
                        FileName = downloadInfo.FileName,
                        Type = downloadInfo.FileType,
                        ChunkSize = ChunkSize,
                        ChunkStart = downloadInfo.RequestedChunks * ChunkSize
                    });

                    downloadInfo.RequestedChunks++;
                }
            }
        }

        public void Handle(FileChunkResponse message)
        {
            DownloadInfo downloadInfo;

            lock (this.downloadInfos)
            {
                if (!this.downloadInfos.TryGetValue(message.DownloadId, out downloadInfo))
                    // Unknown, ignore
                    return;

                string chunkFile = Path.Combine(downloadInfo.TempFolder, string.Format("chunk_{0}.bin", message.ChunkStart));
                Directory.CreateDirectory(Path.GetDirectoryName(chunkFile));

                File.WriteAllBytes(chunkFile, message.Chunk);

                downloadInfo.ReceivedChunks++;
                downloadInfo.LastReceive = DateTime.Now;

                // Request next chunk
                if (downloadInfo.RequestedChunks < downloadInfo.Chunks)
                {
                    this.log.Trace("Requesting chunk {0} of file {1}", downloadInfo.RequestedChunks, downloadInfo.FileName);

                    SendMessage(new FileChunkRequest
                    {
                        DownloadId = message.DownloadId,
                        FileName = downloadInfo.FileName,
                        Type = downloadInfo.FileType,
                        ChunkSize = ChunkSize,
                        ChunkStart = downloadInfo.RequestedChunks * ChunkSize
                    });

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
                                        this.log.Trace("Re-requesting chunk {0} of file {1}", chunkStart / ChunkSize, downloadInfo.FileName);

                                        // Re-request
                                        SendMessage(new FileChunkRequest
                                        {
                                            DownloadId = message.DownloadId,
                                            FileName = downloadInfo.FileName,
                                            Type = downloadInfo.FileType,
                                            ChunkSize = ChunkSize,
                                            ChunkStart = chunkStart
                                        });

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

                                SendMessage(new FileRequest
                                {
                                    DownloadId = downloadInfo.Id,
                                    Type = downloadInfo.FileType,
                                    FileName = downloadInfo.FileName
                                });

                                return;
                            }

                            // Match, move to final directory
                            File.Move(assembleFile, downloadInfo.FinalFilePath);
                            assembleFile = null;
                            this.downloadInfos.Remove(downloadInfo.Id);

                            if (downloadInfo.TriggerMessage != null)
                            {
                                // Invoke
                                var method = typeof(MonoExpanderClient).GetMethods()
                                    .Where(x => x.Name == "Handle" && x.GetParameters().Any(p => p.ParameterType == downloadInfo.TriggerMessage.GetType()))
                                    .ToList();

                                method.SingleOrDefault()?.Invoke(this, new object[] { downloadInfo.TriggerMessage });
                            }
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
}
