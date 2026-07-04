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
}
