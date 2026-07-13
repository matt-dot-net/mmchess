using System;
using System.Collections.Concurrent;
using System.Threading;

namespace mmchess;

// A fixed set of long-lived search workers backed by a bounded queue. The
// search owner is not represented here: ThreadCount=N creates N-1 workers so
// the owner remains available to search and join work itself.
public sealed class SearchScheduler : IDisposable
{
    readonly BlockingCollection<Action> workQueue;
    readonly ConcurrentQueue<Exception> workerExceptions = new ConcurrentQueue<Exception>();
    readonly Thread[] workers;
    int disposed;

    public int WorkerCount => workers.Length;
    public int Capacity { get; }
    public int PendingCount => workQueue.Count;
    public bool IsEnabled => WorkerCount > 0;

    public SearchScheduler(int workerCount, int? capacity = null)
    {
        if (workerCount < 0)
            throw new ArgumentOutOfRangeException(nameof(workerCount));

        Capacity = capacity ?? Math.Max(1, workerCount * 2);
        if (Capacity < 1)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        workQueue = new BlockingCollection<Action>(Capacity);
        workers = new Thread[workerCount];
        for (var i = 0; i < workers.Length; i++)
        {
            workers[i] = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = $"mmchess-search-{i + 1}"
            };
            workers[i].Start();
        }
    }

    // Never blocks. If all queue slots are occupied, the owner should execute
    // the work locally or defer it instead of waiting for a worker.
    public bool TrySchedule(Action work)
    {
        if (work == null)
            throw new ArgumentNullException(nameof(work));
        if (!IsEnabled || Volatile.Read(ref disposed) != 0)
            return false;

        try
        {
            return workQueue.TryAdd(work);
        }
        catch (InvalidOperationException)
        {
            // Dispose may complete the queue between the disposed check and
            // TryAdd. Treat that race exactly like a full/closed scheduler.
            return false;
        }
    }

    public bool TryTakeWorkerException(out Exception exception)
    {
        return workerExceptions.TryDequeue(out exception);
    }

    void WorkerLoop()
    {
        foreach (var work in workQueue.GetConsumingEnumerable())
        {
            try
            {
                work();
            }
            catch (Exception ex)
            {
                // A failed work item must not terminate a long-lived worker.
                // Split work will normally capture errors in its own result;
                // this queue is the scheduler-level safety net.
                workerExceptions.Enqueue(ex);
            }
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
            return;

        workQueue.CompleteAdding();
        foreach (var worker in workers)
        {
            if (worker != Thread.CurrentThread)
                worker.Join();
        }
        workQueue.Dispose();
    }
}
