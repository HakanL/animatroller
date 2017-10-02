using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using Animatroller.Framework;
using Animatroller.Framework.Extensions;
using Expander = Animatroller.Framework.Expander;
using Animatroller.Framework.LogicalDevice;
using Controller = Animatroller.Framework.Controller;
using Effect = Animatroller.Framework.Effect;
using Effect2 = Animatroller.Framework.Effect2;
using Physical = Animatroller.Framework.PhysicalDevice;
using Import = Animatroller.Framework.Import;
using System.IO;

namespace Animatroller.Scenes
{
    internal class MatrixTest1 : BaseScene
    {
        const int SacnUniverse1 = 1;

        Expander.AcnStream acnOutput = new Expander.AcnStream();
        VirtualPixel2D3 pixelsMatrix = new VirtualPixel2D3(64, 32);
        ColorDimmer3 testLight = new ColorDimmer3();
        Dimmer3 testLight2 = new Dimmer3();

        DigitalInput2 buttonTest = new DigitalInput2();

        public MatrixTest1(IEnumerable<string> args)
        {
            string expanderFilesFolder = string.Empty;
            string expFilesParam = args.FirstOrDefault(x => x.StartsWith("EXPFILES"));
            if (!string.IsNullOrEmpty(expFilesParam))
            {
                string[] parts = expFilesParam.Split('=');
                if (parts.Length == 2)
                    expanderFilesFolder = parts[1];
            }

            //var pixelMapping = Framework.Utility.PixelMapping.GeneratePixelMapping(
            //    width: 64,
            //    height: 32,
            //    pixelOrder: Framework.Utility.PixelOrder.HorizontalSnakeBottomLeft);
            //acnOutput.Connect(new Physical.Pixel2D(pixelsMatrix, pixelMapping), SacnUniverse1, 1);

            acnOutput.Connect(new Physical.GenericPixelRGB(testLight, 1), SacnUniverse1);
//            acnOutput.Connect(new Physical.GenericDimmer(testLight2, 1), SacnUniverse1);

            buttonTest.Output.Subscribe(x =>
                {
                    if (x)
                    {
                        Color rndCol = Color.FromArgb(random.Next(256), random.Next(256), random.Next(256));
                        pixelsMatrix.InjectRow(rndCol, 1.0);

                        testLight2.SetBrightness(rndCol.R.GetDouble());

                        testLight.SetColor(rndCol, 1.0);
                    }
                });
        }
    }
}
