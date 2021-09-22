using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Processor;
using Animatroller.Processor.Transform;
using PowerArgs;

namespace Animatroller.PostProcessor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var arguments = Args.Parse<Arguments>(args);

                Common.IO.IFileReader fileReader = null;
                Common.IO.IFileWriter fileWriter;
                Common.IInputReader inputReader = null;
                Common.Analyzer analyzer = null;

                if (!string.IsNullOrEmpty(arguments.InputFile))
                {
                    switch (arguments.InputFileFormat)
                    {
                        case Arguments.FileFormats.Binary:
                            fileReader = new Common.IO.BinaryFileReader(arguments.InputFile);
                            break;

                        case Arguments.FileFormats.PCapAcn:
                            fileReader = new Common.IO.PCapAcnFileReader(arguments.InputFile);
                            break;

                        case Arguments.FileFormats.PCapArtNet:
                            fileReader = new Common.IO.PCapArtNetFileReader(arguments.InputFile);
                            break;

                        case Arguments.FileFormats.FSeq:
                            fileReader = new Common.IO.FseqFileReader(arguments.InputFile, arguments.InputConfigFile);
                            break;

                        default:
                            throw new ArgumentException("Unhandled input file format " + arguments.InputFileFormat);
                    }

                    inputReader = new Common.InputReader(fileReader);
                    analyzer = new Common.Analyzer(inputReader);

                    analyzer.Analyze();

                    // Rewind so we'll start from the beginning
                    inputReader.Rewind();
                }


                if (!string.IsNullOrEmpty(arguments.OutputFile))
                {
                    switch (arguments.OutputFileFormat)
                    {
                        case Arguments.FileFormats.Binary:
                            fileWriter = new Common.IO.BinaryFileWriter(arguments.OutputFile);
                            break;

                        case Arguments.FileFormats.PCapAcn:
                            fileWriter = new Common.IO.PCapAcnFileWriter(arguments.OutputFile);
                            break;

                        case Arguments.FileFormats.PCapArtNet:
                            fileWriter = new Common.IO.PCapArtNetFileWriter(arguments.OutputFile);
                            break;

                        default:
                            throw new ArgumentException("Unhandled output file format " + arguments.OutputFileFormat);
                    }
                }
                else
                {
                    fileWriter = null;
                }

                var transforms = new List<IBaseTransform>();

                transforms.Add(new UniverseReporter());

                var enhancers = new HashSet<string>((arguments.Enhancers ?? string.Empty).Split(',').Select(x => x.Trim().ToLower()).Where(x => !string.IsNullOrEmpty(x)));

                if (analyzer != null && analyzer.SyncFrameDetected)
                {
                    if (enhancers.Contains("timestampfixer"))
                        transforms.Add(new TimestampFixerWithSync(analyzer.AdjustedIntervalMS, adjustTolerancePercent: 80));
                }

                // Test
#if DEBUG
                if (enhancers.Contains("brightnessfixer"))
                    transforms.Add(new BrightnessFixer());
#endif

                if (!string.IsNullOrEmpty(arguments.UniverseMapping))
                {
                    Processor.Transform.UniverseMapper mapper = null;

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

                        if (mapper == null)
                        {
                            mapper = new Processor.Transform.UniverseMapper();
                            transforms.Add(mapper);
                        }

                        mapper.AddUniverseMapping(inputUniverse, outputUniverse);
                    }
                }

                var transformer = new Transformer(transforms, fileWriter, arguments.Loop ?? 0);
                ICommand command = null;

                int[] universeIds = null;
                if (!string.IsNullOrEmpty(arguments.Universes))
                {
                    universeIds = arguments.Universes.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).Select(x => int.Parse(x)).ToArray();
                }

                switch (arguments.Command)
                {
                    case Arguments.Commands.TrimBlack:
                        if (fileWriter == null)
                            throw new ArgumentNullException("Missing output file");

                        command = new Processor.Command.TrimBlack(inputReader, transformer);
                        break;

                    case Arguments.Commands.FindLoop:
                        command = new Processor.Command.FindLoop(inputReader, transformer);
                        break;

                    case Arguments.Commands.Trim:
                        if (fileWriter == null)
                            throw new ArgumentNullException("Missing output file");

                        command = new Processor.Command.Trim(inputReader, arguments.TrimStart, arguments.TrimEnd, arguments.TrimCount, transformer);
                        break;

                    case Arguments.Commands.FileConvert:
                        if (fileWriter == null)
                            throw new ArgumentNullException("Missing output file");

                        command = new Processor.Command.FileConvert(inputReader, transformer);
                        break;

                    case Arguments.Commands.Generate:
                        if (fileWriter == null)
                            throw new ArgumentNullException("Missing output file");

                        command = new Processor.Command.Generate(transformer, universeIds, arguments.Frequency.Value, arguments.TrimCount.Value);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException("Unknown command");
                }

                var context = new TransformContext
                {
                    FirstSyncTimestampMS = analyzer?.FirstSyncTimestampMS ?? 0,
                    HasSyncFrames = analyzer?.SyncFrameDetected ?? false
                };

                command.Execute(context);

                transformer.WriteOutput();

                (fileReader as IDisposable)?.Dispose();
                (fileWriter as IDisposable)?.Dispose();
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
