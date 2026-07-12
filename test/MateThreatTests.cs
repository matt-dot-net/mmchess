using Xunit;
namespace mmchess.Test;

public class MateThreatTests
{
    [Fact]
    public void NullMoveDetectsMateThreatBehindItsFreeTempo()
    {
        // White king g1 is boxed by its own pawns f2/g2/h2 (no luft, classic
        // back-rank weakness). Black's rook starts on a8, off the e-file, so
        // there's no immediate mate on the board - but once Black lines the
        // rook up on the open e-file (e.g. ...Rae8), White is in zugzwang:
        // any move that doesn't create luft allows ...Re1# next. That's a
        // textbook "mate threat behind a null move" - the null-move search
        // at White's node (skip a turn) lets Black immediately deliver the
        // back-rank mate, which should be flagged via Metrics.MateThreats
        // rather than silently falling through the (currently dead)
        // mateThreat local in AlphaBeta.Search.
        // White also carries 3 spare minor pieces (a1/c1/h1, well clear of
        // the back-rank mate pattern) purely so PieceCount > 2 and null-move
        // pruning isn't skipped for zugzwang-safety in a near-bare-material
        // position - that safeguard would otherwise mask the very thing this
        // test is checking.
        var board = Board.ParseFenString("r5k1/5ppp/8/8/8/8/5PPP/N1B3KN b - - 0 1");
        var state = new GameState { GameBoard = board };
        var ab = new AlphaBeta(state, () => { });
        AlphaBetaContext context = new AlphaBetaContext(state, board);

        ab.SearchRoot(context,-10000, 10000, 10);

        Assert.True(context.Metrics.MateThreats > 0,
            $"expected the null-move search to flag a mate threat, got {context.Metrics.MateThreats}");
    }
}
