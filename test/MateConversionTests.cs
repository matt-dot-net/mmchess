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

    [Fact]
    public void PawnEnding_DoesNotApplyMateConversion()
    {
        // KP vs K: the winner has a pawn, so the win runs through promotion,
        // not cornering - the drive-to-corner / king-proximity term must NOT
        // fire here (it gives bad guidance and, on defense, steers the lone
        // king off its drawing squares). b6 and g6 have identical KingEndgame
        // PST values, so with the correct pawn-ending fallback these two
        // positions must evaluate EQUALLY. The buggy mate-conversion path
        // couples the two kings via its king-distance term (b6 is 3 from the
        // black king, g6 is 2) and would score them differently.
        var winnerKingB6 = Board.ParseFenString("4k3/8/1K6/8/4P3/8/8/8 w - - 0 1");
        var winnerKingG6 = Board.ParseFenString("4k3/8/6K1/8/4P3/8/8/8 w - - 0 1");

        Assert.Equal(Evaluator.Evaluate(winnerKingB6, Min, Max),
                     Evaluator.Evaluate(winnerKingG6, Min, Max));
    }

    [Fact]
    public void PawnlessPieceMate_StillCouplesKings()
    {
        // Guard the other side of the fix: with no pawns on the board the
        // conversion term must still fire, so the same b6/g6 winning-king
        // swap (KR vs K) DOES change the eval via king proximity.
        var winnerKingB6 = Board.ParseFenString("4k3/8/1K6/8/8/8/8/7R w - - 0 1");
        var winnerKingG6 = Board.ParseFenString("4k3/8/6K1/8/8/8/8/7R w - - 0 1");

        Assert.NotEqual(Evaluator.Evaluate(winnerKingB6, Min, Max),
                        Evaluator.Evaluate(winnerKingG6, Min, Max));
    }
}
