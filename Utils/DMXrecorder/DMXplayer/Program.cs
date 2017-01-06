using PowerArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.DMXplayer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var arguments = Args.Parse<Arguments>(args);

                IOutput output;
                switch (arguments.OutputType)
                {
                    case Arguments.OutputTypes.sACN:
                        output = new AcnStream();
                        break;

                    default:
                        throw new ArgumentException("Unsupported output type");
                }

                Common.IFileReader fileReader;
                switch (arguments.FileFormat)
                {
                    case Arguments.FileFormats.Binary:
                        fileReader = new Common.BinaryFileReader(arguments.InputFile);
                        break;

                    case Arguments.FileFormats.PCapAcn:
                        fileReader = new Common.PCapAcnFileReader(arguments.InputFile);
                        break;

                    default:
                        throw new ArgumentException("Unsupported file format");
                }

                using (var dmxPlayback = new DmxPlayback(fileReader, output))
                {
                    dmxPlayback.Run(arguments.Loop);

                    Console.WriteLine("Playing back...");

                    dmxPlayback.WaitForCompletion();
                }
            }
            catch (ArgException ex)
            {
                Console.WriteLine("Argument error {0}", ex.Message);

                Console.WriteLine(ArgUsage.GenerateUsageFromTemplate<Arguments>());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unhandled exception: {0}", ex);
            }
        }
    }
}
