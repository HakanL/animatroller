//#define VERBOSE_LOGGING
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Framework.MonoExpanderMessages;
using Newtonsoft.Json.Linq;
using NLog;

namespace Animatroller.Framework.Expander
{
    public abstract class MonoExpanderBaseInstance
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();
        private string expanderSharedFiles;
        protected Action<object> sendAction;
        private Dictionary<Type, System.Reflection.MethodInfo> handleMethodCache;
        protected string connectionId;
        protected string name;
        protected string instanceId;
        private Dictionary<string, object> lastState;

        public MonoExpanderBaseInstance()
        {
            this.handleMethodCache = new Dictionary<Type, System.Reflection.MethodInfo>();
            this.lastState = new Dictionary<string, object>();
        }

        internal void Initialize(string expanderSharedFiles, string instanceId, Action<object> sendAction)
        {
            this.expanderSharedFiles = expanderSharedFiles;
            this.instanceId = instanceId;
            this.sendAction = sendAction;
        }

        protected void SendMessage(object message, string stateKey = null)
        {
            this.sendAction?.Invoke(message);

            if (!string.IsNullOrEmpty(stateKey))
                this.lastState[stateKey] = message;
        }

        public void ClientConnected(string connectionId)
        {
            this.connectionId = connectionId;

            log.Info("Client {0} connected to instance {1}", connectionId, this.instanceId);

            // Send all state data
            foreach (var kvp in this.lastState)
                SendMessage(kvp.Value);
        }

        public void HandleMessage(string connectionId, Type messageType, object messageObject)
        {
            this.connectionId = connectionId;

            System.Reflection.MethodInfo methodInfo;
            lock (this)
            {
                if (!this.handleMethodCache.TryGetValue(messageType, out methodInfo))
                {
                    var handleMethods = typeof(MonoExpanderInstance).GetMethods()
                        .Where(x => x.Name == "Handle" && x.GetParameters().Any(p => p.ParameterType == messageType))
                        .ToList();

                    methodInfo = handleMethods.SingleOrDefault();

                    this.handleMethodCache.Add(messageType, methodInfo);
                }
            }

            methodInfo?.Invoke(this, new object[] { messageObject });
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

        public void Handle(Ping message)
        {
#if VERBOSE_LOGGING
            log.Trace($"Response from instance {this.name} at {this.connectionId}");
#endif

        }

        public void Handle(FileRequest message)
        {
            log.Info("Requested download file {1} of type {0}", message.Type, message.FileName);

            if (!string.IsNullOrEmpty(Path.GetDirectoryName(message.FileName)))
                throw new ArgumentException("FileName should be without path");

            string fileTypeFolder = Path.Combine(this.expanderSharedFiles, message.Type.ToString());
            Directory.CreateDirectory(fileTypeFolder);

            string filePath = Path.Combine(fileTypeFolder, message.FileName);

            if (!File.Exists(filePath))
            {
                log.Warn("File {0} of type {1} doesn't exist", message.FileName, message.Type);

                SendMessage(new FileResponse
                {
                    DownloadId = message.DownloadId,
                    Size = 0
                });

                return;
            }

            var fi = new FileInfo(filePath);

            SendMessage(new FileResponse
            {
                DownloadId = message.DownloadId,
                Size = fi.Length,
                SignatureSha1 = CalculateSignatureSha1(filePath)
            });
        }

        public void Handle(FileChunkRequest message)
        {
            string filePath = Path.Combine(this.expanderSharedFiles, message.Type.ToString(), message.FileName);

            long fileSize = new FileInfo(filePath).Length;
            int chunkId = (int)(message.ChunkStart / message.ChunkSize);
            int chunks = (int)(fileSize / message.ChunkSize);
            log.Info("Request for file {0} chunk {1}/{2} for {3:N0} bytes", message.FileName, chunkId, chunks, message.ChunkSize);

            using (var fs = File.OpenRead(filePath))
            {
                fs.Seek(message.ChunkStart, SeekOrigin.Begin);

                int bytesToRead = Math.Min(message.ChunkSize, (int)(fs.Length - message.ChunkStart));
                if (bytesToRead <= 0)
                    return;

                byte[] chunk = new byte[bytesToRead];
                fs.Read(chunk, 0, chunk.Length);

                SendMessage(new FileChunkResponse
                {
                    DownloadId = message.DownloadId,
                    ChunkStart = message.ChunkStart,
                    Chunk = chunk
                });
            }
        }
    }
}
