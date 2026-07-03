using System;
using System.Collections.Concurrent;
using System.Threading;

namespace mmchess;

// Console.In.Peek()/Console.KeyAvailable block on redirected (piped) stdin
// when no data has arrived yet, which is the normal state while a GUI is
// waiting on us to think. A single background thread owns the blocking
// Console.ReadLine() calls and hands lines off through this queue, so the
// search's periodic interrupt check can poll it without ever blocking.
public static class ConsoleInputQueue
{
    static readonly BlockingCollection<string> Queue = new BlockingCollection<string>();
    static Thread _readerThread;
    static readonly object StartLock = new object();

    public static void Start()
    {
        lock (StartLock)
        {
            if (_readerThread != null)
                return;

            _readerThread = new Thread(ReadLoop) { IsBackground = true };
            _readerThread.Start();
        }
    }

    static void ReadLoop()
    {
        string line;
        while ((line = Console.ReadLine()) != null)
            Queue.Add(line);

        Queue.CompleteAdding();
    }

    // Blocks until a line is available; returns null once stdin has closed.
    public static string ReadLine()
    {
        try
        {
            return Queue.Take();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    // Non-blocking: returns true and sets line if one was already queued.
    public static bool TryReadLine(out string line)
    {
        return Queue.TryTake(out line);
    }
}
