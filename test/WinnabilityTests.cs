using Xunit;
namespace mmchess.Test;

// Winnability semantics: a side that cannot possibly checkmate (bare king,
// lone minor, or two knights with no pawns/majors) should have its score
// CAPPED at a draw - not force the whole evaluation to 0. Conversely a side
// whose opponent cannot checkmate can never lose, so its score has a floor
// of 0. Both caps together (neither side can win) yield exactly 0.
public class WinnabilityTests
{
    const int Min = -10000;
    const int Max = 10000;

    [Fact]
    public void KRvK_LosingSideToMove_EvaluatesNegative()
    {
        // Black has a bare king (cannot win) but White very much can;
        // Black to move must NOT see a draw score here.
        var b = Board.ParseFenString("8/8/8/4k3/8/8/8/R3K3 b - - 0 1");
        var eval = Evaluator.Evaluate(b, Min, Max);
        Assert.True(eval < 0, $"expected losing bare-king side to score negative, got {eval}");
    }

    [Fact]
    public void KRvK_WinningSideToMove_EvaluatesPositive()
    {
        var b = Board.ParseFenString("8/8/8/4k3/8/8/8/R3K3 w - - 0 1");
        var eval = Evaluator.Evaluate(b, Min, Max);
        Assert.True(eval > 0, $"expected winning side to score positive, got {eval}");
    }

    [Fact]
    public void KNvKPawns_KnightSideToMove_SeesTheDanger()
    {
        // White: K+N (cannot win). Black: K + three connected passed pawns.
        // White's best case is a draw, and positionally it is worse than
        // that - the score must be negative, not a flat 0.
        var b = Board.ParseFenString("8/8/8/4k3/8/ppp5/8/4K2N w - - 0 1");
        var eval = Evaluator.Evaluate(b, Min, Max);
        Assert.True(eval < 0, $"expected can't-win side in trouble to score negative, got {eval}");
    }

    [Fact]
    public void KNvKP_KnightSideToMove_NeverEvaluatesAboveDraw()
    {
        // Up 200 in material, but a lone knight cannot mate: cap at 0.
        var b = Board.ParseFenString("8/8/8/4k3/8/2p5/8/4K2N w - - 0 1");
        var eval = Evaluator.Evaluate(b, Min, Max);
        Assert.True(eval <= 0, $"expected can't-win side capped at 0, got {eval}");
    }

    [Fact]
    public void KNvKP_PawnSideToMove_NeverEvaluatesBelowDraw()
    {
        // Black is down 200 material but cannot lose (White can't mate):
        // floor at 0.
        var b = Board.ParseFenString("8/8/8/4k3/8/2p5/8/4K2N b - - 0 1");
        var eval = Evaluator.Evaluate(b, Min, Max);
        Assert.True(eval >= 0, $"expected side facing can't-win opponent floored at 0, got {eval}");
    }

    [Theory]
    [InlineData("8/8/8/4k3/8/8/8/1NN1K3 w - - 0 1")] // KNN v K, winner to move
    [InlineData("8/8/8/4k3/8/8/8/1NN1K3 b - - 0 1")] // KNN v K, defender to move
    public void KNNvK_EvaluatesAsDraw(string fen)
    {
        // Two knights cannot force mate; neither side can win.
        var b = Board.ParseFenString(fen);
        Assert.Equal(0, Evaluator.Evaluate(b, Min, Max));
    }

    [Theory]
    [InlineData("8/8/8/4k3/8/8/8/1B2K3 w - - 0 1")]
    [InlineData("8/8/8/4k3/8/8/8/1B2K3 b - - 0 1")]
    public void KBvK_EvaluatesAsDraw(string fen)
    {
        var b = Board.ParseFenString(fen);
        Assert.Equal(0, Evaluator.Evaluate(b, Min, Max));
    }

    [Fact]
    public void LazyExit_StillRespectsWinnabilityCap()
    {
        // KNN v K with a narrow window: material (+600) would normally take
        // the lazy exit far above beta, but the capped score must still be
        // a draw. Guards against applying the cap only on the full-eval path.
        var b = Board.ParseFenString("8/8/8/4k3/8/8/8/1NN1K3 w - - 0 1");
        Assert.Equal(0, Evaluator.Evaluate(b, -50, 50));
    }
}
