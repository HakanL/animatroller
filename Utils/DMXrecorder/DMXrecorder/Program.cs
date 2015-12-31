using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PowerArgs;

namespace Animatroller.DMXrecorder
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var arguments = Args.Parse<Arguments>(args);

                using (var writer = new OutputProcessor(arguments))
                {
                    IRecorder recorder;

                    switch (arguments.InputType)
                    {
                        case Arguments.InputTypes.sACN:
                            recorder = new AcnRecorder(writer, arguments.Universes);
                            break;

                        case Arguments.InputTypes.ArtNet:
                            recorder = new ArtNetRecorder(writer, arguments.Universes);
                            break;

                        default:
                            throw new ArgumentException("Invalid input type");
                    }

                    recorder.StartRecord();

                    Console.WriteLine("Recording...");
                    Console.WriteLine();
                    Console.WriteLine("Press enter to stop recording");

                    Console.ReadLine();

                    recorder.StopRecord();

                    recorder.Dispose();
                    recorder = null;
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
