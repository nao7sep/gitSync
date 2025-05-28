using System;

namespace gitSyncApp
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
            }
            catch (Exception ex)
            {
                SafeConsoleWriter.Default.EnqueueException(ex, "Error");
            }
            finally
            {
                // Ensure all enqueued console output is written before prompting the user to exit.
                SafeConsoleWriter.Default.Dispose();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}
