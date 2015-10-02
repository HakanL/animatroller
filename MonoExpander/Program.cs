using System;
using System.Collections.Generic;
using PowerArgs;
using NLog;
using System.Threading;

namespace Animatroller.MonoExpander
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var log = LogManager.GetLogger("Program");

            log.Info("Starting");

            try
            {
                var arguments = Args.Parse<Arguments>(args);

                var cts = new CancellationTokenSource();

                Console.CancelKeyPress += (s, e) =>
                {
                    e.Cancel = true;
                    cts.Cancel();
                };

                using (var main = new Main(arguments))
                {
                    main.Execute(cts.Token);
                }

            }
            catch (ArgException ex)
            {
                log.Warn(ex.Message);
                Console.WriteLine(ArgUsage.GenerateUsageFromTemplate<Arguments>());
            }
            catch (Exception ex)
            {
                log.Error(ex, "Unhandled exception");
                Console.WriteLine("Unhandled exception: {0}", ex);
            }

            log.Info("Closing");
        }
    }
}
