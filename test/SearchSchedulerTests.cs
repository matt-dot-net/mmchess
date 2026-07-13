using System;
using System.Threading;
using Xunit;

namespace mmchess;

public class SearchSchedulerTests
{
    [Fact]
    public void ExecutesWorkOnLongLivedWorkers()
    {
        using var scheduler = new SearchScheduler(workerCount: 2, capacity: 4);
        using var completed = new CountdownEvent(4);

        for (var i = 0; i < 4; i++)
            Assert.True(scheduler.TrySchedule(() => completed.Signal()));

        Assert.True(completed.Wait(TimeSpan.FromSeconds(5)));
        Assert.Equal(2, scheduler.WorkerCount);
        Assert.False(scheduler.TryTakeWorkerException(out _));
    }

    [Fact]
    public void QueueIsBoundedAndSchedulingNeverBlocks()
    {
        using var scheduler = new SearchScheduler(workerCount: 1, capacity: 2);
        using var workerStarted = new ManualResetEventSlim();
        using var releaseWorker = new ManualResetEventSlim();

        Assert.True(scheduler.TrySchedule(() =>
        {
            workerStarted.Set();
            releaseWorker.Wait();
        }));
        Assert.True(workerStarted.Wait(TimeSpan.FromSeconds(5)));

        Assert.True(scheduler.TrySchedule(() => { }));
        Assert.True(scheduler.TrySchedule(() => { }));
        Assert.False(scheduler.TrySchedule(() => { }));
        Assert.Equal(2, scheduler.PendingCount);
        releaseWorker.Set();
    }

    [Fact]
    public void WorkerSurvivesFailedWorkItem()
    {
        using var scheduler = new SearchScheduler(workerCount: 1, capacity: 2);
        using var completed = new ManualResetEventSlim();

        Assert.True(scheduler.TrySchedule(() => throw new InvalidOperationException("test")));
        Assert.True(scheduler.TrySchedule(completed.Set));

        Assert.True(completed.Wait(TimeSpan.FromSeconds(5)));
        Assert.True(SpinWait.SpinUntil(
            () => scheduler.TryTakeWorkerException(out _),
            TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void DisposedSchedulerRejectsNewWork()
    {
        var scheduler = new SearchScheduler(workerCount: 0);
        scheduler.Dispose();

        Assert.False(scheduler.TrySchedule(() => { }));
    }

    [Fact]
    public void DisabledSchedulerRejectsWork()
    {
        using var scheduler = new SearchScheduler(workerCount: 0);

        Assert.False(scheduler.TrySchedule(() => { }));
    }
}
