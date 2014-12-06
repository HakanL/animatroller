using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Framework;
using NLog;

namespace Animatroller.SceneRunner
{
    public class Program
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public static void Main(string[] args)
        {
            // Variables for al types of IO expanders, etc
            Animatroller.Simulator.SimulatorForm simForm = null;
            Animatroller.Framework.Expander.DMXPro dmxPro = null;
            Animatroller.Framework.Expander.Renard renard = null;
            Animatroller.Framework.Expander.IOExpander ioExpander = null;
            Animatroller.Framework.Expander.AcnStream acnOutput = null;
            Animatroller.Framework.Expander.MidiInput midiInput = null;

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

                    case "DMXPRO":
                        // Enttec DMX USB Pro or DMXLink (specify virtual COM port in config file)
                        dmxPro = new Animatroller.Framework.Expander.DMXPro(parts[1]);
                        break;

                    case "RENARD":
                        // Renard (specify virtual COM port in config file)
                        renard = new Animatroller.Framework.Expander.Renard(parts[1]);
                        break;

                    case "IOEXP":
                        // Propeller-based expansion board for input/output/dmx/GECE-pixels/motor, etc
                        ioExpander = new Animatroller.Framework.Expander.IOExpander(parts[1]);
                        break;

                    case "MIDI":
                        // Midi input (like Akai LPD-8)
                        midiInput = new Animatroller.Framework.Expander.MidiInput();
                        break;

                    case "ACN":
                        // ACN E1.31 streaming output. Will pick first non-loopback network card to bind to
                        acnOutput = new Framework.Expander.AcnStream();
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
            if(sceneType == null)
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

            if (scene is ISceneRequiresDMXPro)
            {
                if (dmxPro == null)
                    throw new ArgumentNullException("DMXpro not configured");
                ((ISceneRequiresDMXPro)scene).WireUp(dmxPro);
            }

            if (scene is ISceneRequiresRenard)
            {
                if (renard == null)
                    throw new ArgumentNullException("Renard not configured");
                ((ISceneRequiresRenard)scene).WireUp(renard);
            }

            if (scene is ISceneRequiresIOExpander)
            {
                if (ioExpander == null)
                    throw new ArgumentNullException("IOExpander not configured");
                ((ISceneRequiresIOExpander)scene).WireUp(ioExpander);
            }

            if (scene is ISceneRequiresMidiInput)
            {
                if (midiInput == null)
                    throw new ArgumentNullException("MidiInput not configured");
                ((ISceneRequiresMidiInput)scene).WireUp(midiInput);
            }

            if (scene is ISceneRequiresAcnStream)
            {
                if (acnOutput == null)
                    throw new ArgumentNullException("AcnOutput not configured");
                ((ISceneRequiresAcnStream)scene).WireUp(acnOutput);
            }

            // Run
            Executor.Current.Run();

            if (simForm != null)
            {
                simForm.Show();
                simForm.FormClosing += (sender, e) =>
                    {
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
                System.Windows.Forms.Application.Run(simForm);

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
