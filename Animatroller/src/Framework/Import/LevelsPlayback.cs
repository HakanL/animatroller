using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Animatroller.Framework.Controller;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.Import.FileFormat;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.Import
{
    public class LevelsPlayback
    {
        private string name;
        //private IReceivesBrightness device;
        private double[] brightnessData;
        private string currentLoadedFile;
        private Subject<double> output;

        public LevelsPlayback([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            this.name = name;
            this.output = new Subject<double>();
        }

        public IObservable<double> Output
        {
            get { return this.output.AsObservable(); }
        }

/*        public void SetOutput(IReceivesBrightness device)
        {
            if (this.device != null)
                throw new ArgumentException("Can only control one device");

            this.device = device;
        }*/

        public string Name
        {
            get { return this.name; }
        }

        public void Load(string fileName)
        {
            if (fileName == this.currentLoadedFile && this.brightnessData != null && this.brightnessData.Length > 0)
                return;

            using (var fs = File.OpenRead(fileName))
            {
                this.brightnessData = new double[fs.Length];

                int pos = 0;
                using (var br = new BinaryReader(fs))
                {
                    while (pos < fs.Length)
                    {
                        this.brightnessData[pos++] = br.ReadByte() / 255.0;
                    }
                }
            }
            this.currentLoadedFile = fileName;
        }

        public CancellationTokenSource Start(IControlToken token = null, int priority = 1)
        {
            //if (this.device == null)
            //    throw new ArgumentException("No device configured");

            int durationMs = (int)(this.brightnessData.Length / 40 * 1000);

            IControlToken localToken = null;
            IControlToken controlToken = token;
            if (controlToken == null)
            {
//                localToken = controlToken = this.device.TakeControl(priority: priority);
            }

            var observer = Observer.Create<double>(pos =>
            {
                int arrayPos = (int)((this.brightnessData.Length - 1) * pos);

                this.output.OnNext(this.brightnessData[arrayPos]);
//                this.device.SetBrightness(this.brightnessData[arrayPos], controlToken);
            }, () =>
            {
                localToken?.Dispose();
                localToken = null;
            });

            return Executor.Current.TimerJobRunner.AddTimerJobPos(observer, durationMs);
        }
    }
}
