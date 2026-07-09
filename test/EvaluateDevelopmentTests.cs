using Xunit;
namespace mmchess.Test;

public class EvaluateDevelopmentTests
{
    [Fact]
    public void BlockedCentralPawnIsPenalizedMoreThanAnOrdinaryBlockedPawn()
    {
        // Same material and pawn placement in both positions (white king e1,
        // pawns e2/h2; black king e8, pawn a7, one rook) - only the rook's
        // square differs: h5 (blocks nothing) in A vs e3 (directly ahead of
        // white's e2 pawn) in B. A rook is used as the "blocker" specifically
        // because EvaluateDevelopment doesn't score rooks at all (unlike
        // knights/bishops, which have their own per-square development
        // tables), and both squares land on an already-half-open file from
        // black's perspective either way - so EvaluatePieces' rook bonuses
        // come out identical too. "KQkq" forces Opening phase (CastleStatus >
        // 0) so EvaluateDevelopment actually runs in both cases (it's
        // skipped entirely in EndGame). That isolates the difference to
        // exactly: the existing generic BlockedPawnPenalty (-8, fires in
        // both EvaluateBlockedPawns regardless of file) plus the new
        // central-file-specific development penalty this test is for.
        var boardA = Board.ParseFenString("4k3/p7/8/7r/8/8/4P2P/4K3 w KQkq - 0 1");
        var boardB = Board.ParseFenString("4k3/p7/8/8/8/4r3/4P2P/4K3 w KQkq - 0 1");

        var evalA = Evaluator.Evaluate(boardA, -10000, 10000);
        var evalB = Evaluator.Evaluate(boardB, -10000, 10000);

        // 8 from the existing generic blocked-pawn penalty (fires in B
        // either way, central or not) + 20 from the central-pawn-specific
        // development penalty + 6 because the rook on e3 attacks into the
        // white king zone, discounted because the queens are off.
        Assert.Equal(34, evalA - evalB);
    }
}
