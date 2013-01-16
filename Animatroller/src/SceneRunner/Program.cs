using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Framework;

namespace Animatroller.SceneRunner
{
    public class Program
    {
        static void Main(string[] args)
        {
            Animatroller.Simulator.SimulatorForm simForm = null;
            Animatroller.Framework.Expander.DMXPro dmxPro = null;
            Animatroller.Framework.Expander.IOExpander ioExpander = null;
            Animatroller.Framework.Expander.AcnStream acnOutput = null;

            var sceneArgs = new List<string>();
            foreach (var arg in args)
            {
                switch (arg)
                {
                    case "SIM":
                        simForm = new Animatroller.Simulator.SimulatorForm();
                        break;

                    case "DMXPRO":
                        dmxPro = new Animatroller.Framework.Expander.DMXPro(Properties.Settings.Default.DMXProPort);
                        break;

                    case "IOEXP":
                        ioExpander = new Animatroller.Framework.Expander.IOExpander(Properties.Settings.Default.IOExpanderPort);
                        break;

                    case "ACN":
                        acnOutput = new Framework.Expander.AcnStream();
                        break;

                    default:
                        sceneArgs.Add(arg);
                        break;
                }
            }

            //var scene = new TestScene();
            //            var scene = new TestScene2();
            //var scene = new HalloweenScene();
            //var scene = new XmasScene();
            var scene = new XmasScene2(sceneArgs);

            if (simForm != null)
                scene.WireUp(simForm);
            if (dmxPro != null)
                scene.WireUp(dmxPro);
            if (ioExpander != null)
                scene.WireUp(ioExpander);
            if (acnOutput != null)
                scene.WireUp(acnOutput);

            Executor.Current.Start();
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
