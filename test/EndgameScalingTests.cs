using Xunit;
namespace mmchess.Test;

// Drawish-ending recognizers: pure opposite-colored-bishop endings scale
// toward a draw, and the wrong-rook-pawn fortress (bishop doesn't control
// the promotion corner, defending king sits in it) is a hard draw.
public class EndgameScalingTests
{
    const int Min = -10000;
    const int Max = 10000;

    [Fact]
    public void OppositeColoredBishops_ScalesAdvantageDown()
    {
        // Identical positions except the black bishop's square: c8 (light,
        // opposite to White's dark c1 bishop) vs b8 (dark, same color).
        // Both bishops are otherwise eval-neutral here because the position
        // is EndGame phase, where EvaluatePieces/Development don't run.
        var ocb = Board.ParseFenString("2b1k3/8/8/3PP3/8/8/8/2B1K3 w - - 0 1");
        var sameColor = Board.ParseFenString("1b2k3/8/8/3PP3/8/8/8/2B1K3 w - - 0 1");

        var ocbEval = Evaluator.Evaluate(ocb, Min, Max);
        var sameEval = Evaluator.Evaluate(sameColor, Min, Max);

        Assert.True(ocbEval < sameEval,
            $"expected OCB ending ({ocbEval}) to score below same-color bishops ({sameEval})");
        Assert.True(ocbEval > 0, $"scaled OCB advantage should still be positive, got {ocbEval}");
    }

    [Theory]
    [InlineData("k7/8/K7/P7/8/8/8/2B5 w - - 0 1")] // attacker to move
    [InlineData("k7/8/K7/P7/8/8/8/2B5 b - - 0 1")] // defender to move
    public void WrongRookPawn_DefenderInCorner_IsDraw(string fen)
    {
        // White: dark-squared bishop + a-pawn; promotion corner a8 is light
        // and the black king owns it. Dead draw despite +400 material.
        var b = Board.ParseFenString(fen);
        Assert.Equal(0, Evaluator.Evaluate(b, Min, Max));
    }

    [Fact]
    public void RightRookPawn_BishopControlsCorner_StillWinning()
    {
        // Same fortress attempt but the bishop (d3) is light-squared and
        // does control a8 - White is simply winning.
        var b = Board.ParseFenString("k7/8/K7/P7/8/3B4/8/8 w - - 0 1");
        var eval = Evaluator.Evaluate(b, Min, Max);
        Assert.True(eval > 0, $"right-bishop rook pawn should still win, got {eval}");
    }

    [Fact]
    public void WrongRookPawn_DefenderKingFarAway_NotADraw()
    {
        // Wrong bishop, but the defending king (h8) can't reach a8.
        var b = Board.ParseFenString("7k/8/K7/P7/8/8/8/2B5 w - - 0 1");
        var eval = Evaluator.Evaluate(b, Min, Max);
        Assert.True(eval > 0, $"defender can't reach the corner; expected > 0, got {eval}");
    }
}
