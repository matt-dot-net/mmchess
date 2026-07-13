using System;
using System.Threading;
using System.Threading.Tasks;
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
    public void IdleWorkerStateTracksRunningWork()
    {
        using var scheduler = new SearchScheduler(workerCount: 1, capacity: 1);
        using var started = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();

        Assert.True(scheduler.HasIdleWorker);
        Assert.True(scheduler.TrySchedule(() =>
        {
            started.Set();
            release.Wait();
        }));
        Assert.True(started.Wait(TimeSpan.FromSeconds(5)));
        Assert.False(scheduler.HasIdleWorker);

        release.Set();
        Assert.True(SpinWait.SpinUntil(
            () => scheduler.HasIdleWorker,
            TimeSpan.FromSeconds(5)));
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

    [Fact]
    public async Task FirstRootMoveSearchDoesNotWaitForAWorker()
    {
        var board = Board.ParseFenString("1r2k3/8/8/8/8/8/8/K6r w - - 0 1");
        var state = new GameState { GameBoard = board };
        state.SetThreadCount(2);
        using var workerStarted = new ManualResetEventSlim();
        using var releaseWorker = new ManualResetEventSlim();

        try
        {
            Assert.True(state.SearchScheduler.TrySchedule(() =>
            {
                workerStarted.Set();
                releaseWorker.Wait();
            }));
            Assert.True(workerStarted.Wait(TimeSpan.FromSeconds(5)));

            var search = Task.Run(() =>
            {
                var context = new AlphaBetaContext(state, board);
                var alphaBeta = new AlphaBeta(state, () => { });
                var score = alphaBeta.SearchRoot(context, -10000, 10000, 1);
                return (score, context.PrincipalVariation[0, 0]);
            });

            var result = await search.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(result.Item2.IsNull);
            Assert.Equal(56, (int)result.Item2.From);
            Assert.Equal(48, (int)result.Item2.To);
        }
        finally
        {
            releaseWorker.Set();
            state.SearchScheduler.Dispose();
        }
    }

    [Fact]
    public void ParallelRootScoutsMatchSequentialSearchAndPreserveParentBoard()
    {
        TranspositionTable.Clear();
        var sequentialBoard = new Board();
        var sequentialState = new GameState { GameBoard = sequentialBoard };
        var sequentialContext = new AlphaBetaContext(sequentialState, sequentialBoard);
        var sequentialSearch = new AlphaBeta(sequentialState, () => { });
        var sequentialScore = sequentialSearch.SearchRoot(sequentialContext, -10000, 10000, 3);
        var sequentialMove = sequentialContext.PrincipalVariation[0, 0];
        TranspositionTable.Clear();

        var parallelBoard = new Board();
        var originalHash = parallelBoard.HashKey;
        var originalHistoryCount = parallelBoard.History.Count;
        var parallelState = new GameState { GameBoard = parallelBoard };
        parallelState.SetThreadCount(3);

        try
        {
            var parallelContext = new AlphaBetaContext(parallelState, parallelBoard);
            var parallelSearch = new AlphaBeta(parallelState, () => { });
            var parallelScore = parallelSearch.SearchRoot(parallelContext, -10000, 10000, 3);

            Assert.Equal(sequentialScore, parallelScore);
            Assert.Equal(sequentialMove, parallelContext.PrincipalVariation[0, 0]);
            Assert.True(parallelContext.Metrics.WorkItemsScheduled > 0);
            Assert.Equal(
                parallelContext.Metrics.WorkItemsScheduled,
                parallelContext.Metrics.WorkItemsStarted);
            Assert.Equal(
                parallelContext.Metrics.WorkItemsScheduled,
                parallelContext.Metrics.WorkItemsCompleted);
            Assert.Equal(originalHash, parallelBoard.HashKey);
            Assert.Equal(originalHistoryCount, parallelBoard.History.Count);
            Assert.Equal(0, parallelContext.Ply);
        }
        finally
        {
            parallelState.SearchScheduler.Dispose();
        }
    }

    [Fact]
    public void ParallelBetaCutoffCancelsSplitAndLeavesSchedulerReusable()
    {
        var board = Board.ParseFenString("3q3k/8/8/8/8/8/8/3QK3 w - - 0 1");
        var state = new GameState { GameBoard = board };
        state.SetThreadCount(3);

        try
        {
            var context = new AlphaBetaContext(state, board);
            var quietFirstMove = Move.ParseMove(board, "e1e2");
            Assert.False(quietFirstMove.IsNull);
            context.PrincipalVariation[0, 0] = quietFirstMove;

            var search = new AlphaBeta(state, () => { });
            var score = search.SearchRoot(context, -10000, 500, 4);

            Assert.True(score >= 500);
            Assert.True(context.Metrics.ParallelBetaCutoffs > 0);
            Assert.Equal(
                context.Metrics.WorkItemsScheduled,
                context.Metrics.WorkItemsCompleted);
            Assert.False(state.TimeUp);

            using var reused = new ManualResetEventSlim();
            Assert.True(state.SearchScheduler.TrySchedule(reused.Set));
            Assert.True(reused.Wait(TimeSpan.FromSeconds(5)));
        }
        finally
        {
            state.SearchScheduler.Dispose();
        }
    }

    [Fact]
    public void InternalSplitMatchesSequentialSearchAndStopsAtOneLevel()
    {
        const string fen = "7k/8/8/2ppp3/3Q4/8/8/7K w - - 0 1";
        var sequentialBoard = Board.ParseFenString(fen);
        var sequentialState = new GameState { GameBoard = sequentialBoard };
        var sequentialContext = new AlphaBetaContext(sequentialState, sequentialBoard);
        var sequentialSearch = new AlphaBeta(sequentialState, () => { });
        var sequentialScore = sequentialSearch.Search(sequentialContext, -10000, 10000, 6);
        TranspositionTable.Clear();

        var parallelBoard = Board.ParseFenString(fen);
        var originalHash = parallelBoard.HashKey;
        var originalHistoryCount = parallelBoard.History.Count;
        var parallelState = new GameState { GameBoard = parallelBoard };
        parallelState.SetThreadCount(3);

        try
        {
            var parallelContext = new AlphaBetaContext(parallelState, parallelBoard);
            var parallelSearch = new AlphaBeta(parallelState, () => { });
            var parallelScore = parallelSearch.Search(parallelContext, -10000, 10000, 6);

            Assert.Equal(sequentialScore, parallelScore);
            Assert.Equal(1UL, parallelContext.Metrics.SplitPointsCreated);
            Assert.True(parallelContext.Metrics.WorkItemsScheduled > 0);
            Assert.Equal(
                parallelContext.Metrics.WorkItemsScheduled,
                parallelContext.Metrics.WorkItemsCompleted);
            Assert.Equal(originalHash, parallelBoard.HashKey);
            Assert.Equal(originalHistoryCount, parallelBoard.History.Count);
            Assert.Equal(0, parallelContext.Ply);
        }
        finally
        {
            parallelState.SearchScheduler.Dispose();
        }
    }

    [Fact]
    public void RootSplitPreventsNestedInternalSplitPoints()
    {
        var board = Board.ParseFenString("7k/8/8/2ppp3/3Q4/8/8/7K w - - 0 1");
        var state = new GameState { GameBoard = board };
        state.SetThreadCount(3);

        try
        {
            var context = new AlphaBetaContext(state, board);
            var search = new AlphaBeta(state, () => { });

            search.SearchRoot(context, -10000, 10000, 7);

            Assert.True(context.Metrics.SplitPointsCreated >= 1);
            Assert.Equal(1, context.Metrics.MaxSplitNesting);
            Assert.Equal(
                context.Metrics.WorkItemsScheduled,
                context.Metrics.WorkItemsCompleted);
        }
        finally
        {
            state.SearchScheduler.Dispose();
        }
    }

    [Theory]
    [InlineData(5, 0)]
    [InlineData(6, 1)]
    public void InternalSplitPolicyRejectsShallowOrNestedNodes(int depth, int splitNesting)
    {
        var board = Board.ParseFenString("7k/8/8/2ppp3/3Q4/8/8/7K w - - 0 1");
        var state = new GameState { GameBoard = board };
        state.SetThreadCount(2);

        try
        {
            var context = new AlphaBetaContext(state, board)
            {
                SplitNesting = splitNesting
            };
            var search = new AlphaBeta(state, () => { });

            search.Search(context, -10000, 10000, depth);

            Assert.Equal(0UL, context.Metrics.SplitPointsCreated);
            Assert.Equal(0UL, context.Metrics.WorkItemsScheduled);
        }
        finally
        {
            state.SearchScheduler.Dispose();
        }
    }
}
