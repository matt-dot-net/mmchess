using Xunit;
namespace mmchess.Test;

public class FixedDepthTests
{
    [Fact]
    public void FixedDepthTimeControlStopsAtRequestedDepth()
    {
        // The "sd" command (CommandProcessor.cs) sets TimeControlType.FixedDepth
        // and GameState.DepthLimit. GetThinkTimeSpan returns TimeSpan.MaxValue
        // for this mode, so DepthLimit itself must be what stops the
        // iterative-deepening loop - without it, nothing would halt the
        // search before MAX_DEPTH (64) or a forced mate, which on the
        // opening position is effectively unbounded.
        var state = new GameState
        {
            GameBoard = new Board(),
            TimeControl = new TimeControl { Type = TimeControlType.FixedDepth },
            DepthLimit = 3
        };

        Iterate.DoIterate(state, () => { }, out var metrics);

        Assert.Equal(3, metrics.Depth);
    }

    [Fact]
    public void ParallelFixedDepthSearchStopsAndUsesRootScouts()
    {
        var state = new GameState
        {
            GameBoard = new Board(),
            TimeControl = new TimeControl { Type = TimeControlType.FixedDepth },
            DepthLimit = 3
        };
        state.SetThreadCount(3);

        try
        {
            var move = Iterate.DoIterate(state, () => { }, out var metrics);

            Assert.False(move.IsNull);
            Assert.Equal(3, metrics.Depth);
            Assert.True(metrics.WorkItemsScheduled > 0);
            Assert.Equal(metrics.WorkItemsScheduled, metrics.WorkItemsCompleted);
        }
        finally
        {
            state.SearchScheduler.Dispose();
        }
    }
}
