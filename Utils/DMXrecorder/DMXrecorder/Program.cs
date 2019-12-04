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
                    var interfaces = Haukcode.sACN.SACNCommon.GetCommonInterfaces();
                    Console.WriteLine("Network adapters");

                    for (int i = 0; i < interfaces.Count; i++)
                    {
                        Console.WriteLine($"{i + 1}. {interfaces[i].AdapterName} - {interfaces[i].IPAddress}");
                    }
                    Console.WriteLine();

                    IPAddress bindAddress = null;
                    if (arguments.NetworkAdapterIndex > 0)
                    {
                        if (arguments.NetworkAdapterIndex > interfaces.Count)
                            throw new ArgumentOutOfRangeException("Invalid network adapter index");

                        bindAddress = interfaces[arguments.NetworkAdapterIndex - 1].IPAddress;
                    }
                    else
                        bindAddress = Haukcode.sACN.SACNCommon.GetFirstBindAddress();

                    Console.WriteLine($"Binding to network adapter with IP {bindAddress}");
                    Console.WriteLine();

                    IRecorder recorder;

                    switch (arguments.InputType)
                    {
                        case Arguments.InputTypes.sACN:
                            recorder = new AcnRecorder(writer, arguments.Universes, bindAddress);
                            break;

                        case Arguments.InputTypes.ArtNet:
                            throw new NotImplementedException();
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
