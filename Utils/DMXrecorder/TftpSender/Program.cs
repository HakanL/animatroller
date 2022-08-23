using System.Diagnostics;
using Tftp.Net;

public class Program
{
    private static AutoResetEvent transferFinishedEvent = new AutoResetEvent(false);
    private static long fileLength;

    public static void Main(string[] args)
    {
        try
        {
            if (args.Length < 2)
            {
                Console.WriteLine("TftpSender [Host] [File] ([Dest filename])");
                return;
            }

            string host = args[0];
            string filename = args[1];
            if (!File.Exists(filename))
            {
                Console.WriteLine($"File not found: {filename}");
                return;
            }

            string? destFilename = null;
            if (args.Length > 2)
                destFilename = args[2];

            var client = new TftpClient(host);

            Console.WriteLine($"Uploading {Path.GetFileName(filename)} to {host}");

            if (string.IsNullOrEmpty(destFilename))
                destFilename = Path.GetFileName(filename);

            var result = client.Upload(destFilename);
            result.TransferMode = TftpTransferMode.octet;
            result.BlockSize = 512;

            fileLength = new FileInfo(filename).Length;
            using var fs = File.OpenRead(filename);

            var watch = Stopwatch.StartNew();

            result.OnError += Result_OnError;
            result.OnFinished += Result_OnFinished;
            result.OnProgress += Result_OnProgress;
            result.Start(fs);

            transferFinishedEvent.WaitOne();
            watch.Stop();
            double speed = (double)fileLength / 1024 / (watch.ElapsedMilliseconds / 1000);
            Console.WriteLine($"Duration: {watch.Elapsed}   Speed: {speed:N1} KiB/s");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Unhandled exception: {0}", ex);
        }
    }

    private static void Result_OnProgress(ITftpTransfer transfer, TftpTransferProgress progress)
    {
        Console.Write($"\rTransferred {progress.TransferredBytes:N0} / {fileLength:N0} bytes ({((double)progress.TransferredBytes / fileLength):P2})");
    }

    private static void Result_OnFinished(ITftpTransfer transfer)
    {
        Console.WriteLine();
        Console.WriteLine("Transfer finished");

        transferFinishedEvent.Set();
    }

    private static void Result_OnError(ITftpTransfer transfer, TftpTransferError error)
    {
        Console.WriteLine();
        Console.WriteLine($"Error: {error}");

        transferFinishedEvent.Set();
    }
}
