using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace gitSyncApp
{
    public class ConsoleChunk
    {
        public string Text { get; set; }
        public ConsoleColor? Foreground { get; set; }
        public ConsoleColor? Background { get; set; }
        public ConsoleChunk(string text, ConsoleColor? fg = null, ConsoleColor? bg = null)
        {
            Text = text;
            Foreground = fg;
            Background = bg;
        }
    }

    public class SafeConsoleWriter : IDisposable
    {
        // BlockingCollection is a thread-safe producer-consumer queue.
        // Multiple threads can safely enqueue items, and a single background thread will consume them.
        private readonly BlockingCollection<List<ConsoleChunk>> _queue = new();
        private readonly Thread _worker;
        private bool _disposed;

        // Singleton instance for global use
        public static SafeConsoleWriter Default { get; } = new SafeConsoleWriter();

        public SafeConsoleWriter()
        {
            _worker = new Thread(ProcessQueue) { IsBackground = true };
            _worker.Start();
        }

        // EnqueueChunks is thread-safe: multiple threads can call this concurrently without locks.
        public void EnqueueChunks(IEnumerable<ConsoleChunk> chunks)
        {
            _queue.Add([.. chunks]);
        }

        // Enqueue an exception and message as a single chunk with red foreground color.
        public void EnqueueException(Exception ex, string? message = null)
        {
            var chunks = new List<ConsoleChunk>();
            if (!string.IsNullOrEmpty(message))
                chunks.Add(new ConsoleChunk(message + ": "));
            chunks.Add(new ConsoleChunk(ex.ToString() + Environment.NewLine, fg: ConsoleColor.Red));
            EnqueueChunks(chunks);
        }

        // ProcessQueue runs in the background and continues until CompleteAdding is called and all items are processed.
        private void ProcessQueue()
        {
            foreach (var chunks in _queue.GetConsumingEnumerable())
            {
                foreach (var chunk in chunks)
                {
                    var origFg = Console.ForegroundColor;
                    var origBg = Console.BackgroundColor;
                    if (chunk.Foreground.HasValue) Console.ForegroundColor = chunk.Foreground.Value;
                    if (chunk.Background.HasValue) Console.BackgroundColor = chunk.Background.Value;
                    Console.Write(chunk.Text);
                    Console.ForegroundColor = origFg;
                    Console.BackgroundColor = origBg;
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // CompleteAdding signals that no more items will be added to the queue.
                // This allows the background thread to finish processing and exit.
                _queue.CompleteAdding();
                // Join waits for the background thread to finish before continuing.
                // This ensures all output is written before disposal completes.
                _worker.Join();
                _disposed = true;
                // GC.SuppressFinalize prevents the GC from calling the finalizer (if present) since cleanup is already done.
                // This does NOT affect garbage collection of managed fields like lists; their memory will still be reclaimed normally.
                GC.SuppressFinalize(this);
            }
        }
    }
}
