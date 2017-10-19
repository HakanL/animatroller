using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

                    case Arguments.FileFormats.FSeq:
                        fileReader = new Common.FseqFileReader(arguments.InputFile, arguments.InputConfigFile);
                        break;

                    default:
                        throw new ArgumentException("Unhandled input file format " + arguments.InputFileFormat);
                }

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

                        default:
                            throw new ArgumentException("Unhandled output file format " + arguments.OutputFileFormat);
                    }
                }
                else
                    fileWriter = null;

                ICommand command = null;

                switch (arguments.Command)
                {
                    case Arguments.Commands.TrimBlack:
                        if (fileWriter == null)
                            throw new ArgumentNullException("Missing output file");

                        command = new Command.TrimBlack(fileReader, fileWriter);
                        break;

                    case Arguments.Commands.FindLoop:
                        command = new Command.FindLoop(fileReader);
                        break;

                    case Arguments.Commands.TrimEnd:
                        if (fileWriter == null)
                            throw new ArgumentNullException("Missing output file");

                        command = new Command.TrimEnd(fileReader, fileWriter, arguments.TrimPos);
                        break;

                    case Arguments.Commands.FileConvert:
                        if (fileWriter == null)
                            throw new ArgumentNullException("Missing output file");

                        command = new Command.FileConvert(fileReader, fileWriter);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException("Unknown command");
                }

                command.Execute();

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
