using Xunit;
namespace mmchess.Test;

public class SingleLegalMoveTests
{
    [Fact]
    public void SingleLegalMoveIsPlayedWithoutSearching()
    {
        // White Ka1 is in check from the rook on h1 (rank 1). b1 is covered by
        // that same rook and b2 by the rook on b8 (b-file), so Ka2 is the only
        // legal move. A forced move needs no search - DoIterate should return
        // it immediately without spending any nodes (or the clock).
        var state = new GameState
        {
            GameBoard = Board.ParseFenString("1r2k3/8/8/8/8/8/8/K6r w - - 0 1"),
            TimeControl = new TimeControl { Type = TimeControlType.FixedDepth },
            DepthLimit = 6
        };

        var move = Iterate.DoIterate(state, () => { }, out var metrics);

        Assert.NotNull(move);
        Assert.Equal(56, (int)move.From); // a1
        Assert.Equal(48, (int)move.To);   // a2
        Assert.Equal(0ul, metrics.Nodes); // no search performed
    }

    [Fact]
    public void MultipleLegalMovesStillSearch()
    {
        // Sanity guard on the short-circuit: the opening position has 20 legal
        // moves, so the search must still run to the requested depth.
        var state = new GameState
        {
            GameBoard = new Board(),
            TimeControl = new TimeControl { Type = TimeControlType.FixedDepth },
            DepthLimit = 3
        };

        var move = Iterate.DoIterate(state, () => { }, out var metrics);

        Assert.NotNull(move);
        Assert.Equal(3, metrics.Depth);
    }
}
