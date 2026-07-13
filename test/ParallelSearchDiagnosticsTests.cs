using System;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace mmchess;

public class ParallelSearchDiagnosticsTests
{
    const string BenchmarkFen =
        "8/7p/5k2/5p2/p1p2P2/Pr1pPK2/1P1R3P/8 b - - 0 1";
    const int BenchmarkDepth = 16;
    readonly ITestOutputHelper output;

    public ParallelSearchDiagnosticsTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public void BenchmarkPositionCompletesAcrossWorkerCounts()
    {
        _ = SearchToDepth(threadCount: 1, depth: 6);
        var sequential = SearchToDepth(threadCount: 1, depth: BenchmarkDepth);
        var twoThreads = SearchToDepth(threadCount: 2, depth: BenchmarkDepth);
        var fourThreads = SearchToDepth(threadCount: 4, depth: BenchmarkDepth);

        output.WriteLine($"sequential={sequential}");
        output.WriteLine($"two={twoThreads}");
        output.WriteLine($"four={fourThreads}");

        AssertLegalRootMove(sequential.Move);
        AssertLegalRootMove(twoThreads.Move);
        AssertLegalRootMove(fourThreads.Move);
        Assert.Equal(0UL, sequential.WorkItems);
        Assert.True(twoThreads.WorkItems > 0);
        Assert.True(fourThreads.WorkItems > 0);
    }

    static void AssertLegalRootMove(Move move)
    {
        var board = Board.ParseFenString(BenchmarkFen);
        Assert.False(move.IsNull);
        Assert.True(board.MakeMove(move));
    }

    static SearchResult SearchToDepth(int threadCount, int depth)
    {
        TranspositionTable.Clear();
        PawnHashTable.Instance.Clear();
        var board = Board.ParseFenString(BenchmarkFen);
        var state = new GameState { GameBoard = board };
        state.SetThreadCount(threadCount);

        try
        {
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var cpuBefore = Process.GetCurrentProcess().TotalProcessorTime;
            var stopwatch = Stopwatch.StartNew();
            var context = new AlphaBetaContext(state, board);
            var search = new AlphaBeta(state, () => { });
            var alpha = -10000;
            var beta = 10000;
            var score = 0;

            for (var currentDepth = 0; currentDepth <= depth; currentDepth++)
            {
                var alphaRelax = 1;
                var betaRelax = 1;
                if (currentDepth > 0)
                {
                    beta = alpha + Iterate.ASPIRATION_WINDOW;
                    alpha -= Iterate.ASPIRATION_WINDOW;
                }

                do
                {
                    score = search.SearchRoot(context, alpha, beta, currentDepth);
                    if (score > alpha && score < beta)
                    {
                        alpha = score;
                        break;
                    }
                    if (score >= beta)
                    {
                        if (score == 10000)
                            break;
                        beta = Iterate.WidenBeta(beta, betaRelax, score);
                        betaRelax *= 4;
                    }
                    else
                    {
                        if (score == -10000)
                            break;
                        alpha = Iterate.WidenAlpha(alpha, alphaRelax, score);
                        alphaRelax *= 4;
                    }
                } while (!state.TimeUp);
            }

            stopwatch.Stop();
            return new SearchResult(
                score,
                context.PrincipalVariation[0, 0],
                context.Metrics.Nodes,
                context.Metrics.SplitPointsCreated,
                context.Metrics.WorkItemsScheduled,
                stopwatch.ElapsedMilliseconds,
                (Process.GetCurrentProcess().TotalProcessorTime - cpuBefore).TotalMilliseconds,
                GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore);
        }
        finally
        {
            state.SearchScheduler.Dispose();
        }
    }

    readonly record struct SearchResult(
        int Score,
        Move Move,
        ulong Nodes,
        ulong SplitPoints,
        ulong WorkItems,
        long ElapsedMilliseconds,
        double CpuMilliseconds,
        long AllocatedBytes)
    {
        public override string ToString()
        {
            return $"{Score}/{Move.ToCoordinateString()} nodes={Nodes} splits={SplitPoints} " +
                $"work={WorkItems} time={ElapsedMilliseconds}ms cpu={CpuMilliseconds:0}ms " +
                $"allocated={AllocatedBytes / 1024}KiB";
        }
    }
}
