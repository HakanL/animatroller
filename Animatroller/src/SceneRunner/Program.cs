﻿using System;
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
            Animatroller.Framework.Expander.Raspberry raspberry1 = null;
            Animatroller.Framework.Expander.Raspberry raspberry2 = null;
            Animatroller.Framework.Expander.Raspberry raspberry3 = null;
            Animatroller.Framework.Expander.Raspberry raspberry4 = null;

            // Figure out which IO expanders to use, taken from command line (space-separated)
            var sceneArgs = new List<string>();
            string sceneName = string.Empty;
            foreach (var arg in args)
            {
                string[] parts2;

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

                    case "IOEXP":
                        // Propeller-based expansion board for input/output/dmx/GECE-pixels/motor, etc
                        ioExpander = new Animatroller.Framework.Expander.IOExpander(parts[1]);
                        break;

                    case "ACN":
                        // ACN E1.31 streaming output. Will pick first non-loopback network card to bind to
                        acnOutput = new Framework.Expander.AcnStream();
                        break;

                    case "RASP":
                    case "RASP1":
                        // Example: RASP=192.168.240.123:5005,3333 to listen on 3333
                        parts2 = parts[1].Split(',');
                        raspberry1 = new Framework.Expander.Raspberry(parts2[0], int.Parse(parts2[1]));
                        break;

                    case "RASP2":
                        // Example: RASP=192.168.240.123:5005,3333 to listen on 3333
                        parts2 = parts[1].Split(',');
                        raspberry2 = new Framework.Expander.Raspberry(parts2[0], int.Parse(parts2[1]));
                        break;

                    case "RASP3":
                        // Example: RASP=192.168.240.123:5005,3333 to listen on 3333
                        parts2 = parts[1].Split(',');
                        raspberry3 = new Framework.Expander.Raspberry(parts2[0], int.Parse(parts2[1]));
                        break;

                    case "RASP4":
                        // Example: RASP=192.168.240.123:5005,3333 to listen on 3333
                        parts2 = parts[1].Split(',');
                        raspberry4 = new Framework.Expander.Raspberry(parts2[0], int.Parse(parts2[1]));
                        break;

                    default:
                        // Pass other parameters to the scene. Can be used to load test data to operating hours, etc
                        sceneArgs.Add(arg);
                        break;
                }
            }




            // Uncomment which scene you want to execute. Can be improved later, but currently I
            // use Visual Studio on my scene-running PC to improve things on the fly

            BaseScene scene = null;
            switch (sceneName)
            {
                case "TEST1":
                    scene = new TestScene2(sceneArgs);
                    break;

                case "TEST2":
                    scene = new TestScene3(sceneArgs);
                    break;

                case "TEST3":
                    scene = new TestScene3(sceneArgs);
                    break;

                case "ITALIAN1":
                    scene = new ItalianScene1(sceneArgs);
                    break;

                case "LOR":
                    scene = new LORScene(sceneArgs);
                    break;

                case "PIXEL1":
                    scene = new PixelScene1(sceneArgs);
                    break;

                case "NUTCRACKER1":
                    scene = new Nutcracker1Scene(sceneArgs);
                    break;

                case "NUTCRACKER2":
                    scene = new Nutcracker2Scene(sceneArgs);
                    break;

                case "NUTCRACKER3":
                    scene = new Nutcracker3Scene(sceneArgs);
                    break;

                case "HALLOWEEN1":
                    scene = new HalloweenScene1(sceneArgs);
                    break;

                case "HALLOWEENSCENE2013":
                    scene = new HalloweenScene2013(sceneArgs);
                    break;

                case "HALLOWEENSCENE2013B":
                    scene = new HalloweenScene2013B(sceneArgs);
                    break;

                case "XMAS1":
                    scene = new XmasScene1(sceneArgs);
                    break;

                case "XMAS2":
                    scene = new XmasScene2(sceneArgs);
                    break;

                default:
                    throw new ArgumentException("Missing start scene");
            }



            // Register the scene (so it can be properly stopped)
            Executor.Current.Register(scene);

            // Wire up the instantiated expanders
            if (scene is ISceneSupportsSimulator && simForm != null)
            {
                ((ISceneSupportsSimulator)scene).WireUp(simForm);
            }

            if (scene is ISceneRequiresDMXPro)
            {
                if (dmxPro == null)
                    throw new ArgumentNullException("DMXpro not configured");
                ((ISceneRequiresDMXPro)scene).WireUp(dmxPro);
            }

            if (scene is ISceneRequiresIOExpander)
            {
                if (ioExpander == null)
                    throw new ArgumentNullException("IOExpander not configured");
                ((ISceneRequiresIOExpander)scene).WireUp(ioExpander);
            }

            if (scene is ISceneRequiresAcnStream)
            {
                if (acnOutput == null)
                    throw new ArgumentNullException("AcnOutput not configured");
                ((ISceneRequiresAcnStream)scene).WireUp(acnOutput);
            }

            if (scene is ISceneRequiresRaspExpander1)
            {
                if (raspberry1 == null)
                    throw new ArgumentNullException("Raspberry1 not configured");
                ((ISceneRequiresRaspExpander1)scene).WireUp1(raspberry1);
            }

            if (scene is ISceneRequiresRaspExpander2)
            {
                if (raspberry2 == null)
                    throw new ArgumentNullException("Raspberry2 not configured");
                ((ISceneRequiresRaspExpander2)scene).WireUp2(raspberry2);
            }

            if (scene is ISceneRequiresRaspExpander3)
            {
                if (raspberry3 == null)
                    throw new ArgumentNullException("Raspberry3 not configured");
                ((ISceneRequiresRaspExpander3)scene).WireUp3(raspberry3);
            }

            if (scene is ISceneRequiresRaspExpander4)
            {
                if (raspberry4 == null)
                    throw new ArgumentNullException("Raspberry4 not configured");
                ((ISceneRequiresRaspExpander4)scene).WireUp4(raspberry4);
            }

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
