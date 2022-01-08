using System;
using System.Collections.Generic;
using System.IO;
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
                Common.IInputReader inputReader = null;
                Common.Analyzer analyzer = null;

                if (!string.IsNullOrEmpty(arguments.InputFilename))
                {
                    // Try to determine the format by probing
                    if (!arguments.InputFileFormat.HasValue)
                    {
                        arguments.InputFileFormat = Common.FileFormatProber.ProbeFile(arguments.InputFilename);
                    }

                    switch (arguments.InputFileFormat)
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

                        case Common.FileFormats.FSeq:
                            fileReader = new Common.IO.FseqFileReader(arguments.InputFilename, arguments.InputConfigFile);
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

                var transformer = new OutputWriter(transforms, null, arguments.Loop ?? 0);
                ICommand command = null;

                int[] universeIds = null;
                if (!string.IsNullOrEmpty(arguments.Universes))
                {
                    universeIds = arguments.Universes.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).Select(x => int.Parse(x)).ToArray();
                }

                Common.IO.IFileWriter fileWriter = null;

                switch (arguments.Command)
                {
                    case Arguments.Commands.TrimBlack:
                        command = new Processor.Command.TrimBlack(arguments.FirstFrameBlack);
                        break;

                    case Arguments.Commands.FindLoop:
                        command = new Processor.Command.FindLoop(trimBlack: false, arguments.FirstFrameBlack);
                        break;

                    case Arguments.Commands.TrimBlackFindLoop:
                        command = new Processor.Command.FindLoop(trimBlack: true, arguments.FirstFrameBlack);
                        break;

                    case Arguments.Commands.TrimFrame:
                        command = new Processor.Command.TrimFrame((long?)arguments.TrimStart, (long?)arguments.TrimEnd, (long?)arguments.TrimDuration);
                        break;

                    case Arguments.Commands.TrimTime:
                        command = new Processor.Command.TrimTime(arguments.TrimStart, arguments.TrimEnd, arguments.TrimDuration);
                        break;

                    case Arguments.Commands.Convert:
                        command = new Processor.Command.Convert();
                        break;

                    case Arguments.Commands.Duplicate:
                        command = new Processor.Command.Duplicate(extraCopies: arguments.Loop ?? 1);
                        break;

                    case Arguments.Commands.GenerateStatic:
                        command = new Processor.Command.Generate(
                            Processor.Command.Generate.GenerateSubCommands.Static,
                            universeIds,
                            arguments.Frequency,
                            arguments.TrimDuration.Value,
                            arguments.FillByte);
                        break;

                    case Arguments.Commands.GenerateRamp:
                        command = new Processor.Command.Generate(
                            Processor.Command.Generate.GenerateSubCommands.Ramp,
                            universeIds,
                            arguments.Frequency,
                            arguments.TrimDuration.Value);
                        break;

                    case Arguments.Commands.GenerateSaw:
                        command = new Processor.Command.Generate(
                            Processor.Command.Generate.GenerateSubCommands.Saw,
                            universeIds,
                            arguments.Frequency,
                            arguments.TrimDuration.Value);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException("Unknown command");
                }

                var context = new ProcessorContext
                {
                    FirstSyncTimestampMS = analyzer?.FirstSyncTimestampMS ?? 0,
                    HasSyncFrames = analyzer?.SyncFrameDetected ?? false,
                    TotalFrames = inputReader?.TotalFrames ?? 0,
                    InputFilename = arguments.InputFilename
                };

                if (command is ICommandInputOutput || command is ICommandOutput)
                {
                    if (string.IsNullOrEmpty(arguments.OutputFilename))
                    {
                        // Use the input name
                        arguments.OutputFilename = Path.Combine(Path.GetDirectoryName(arguments.InputFilename),
                            Path.GetFileNameWithoutExtension(arguments.InputFilename) + "_out" + Path.GetExtension(arguments.InputFilename));
                    }

                    if (!arguments.OutputFileFormat.HasValue)
                        arguments.OutputFileFormat = arguments.InputFileFormat;

                    if (!arguments.OutputFileFormat.HasValue)
                        // Default
                        arguments.OutputFileFormat = Common.FileFormats.PCapAcn;

                    switch (arguments.OutputFileFormat)
                    {
                        case Common.FileFormats.Binary:
                            fileWriter = new Common.IO.BinaryFileWriter(arguments.OutputFilename);
                            break;

                        case Common.FileFormats.PCapAcn:
                            fileWriter = new Common.IO.PCapAcnFileWriter(arguments.OutputFilename);
                            break;

                        case Common.FileFormats.PCapArtNet:
                            fileWriter = new Common.IO.PCapArtNetFileWriter(arguments.OutputFilename);
                            break;

                        default:
                            throw new ArgumentException("Unhandled output file format " + arguments.OutputFileFormat);
                    }
                }

                transformer.FileWriter = fileWriter;

                if (command is ICommandInputOutput commandInputOutput)
                    commandInputOutput.Execute(context, inputReader, transformer);
                else if (command is ICommandOutput commandOutput)
                    commandOutput.Execute(context, transformer);
                else if (command is ICommandInput commandInput)
                    commandInput.Execute(context, inputReader);

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
