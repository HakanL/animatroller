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

                    case Arguments.OutputTypes.ArtNet:
                        output = new ArtNetStream(bindAddress);
                        break;

                    default:
                        throw new ArgumentException("Unsupported output type");
                }

                // Try to determine the format by probing
                if (!arguments.FileFormat.HasValue)
                {
                    arguments.FileFormat = Common.FileFormatProber.ProbeFile(arguments.InputFilename);
                }

                Common.IO.IFileReader fileReader;
                switch (arguments.FileFormat)
                {
                    case Common.FileFormats.Binary:
                        fileReader = new Common.IO.BinaryFileReader(arguments.InputFilename);
                        break;

                    case Common.FileFormats.PCapAcn:
                        fileReader = new Common.IO.PCapAcnFileReader(arguments.InputFilename);
                        break;

                    case Common.FileFormats.PCapArtNet:
                        fileReader = new Common.IO.PCapArtNetFileReader(arguments.InputFilename);
                        break;

                    default:
                        throw new ArgumentException("Unsupported file format");
                }

                var inputReader = new Common.InputReader(fileReader);
                var analyzer = new Common.Analyzer(inputReader);

                analyzer.Analyze();

                // Rewind so we'll start from the beginning
                inputReader.Rewind();

                int frequencyHertz = 40;
                int sendSyncAddress;
                if (analyzer.SyncFrameDetected)
                {
                    sendSyncAddress = 1;

                    if (analyzer.IsOptimizedStream.HasValue)
                    {
                        if (analyzer.IsOptimizedStream == true)
                            frequencyHertz = analyzer.ShortestFrequency.Value;
                        else
                            frequencyHertz = analyzer.AdjustedFrequency.Value;
                    }
                }
                else
                {
                    sendSyncAddress = 0;
                }

                using (var dmxPlayback = new DmxPlayback(inputReader, output, 1000 / frequencyHertz, sendSyncAddress))
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

                            if (!int.TryParse(inputOutputParts[0], out int inputUniverse) || inputUniverse < 1 || inputUniverse > 63999)
                            {
                                // Ignore
                                Console.WriteLine($"Invalid input universe: {inputOutputParts[0]}");
                                continue;
                            }

                            if (!int.TryParse(inputOutputParts[1], out int outputUniverse) || outputUniverse < 1 || outputUniverse > 63999)
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

                    if(sendSyncAddress > 0)
                    {
                        output.SendSync(sendSyncAddress);
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
