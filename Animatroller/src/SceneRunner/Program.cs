using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Animatroller.Framework;
using Serilog;

namespace Animatroller.Scenes
{
    public class Program
    {
        private static ILogger log;

        public static void Main(string[] args)
        {
            var logConfig = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Verbose()
                .WriteTo.ColoredConsole()
                .WriteTo.RollingFile("Logs\\SceneRunner.{Date}.log");

            if (!string.IsNullOrEmpty(SceneRunner.Properties.Settings.Default.SeqServerURL))
                logConfig = logConfig.WriteTo.Seq(serverUrl: SceneRunner.Properties.Settings.Default.SeqServerURL, apiKey: SceneRunner.Properties.Settings.Default.SeqApiKey);

            log = Log.Logger = logConfig.CreateLogger();

            Log.Logger.Information("Starting up!");

            Console.SetWindowSize(180, 70);
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

            IScene scene = (IScene)Activator.CreateInstance(sceneType, sceneArgs);


            // Register the scene (so it can be properly stopped)
            Executor.Current.Register(scene);

            // Wire up the instantiated expanders
            if (simForm != null)
            {
                simForm.AutoWireUsingReflection(scene);
            }

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
        }
    }
}
