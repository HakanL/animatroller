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

                using (var fileReader = new Common.BinaryFileReader(arguments.Inputfile))
                {
                    Common.BinaryFileWriter fileWriter = null;

                    switch (arguments.Command)
                    {
                        case Arguments.Commands.TrimBlack:
                            fileWriter = new Common.BinaryFileWriter(arguments.OutputFile);
                            var trimBlackCommand = new TrimBlack(fileReader, fileWriter);
                            trimBlackCommand.Execute();
                            break;

                        case Arguments.Commands.FindLoop:
                            var findLoopCommand = new FindLoop(fileReader);
                            findLoopCommand.Execute();
                            break;

                        case Arguments.Commands.TrimEnd:
                            fileWriter = new Common.BinaryFileWriter(arguments.OutputFile);
                            var trimEndCommand = new TrimEnd(fileReader, fileWriter, arguments.TrimPos);
                            trimEndCommand.Execute();
                            break;
                    }

                    if (fileWriter != null)
                        fileWriter.Dispose();
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
