using Animatroller.Framework;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Animatroller.SceneRunner
{
    public class Program
    {
        private const string FileTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Logger} [{Level}] {Message}{NewLine}{Exception}";
        private const string ConsoleTemplate = "{Timestamp:HH:mm:ss.fff} {Logger} [{Level}] {Message}{NewLine}{Exception}";
        //private const string DebugTemplate = "{Timestamp:HH:mm:ss.fff} {Logger} [{Level}] {Message}{NewLine}{Exception}";
        private const string DebugTemplate = "{Timestamp:HH:mm:ss.fff} {Logger} [{Level}] {Message}{Exception}\r\n";
        private const int RemoteUpdateThrottleMilliseconds = 100;

        private static ILogger log;
        private static readonly Dictionary<string, Type> typeCache = new Dictionary<string, Type>();
        private static System.Threading.Timer remoteUpdateTimer;
        private static AdminMessage.SceneDefinition sceneDefinition;
        private static List<SendObject> sendControls;
        private static ExpanderCommunication.IServerCommunication adminServer;
        private static readonly Dictionary<string, DateTime> clients = new Dictionary<string, DateTime>();
        private static readonly Stopwatch lastSentUpdate = Stopwatch.StartNew();
        private static bool updatesAvailable;

        public static async Task Main(string[] args)
        {
            var logConfig = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Verbose()
                .WriteTo.Console(outputTemplate: ConsoleTemplate)
                .WriteTo.Debug(outputTemplate: DebugTemplate)
                .WriteTo.File(
                    path: Path.Combine(AppContext.BaseDirectory, "Logs", "log-{Date}.txt"),
                    outputTemplate: FileTemplate);

            if (!string.IsNullOrEmpty(SceneRunner.Properties.Settings.Default.SeqServerURL))
                logConfig = logConfig.WriteTo.Seq(
                    serverUrl: SceneRunner.Properties.Settings.Default.SeqServerURL,
                    apiKey: SceneRunner.Properties.Settings.Default.SeqApiKey,
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug);

            log = Log.Logger = logConfig.CreateLogger();

            Log.Logger.Information("Starting up!");

            Executor.Current.SetLogger(log);

            Console.SetWindowSize(Math.Min(Console.LargestWindowWidth, 180), Math.Min(Console.LargestWindowHeight, 70));
            // Why doesn't this work?
            //Console.SetWindowPosition(0, 0);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Variables for al types of IO expanders, etc
            Animatroller.Simulator.SimulatorForm simForm = null;

            // Figure out which IO expanders to use, taken from command line (space-separated)
            var sceneArgs = new List<string>();
            string sceneName = string.Empty;
            foreach (var arg in args)
            {
                var parts = arg.Split('=');
                parts[0] = parts[0].ToUpper();

                switch (parts[0])
                {
                    case "SCENE":
                        sceneName = parts[1].ToUpper();
                        break;

                    case "SIM":
                        // WinForms simulator
                        simForm = new Animatroller.Simulator.SimulatorForm();
                        break;

                    case "OFFLINE":
                        Executor.Current.IsOffline = true;
                        break;

                    default:
                        // Pass other parameters to the scene. Can be used to load test data to operating hours, etc
                        sceneArgs.Add(arg);
                        break;
                }
            }


            // Load scene
            var sceneInterfaceType = typeof(IScene);

            var scenesAssembly = System.Reflection.Assembly.LoadFile(Path.Combine(Path.GetDirectoryName(sceneInterfaceType.Assembly.Location), "Scenes.dll"));

            var sceneTypes = scenesAssembly.GetTypes()
                .Where(p => sceneInterfaceType.IsAssignableFrom(p) &&
                    !p.IsInterface &&
                    !p.IsAbstract);

            var sceneType = sceneTypes.SingleOrDefault(x => x.Name.Equals(sceneName, StringComparison.OrdinalIgnoreCase));
            if (sceneType == null)
                throw new ArgumentException("Missing start scene");

            Executor.Current.KeyStoragePrefix = sceneType.Name;

            var scene = (IScene)Activator.CreateInstance(sceneType, sceneArgs);


            // Register the scene (so it can be properly stopped)
            Executor.Current.Register(scene);

            // Wire up the instantiated expanders
            if (simForm != null)
            {
                simForm.AutoWireUsingReflection(scene);
            }

            remoteUpdateTimer = new System.Threading.Timer(RemoteUpdateTimerCallback, null, RemoteUpdateThrottleMilliseconds, RemoteUpdateThrottleMilliseconds);

            var sceneBuilder = new SceneDefinitionBuilder();
            var updateAvailable = new Action(() =>
            {
                updatesAvailable = true;

                if (lastSentUpdate.ElapsedMilliseconds >= RemoteUpdateThrottleMilliseconds)
                    // Send within 10 ms, otherwise wait for next timer callback
                    remoteUpdateTimer.Change(10, RemoteUpdateThrottleMilliseconds);
            });
            var sceneData = sceneBuilder.AutoWireUsingReflection(scene, updateAvailable);
            sceneDefinition = sceneData.SceneDefinition;
            sendControls = sceneData.SendControls.Select(x => new SendObject
            {
                ComponentId = x.ComponentId,
                SendControl = x
            }).ToList();

            adminServer = new ExpanderCommunication.NettyServer(
                        logger: Log.Logger,
                        listenPort: 54345,
                        dataReceivedAction: DataReceived,
                        clientConnectedAction: ClientConnected);

            Task.Run(async () => await adminServer.StartAsync()).Wait();

            // Run
            Executor.Current.Run();

            Executor.Current.SetScenePersistance(scene);

            if (simForm != null)
            {
                simForm.Show();
                simForm.FormClosing += (sender, e) =>
                    {
                        simForm.PendingClose = true;

                        // Do this on a separate thread so it won't block the Main UI thread
                        var stopTask = new Task(() => Executor.Current.Stop());
                        stopTask.Start();

                        while (!Executor.Current.EverythingStopped())
                        {
                            System.Windows.Forms.Application.DoEvents();
                            System.Threading.Thread.Sleep(50);
                        }
                        stopTask.Wait();
                    };
                // If using the simulator then run it until the form is closed. Otherwise run until NewLine
                // in command prompt
                Application.Run(simForm);

                Executor.Current.WaitToStop(5000);
            }
            else
            {
                Console.ReadLine();
                Executor.Current.Stop();
                Executor.Current.WaitToStop(5000);
            }

            remoteUpdateTimer.Dispose();
            remoteUpdateTimer = null;
            await adminServer.StopAsync();
        }

        private static IList<AdminMessage.ComponentUpdate> GetComponentUpdates(bool force)
        {
            var list = new List<AdminMessage.ComponentUpdate>();

            var hash = System.Security.Cryptography.MD5.Create();

            foreach (var kvp in sendControls)
            {
                var msg = kvp.SendControl.GetMessageToSend();

                if (msg != null)
                {
                    using (var ms = new MemoryStream())
                    {
                        AdminMessage.Serializer.Serialize(msg, ms);

                        ms.Position = 0;

                        byte[] currentHash = hash.ComputeHash(ms);

                        if (force || (kvp.LastHash == null ||
                            !kvp.LastHash.SequenceEqual(currentHash) ||
                            (DateTime.UtcNow - kvp.LastSend).TotalMinutes > 10))
                        {
                            // Different or due for a refresh
                            kvp.LastHash = currentHash;
                            kvp.LastSend = DateTime.UtcNow;

                            list.Add(new AdminMessage.ComponentUpdate
                            {
                                ComponentId = kvp.ComponentId,
                                MessageType = msg.GetType().FullName,
                                Object = ms.ToArray()
                            });
                        }
                    }
                }
            }

            return list;
        }

        private static void RemoteUpdateTimerCallback(object state)
        {
            if (!updatesAvailable)
                // No updates available, don't do anything, wait for next timer callback
                return;

            // Stop timer
            remoteUpdateTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

            try
            {
                if (!clients.Any())
                    return;

                // Reset
                updatesAvailable = false;
                lastSentUpdate.Restart();

                var list = GetComponentUpdates(false);

                if (list.Any())
                {
                    var controlUpdateMessage = new AdminMessage.ControlUpdate
                    {
                        Updates = list.ToArray()
                    };
                    string messageType = controlUpdateMessage.GetType().FullName;

                    using (var ms = new MemoryStream())
                    {
                        AdminMessage.Serializer.Serialize(controlUpdateMessage, ms);

                        var data = ms.ToArray();

                        // Broadcast
                        foreach (var client in clients.ToList())
                        {
                            if ((DateTime.UtcNow - client.Value).TotalHours > 4)
                            {
                                clients.Remove(client.Key);
                                continue;
                            }

                            adminServer.SendToClientAsync(client.Key, messageType, data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, "Error in " + nameof(RemoteUpdateTimerCallback));
            }
            finally
            {
                remoteUpdateTimer?.Change(RemoteUpdateThrottleMilliseconds, RemoteUpdateThrottleMilliseconds);
            }
        }

        private static void DataReceived(string instanceId, string connectionId, string messageType, byte[] data)
        {
            clients[instanceId] = DateTime.UtcNow;

            object messageObject;
            Type type;

            using (var ms = new MemoryStream(data))
            {
                lock (typeCache)
                {
                    if (!typeCache.TryGetValue(messageType, out type))
                    {
                        type = typeof(AdminMessage.Ping).Assembly.GetType(messageType, true);
                        typeCache.Add(messageType, type);
                    }
                }

                messageObject = AdminMessage.Serializer.DeserializeFromStream(ms, type);
            }

            if (messageObject != null)
            {

            }
        }

        private static void ClientConnected(string instanceId, string connectionId, System.Net.EndPoint endPoint)
        {
            clients[instanceId] = DateTime.UtcNow;

            var componentStatus = GetComponentUpdates(true);

            var newDef = new AdminMessage.NewSceneDefinition
            {
                Definition = sceneDefinition,
                InitialStatus = componentStatus.ToArray()
            };

            using (var ms = new MemoryStream())
            {
                AdminMessage.Serializer.Serialize(newDef, ms);

                adminServer.SendToClientAsync(instanceId, newDef.GetType().FullName, ms.ToArray());
            }
        }
    }
}
