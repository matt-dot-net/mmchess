using Xunit;
namespace mmchess;

public class PawnHashTableTests
{
    [Fact]
    public void StoreThenProbeReturnsTheStoredScore()
    {
        var table = PawnHashTable.Instance;
        var key = 0x1234_5678_9abc_def0UL;
        var score = new PawnScore { Eval = 42 };

        table.Store(key, score);

        Assert.Same(score, table.Probe(key));
    }

    [Fact]
    public void ProbeReturnsNullOnKeyMismatch()
    {
        var table = PawnHashTable.Instance;
        var key = 0x0f0e_0d0c_0b0a_0908UL;
        table.Store(key, new PawnScore { Eval = 7 });

        // a different key that happens to land in the same slot (same low
        // bits, different high bits) must not be served the wrong entry
        var collidingKey = key ^ 0x8000_0000_0000_0000UL;

        Assert.Null(table.Probe(collidingKey));
    }

    [Fact]
    public void BlockedPawnStatusIsNotStaleAcrossIdenticalPawnHashKeys()
    {
        // Both positions have the exact same pawn placement for both sides
        // (white pawns e2/h2, black pawn a7) - only a black knight's square
        // differs, sitting on h5 (not blocking anything) in A vs e3 (directly
        // ahead of white's e2 pawn) in B. Same pawn placement means the same
        // Board.PawnHashKey, so this is exactly the scenario the pawn hash
        // table's design has to get right: caching the structural (doubled/
        // passed pawn) score by pawn-only key must not also serve up a stale
        // blocked-pawn verdict computed under different piece placement.
        // Both positions are engineered to land in Evaluator's EndGame phase
        // branch (CastleStatus=0, white has no majors/minors), which skips
        // every other piece-square-table-dependent eval term, so any
        // difference in Evaluate()'s result has to come from the pawn eval.
        var boardA = Board.ParseFenString("4k3/p7/8/7n/8/8/4P2P/4K3 w - - 0 1");
        var boardB = Board.ParseFenString("4k3/p7/8/8/8/4n3/4P2P/4K3 w - - 0 1");

        Assert.Equal(boardA.PawnHashKey, boardB.PawnHashKey);

        var evalA = Evaluator.Evaluate(boardA, -10000, 10000);
        // evaluating A first populates the pawn hash cache for this shared key
        var evalB = Evaluator.Evaluate(boardB, -10000, 10000);

        // B's e2 pawn is blocked (knight on e3), A's is not (knight on h5) -
        // that's a difference the pawn-hash cache alone can't see, since the
        // key is identical between the two positions
        Assert.Equal(8, evalA - evalB);
    }
}
