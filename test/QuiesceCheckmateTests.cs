using Xunit;
namespace mmchess.Test;

public class QuiesceCheckmateTests
{
    [Fact]
    public void QuiesceDetectsCheckmateWithPlyAdjustedScore()
    {
        // White (to move) is checkmated: king h3, black queen f2, bishop g5,
        // knight g1 (delivering check), pawn h5. No legal white moves at all.
        // Without explicit mate detection, Quiesce falls through to
        // "return alpha", silently returning whatever bound was passed in
        // instead of a real, ply-adjusted mate score - this is the bug that
        // caused the engine to emit no move at all in a related position
        // (see AlphaBeta.cs SearchRoot for the same pattern already handled
        // correctly there).
        var board = Board.ParseFenString("7k/8/8/6bp/8/7K/5q2/6n1 w - - 0 77");
        var state = new GameState { GameBoard = board };
        var ab = new AlphaBeta(state, () => { });

        // deliberately use a narrow, non-mate-value window - the broken
        // fallback ("return alpha") would return -500 here, not a real mate
        // score, so this window actually discriminates between the two
        var result = ab.Quiesce(-500, 500);

        Assert.Equal(-10000, result); // mate detected at Ply=0 -> -10000+0
    }

    [Fact]
    public void SearchRootFindsOnlyLegalMoveEvenWhenItLeadsToForcedMate()
    {
        // The position one ply earlier: White to move, king h2, with exactly
        // one legal move (Kh3). Black can then force mate via underpromotion
        // to a knight. Before the fix, Quiesce's missing mate detection meant
        // this correct-but-losing move never registered as an improvement
        // over the initial alpha bound, so SearchRoot returned with no best
        // move at all (PrincipalVariation[0,0] stayed null).
        var board = Board.ParseFenString("7k/8/8/6bp/8/8/5qpK/8 w - - 2 76");
        var state = new GameState { GameBoard = board };
        var ab = new AlphaBeta(state, () => { });

        ab.SearchRoot(-10000, 10000, 0);

        Assert.NotNull(ab.PrincipalVariation[0, 0]);
        Assert.Equal(55, ab.PrincipalVariation[0, 0].From); // h2
        Assert.Equal(47, ab.PrincipalVariation[0, 0].To);   // h3
    }
}
