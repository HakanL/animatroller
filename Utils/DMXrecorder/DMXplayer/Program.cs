using Haukcode.sACN;
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

                var networkInterfaces = SACNCommon.GetCommonInterfaces();
                Console.WriteLine("Network interfaces");
                foreach (var nic in networkInterfaces)
                {
                    Console.WriteLine($"{nic.AdapterName} - {nic.IPAddress}");
                }

                System.Net.IPAddress bindAddress = null;

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

                IOutput output;
                switch (arguments.OutputType)
                {
                    case Arguments.OutputTypes.sACN:
                        output = new AcnStream(bindAddress, priority: 100);
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

                    case Arguments.FileFormats.PCapArtNet:
                        fileReader = new Common.PCapArtNetFileReader(arguments.InputFile);
                        break;

                    default:
                        throw new ArgumentException("Unsupported file format");
                }

                using (var dmxPlayback = new DmxPlayback(fileReader, output))
                {
                    if (!string.IsNullOrEmpty(arguments.UniverseMapping))
                    {
                        var parts = arguments.UniverseMapping.Split(',').Select(x => x.Trim()).ToList();
                        foreach (string part in parts)
                        {
                            var inputOutputParts = part.Split('=').Select(x => x.Trim()).ToList();
                            if (inputOutputParts.Count != 2)
                            {
                                // Ignore
                                Console.WriteLine($"Invalid mapping data: {part}");
                                continue;
                            }

                            if (!ushort.TryParse(inputOutputParts[0], out ushort inputUniverse) || inputUniverse < 1 || inputUniverse > 63999)
                            {
                                // Ignore
                                Console.WriteLine($"Invalid input universe: {inputOutputParts[0]}");
                                continue;
                            }

                            if (!ushort.TryParse(inputOutputParts[1], out ushort outputUniverse) || outputUniverse < 1 || outputUniverse > 63999)
                            {
                                // Ignore
                                Console.WriteLine($"Invalid output universe: {inputOutputParts[1]}");
                                continue;
                            }

                            Console.WriteLine($"Map input universe {inputUniverse} to output universe {outputUniverse}");
                            dmxPlayback.AddUniverseMapping(inputUniverse, outputUniverse);
                        }
                    }

                    Console.CancelKeyPress += (sender, evt) =>
                    {
                        Console.WriteLine("Aborting playback");
                        evt.Cancel = true;
                        dmxPlayback.Cancel();
                    };

                    Console.WriteLine();

                    dmxPlayback.Run(arguments.Loop);

                    Console.WriteLine("Playing back...");

                    dmxPlayback.WaitForCompletion();
                }

                if (arguments.BlackOutAtEnd)
                {
                    Console.WriteLine("Black Out");

                    foreach (int universeId in output.UsedUniverses)
                    {
                        output.SendDmx(universeId, new byte[512]);
                    }
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

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
