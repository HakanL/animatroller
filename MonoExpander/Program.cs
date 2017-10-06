using System;
using System.Collections.Generic;
using PowerArgs;
using Serilog;
using System.Threading;
using System.IO;

namespace Animatroller.MonoExpander
{
    public class Program
    {
        private static ILogger log;
        private const string FileTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Logger} [{Level}] {Message}{NewLine}{Exception}";
        private const string TraceTemplate = "{Timestamp:HH:mm:ss.fff} {Logger} [{Level}] {Message}{NewLine}{Exception}";

        public static void Main(string[] args)
        {
            var logConfig = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Verbose()
                .WriteTo.ColoredConsole(outputTemplate: TraceTemplate)
                .WriteTo.Trace(outputTemplate: TraceTemplate)
                .WriteTo.RollingFile(
                    pathFormat: Path.Combine(AppContext.BaseDirectory, "Logs", "log-{Date}.txt"),
                    outputTemplate: FileTemplate);

            log = Log.Logger = logConfig.CreateLogger();

            log.Information("Starting up!");

            try
            {
                var arguments = Args.Parse<Arguments>(args);

                var cts = new CancellationTokenSource();

                Console.CancelKeyPress += (s, e) =>
                {
                    e.Cancel = true;
                    cts.Cancel();
                };

                try
                {
                    using (var main = new Main(arguments))
                    {
                        main.Execute(cts.Token);
                    }
                }
                finally
                {
                    Console.CursorVisible = true;
                }

            }
            catch (ArgException ex)
            {
                log.Warning(ex.Message);
                Console.WriteLine(ArgUsage.GenerateUsageFromTemplate<Arguments>());
            }
            catch (Exception ex)
            {
                log.Error(ex, "Unhandled exception");
                Console.WriteLine("Unhandled exception: {0}", ex);
            }

            log.Information("Closing");
        }
    }
}
