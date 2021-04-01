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

                Common.IFileReader fileReader;
                Common.IFileWriter fileWriter;
                switch (arguments.InputFileFormat)
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

                    case Arguments.FileFormats.FSeq:
                        fileReader = new Common.FseqFileReader(arguments.InputFile, arguments.InputConfigFile);
                        break;

                    default:
                        throw new ArgumentException("Unhandled input file format " + arguments.InputFileFormat);
                }

                var analyzer = new Common.Analyzer(fileReader);

                analyzer.Analyze();

                // Rewind so we'll start from the beginning
                fileReader.Rewind();

                if (!string.IsNullOrEmpty(arguments.OutputFile))
                {
                    switch (arguments.OutputFileFormat)
                    {
                        case Arguments.FileFormats.Binary:
                            fileWriter = new Common.BinaryFileWriter(arguments.OutputFile);
                            break;

                        case Arguments.FileFormats.PCapAcn:
                            fileWriter = new Common.PCapAcnFileWriter(arguments.OutputFile);
                            break;

                        case Arguments.FileFormats.PCapArtNet:
                            fileWriter = new Common.PCapArtNetFileWriter(arguments.OutputFile);
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

                if (analyzer.SyncFrameDetected)
                {
                    if (enhancers.Contains("timestampfixer"))
                        transforms.Add(new TimestampFixerWithSync(analyzer.AdjustedTiming, adjustTolerancePercent: 80));
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

                var transformer = new Transformer(transforms);
                ICommand command = null;

                switch (arguments.Command)
                {
                    case Arguments.Commands.TrimBlack:
                        if (fileWriter == null)
                            throw new ArgumentNullException("Missing output file");

                        command = new Processor.Command.TrimBlack(fileReader, fileWriter, transformer);
                        break;

                    case Arguments.Commands.FindLoop:
                        command = new Processor.Command.FindLoop(fileReader, transformer);
                        break;

                    case Arguments.Commands.Trim:
                        if (fileWriter == null)
                            throw new ArgumentNullException("Missing output file");

                        command = new Processor.Command.Trim(fileReader, fileWriter, arguments.TrimStart, arguments.TrimEnd, arguments.TrimCount, transformer);
                        break;

                    case Arguments.Commands.FileConvert:
                        if (fileWriter == null)
                            throw new ArgumentNullException("Missing output file");

                        command = new Processor.Command.FileConvert(fileReader, fileWriter, transformer);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException("Unknown command");
                }

                var context = new TransformContext
                {
                    FirstSyncTimestampMS = analyzer.FirstSyncTimestampMS,
                    HasSyncFrames = analyzer.SyncFrameDetected
                };
                command.Execute(context);

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
