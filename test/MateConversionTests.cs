using Xunit;
namespace mmchess.Test;

// Mate-conversion eval: when one side has mating material and the other a
// bare king, the winner should be rewarded for bringing its king toward
// the defender (the static king PST can't see king proximity) and, in the
// KBN ending, for driving the defender to a corner of the bishop's color.
public class MateConversionTests
{
    const int Min = -10000;
    const int Max = 10000;

    [Fact]
    public void KRK_WinningKingCloserToDefender_ScoresHigher()
    {
        // Identical except the white king: g6 (2 king-moves from h8) vs b6
        // (6 away). Both squares have the same value in the old KingEndgame
        // PST, so only a real proximity term can separate these.
        var close = Board.ParseFenString("7k/8/6K1/8/8/8/8/R7 w - - 0 1");
        var far = Board.ParseFenString("7k/8/1K6/8/8/8/8/R7 w - - 0 1");

        var closeEval = Evaluator.Evaluate(close, Min, Max);
        var farEval = Evaluator.Evaluate(far, Min, Max);

        Assert.True(closeEval > farEval,
            $"expected king proximity to score higher: close={closeEval}, far={farEval}");
    }

    [Fact]
    public void KBNK_DefenderInBishopColorCorner_ScoresHigher()
    {
        // Dark-squared bishop: mate only happens in a dark corner (a1/h8).
        // Defender on a1 (right corner) must score higher for White than
        // defender on a8 (safe light corner).
        var rightCorner = Board.ParseFenString("8/8/8/8/4K3/8/8/k1B3N1 w - - 0 1");
        var wrongCorner = Board.ParseFenString("k7/8/8/8/4K3/8/8/2B3N1 w - - 0 1");

        var rightEval = Evaluator.Evaluate(rightCorner, Min, Max);
        var wrongEval = Evaluator.Evaluate(wrongCorner, Min, Max);

        Assert.True(rightEval > wrongEval,
            $"expected bishop-color corner to score higher: right={rightEval}, wrong={wrongEval}");
    }

    [Fact]
    public void KQK_DefenderOnEdge_ScoresHigherThanCenter()
    {
        // General drive-to-edge gradient, defender king d1 (edge) vs d5
        // (center), everything else fixed.
        var edge = Board.ParseFenString("8/8/8/8/8/8/1Q6/3k1K2 w - - 0 1");
        var center = Board.ParseFenString("8/8/8/3k4/8/8/1Q6/5K2 w - - 0 1");

        var edgeEval = Evaluator.Evaluate(edge, Min, Max);
        var centerEval = Evaluator.Evaluate(center, Min, Max);

        Assert.True(edgeEval > centerEval,
            $"expected edge defender to score higher: edge={edgeEval}, center={centerEval}");
    }
}
