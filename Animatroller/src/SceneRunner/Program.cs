using System;
using System.Collections.Generic;
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
            Animatroller.Framework.Expander.IOExpander ioExpander = null;
            Animatroller.Framework.Expander.AcnStream acnOutput = null;
            Animatroller.Framework.Expander.Raspberry oscServer = null;

            // Figure out which IO expanders to use, taken from command line (space-separated)
            var sceneArgs = new List<string>();
            foreach (var arg in args)
            {
                switch (arg)
                {
                    case "SIM":
                        // WinForms simulator
                        simForm = new Animatroller.Simulator.SimulatorForm();
                        break;

                    case "DMXPRO":
                        // Enttec DMX USB Pro or DMXLink (specify virtual COM port in config file)
                        dmxPro = new Animatroller.Framework.Expander.DMXPro(Properties.Settings.Default.DMXProPort);
                        break;

                    case "IOEXP":
                        // Propeller-based expansion board for input/output/dmx/GECE-pixels/motor, etc
                        // Specify virtual COM port in config file
                        ioExpander = new Animatroller.Framework.Expander.IOExpander(Properties.Settings.Default.IOExpanderPort);
                        break;

                    case "ACN":
                        // ACN E1.31 streaming output. Will pick first non-loopback network card to bind to
                        acnOutput = new Framework.Expander.AcnStream();
                        break;

                    case "RASP":
                        oscServer = new Framework.Expander.Raspberry(Properties.Settings.Default.RaspberryHost);
                        break;

                    default:
                        // Pass other parameters to the scene. Can be used to load test data to operating hours, etc
                        sceneArgs.Add(arg);
                        break;
                }
            }




            // Uncomment which scene you want to execute. Can be improved later, but currently I
            // use Visual Studio on my scene-running PC to improve things on the fly

            //var scene = new TestScene();
            var scene = new TestScene3();
            //var scene = new LORScene();
//            var scene = new PixelScene1(sceneArgs);
            //var scene = new Nutcracker1Scene();
            //var scene = new Nutcracker2Scene();
            //var scene = new Nutcracker3Scene();
            //var scene = new HalloweenScene();
            //var scene = new XmasScene();
            //var scene = new XmasScene2(sceneArgs);




            // Register the scene (so it can be properly stopped)
            Executor.Current.Register(scene);

            // Wire up the instantiated expanders
            if (simForm != null)
                scene.WireUp(simForm);
            if (dmxPro != null)
                scene.WireUp(dmxPro);
            if (ioExpander != null)
                scene.WireUp(ioExpander);
            if (acnOutput != null)
                scene.WireUp(acnOutput);
            if (oscServer != null)
                scene.WireUp(oscServer);

            // Initialize
            Executor.Current.Start();
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
