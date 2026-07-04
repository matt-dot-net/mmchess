using Xunit;
namespace mmchess.Test;

public class SeePruningEvasionTests
{
    [Fact]
    public void OnlyLegalEvasionIsALosingCaptureBySee()
    {
        // White king g1 is boxed in by its own pawns (f2/g2/h2) and in check
        // from the rook on a1 along the back rank. The only legal response is
        // Qxa1 - the queen is the only piece that can reach a1 at all (no
        // other piece can interpose on b1-f1) - but it's a bad trade: the
        // bishop on b2 recaptures the queen. Confirmed via python-chess: in
        // check, not checkmate, exactly one legal move (Qxa1).
        var board = Board.ParseFenString("Q6k/8/8/8/8/8/1b3PPP/r5K1 w - - 0 1");
        var move = new Move { From = 0, To = 56, Bits = (byte)(MoveBits.Queen | MoveBits.Capture) }; // Qa8xa1

        var seeValue = StaticExchange.Eval(board, move, board.SideToMove);

        Assert.True(seeValue < 0, $"expected a losing trade, got SEE={seeValue}");
    }

    [Fact]
    public void QuiesceDoesNotDeclareCheckmateWhenOnlyEvasionIsSeePruned()
    {
        // Same position: this is NOT checkmate (Qxa1 is legal, just costly).
        // If SEE-pruning discards it as a "losing capture" the same way it
        // would for a purely optional capture elsewhere in the search, Quiesce
        // would end up with zero moves tried while in check, and (per the
        // checkmate-detection fix) incorrectly report a forced mate score
        // instead of the real, merely-bad-but-legal outcome.
        var board = Board.ParseFenString("Q6k/8/8/8/8/8/1b3PPP/r5K1 w - - 0 1");
        var state = new GameState { GameBoard = board };
        var ab = new AlphaBeta(state, () => { });

        var result = ab.Quiesce(-10000, 10000);

        Assert.NotEqual(-10000, result); // must not be reported as checkmate
    }
}
