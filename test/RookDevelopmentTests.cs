using Xunit;
namespace mmchess.Test;

public class RookDevelopmentTests
{
    [Fact]
    public void RookStillOnHomeCornerIsPenalizedVersusOneMovedOffIt()
    {
        // Same material and pawns in both positions (white king e1, pawns
        // e2/h2; black king e8, pawn a7) - only the white rook's square
        // differs: a1 (never moved) vs a2 (moved, but deliberately kept on
        // the same file and off the 7th/8th rank) so EvaluatePieces' open-
        // file and 7th-rank rook bonuses come out identical either way,
        // isolating the difference to exactly the new "still on the home
        // corner" development penalty. "KQkq" forces Opening phase so
        // EvaluateDevelopment actually runs (it's skipped in EndGame). The
        // king sits in the middle (not the kingside/queenside bucket), so
        // EvaluateKingSafety's separate "rook in the corner" check - which
        // only fires for a king already tucked into that side - doesn't
        // apply here and can't double up with this penalty.
        var boardHomeCorner = Board.ParseFenString("4k3/p7/8/8/8/8/4P2P/R3K3 w KQkq - 0 1");
        var boardDeveloped = Board.ParseFenString("4k3/p7/8/8/8/8/R3P2P/4K3 w KQkq - 0 1");

        var evalHomeCorner = Evaluator.Evaluate(boardHomeCorner, -10000, 10000);
        var evalDeveloped = Evaluator.Evaluate(boardDeveloped, -10000, 10000);

        Assert.Equal(-15, evalHomeCorner - evalDeveloped);
    }
}
