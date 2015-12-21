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

        protected Logger log = LogManager.GetCurrentClassLogger();
        private Main main;
        private Dictionary<string, DownloadInfo> downloadInfo;
        private const int ChunkSize = 16384;
        private const int BufferedChunks = 5;
        private IHubProxy hub;

        public MonoExpanderClient(Main main, IHubProxy hub)
        {
            this.main = main;
            this.hub = hub;
            this.downloadInfo = new Dictionary<string, DownloadInfo>();
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
            this.hub.Invoke("HandleMessage", message.GetType(), message);
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
            SendMessage(new FileRequest
            {
                DownloadId = downloadInfo.Id,
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
                        this.downloadInfo.Remove(downloadInfo.Id);

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
