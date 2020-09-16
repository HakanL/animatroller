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
                    var networkInterfaces = Haukcode.sACN.SACNCommon.GetCommonInterfaces();
                    Console.WriteLine("Network adapters");

                    foreach(var nic in networkInterfaces)
                    {
                        Console.WriteLine($"{nic.AdapterName} - {nic.IPAddress}");
                    }
                    Console.WriteLine();

                    IPAddress bindAddress = null;

                    if (!string.IsNullOrEmpty(arguments.NetworkAdapter))
                    {
                        var selectedInterface = networkInterfaces.FirstOrDefault(x => x.AdapterName.Equals(arguments.NetworkAdapter.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (selectedInterface.IPAddress == null)
                            throw new ArgumentException($"Unknown/incorrect network adapter name: {arguments.NetworkAdapter}");

                        bindAddress = selectedInterface.IPAddress;
                    }
                    else
                    {
                        // Select the first that isn't virtual (Hyper-V)
                        var selectedInterface = networkInterfaces.FirstOrDefault(x => !x.AdapterName.StartsWith("vEthernet"));
                        if (selectedInterface.IPAddress != null)
                            bindAddress = selectedInterface.IPAddress;
                    }

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
