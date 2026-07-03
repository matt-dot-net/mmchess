using Xunit;
namespace mmchess.Test;

public class StaticExchangeTests
{
    [Fact]
    public void EnPassantCaptureCreditsThePawnValue()
    {
        // White just played e2-e4; black's pawn on d4 can capture en passant on e3,
        // winning a free, undefended pawn (White only has a king on e1, out of range).
        // The captured pawn sits on e4, not on the empty destination square e3, so
        // naively reading the piece value off m.To misses it entirely.
        var board = Board.ParseFenString("4k3/8/8/8/3pP3/8/8/4K3 b - e3 0 1");
        var move = new Move
        {
            From = 35, // d4
            To = 44,   // e3
            Bits = (byte)(MoveBits.Pawn | MoveBits.Capture)
        };

        var score = StaticExchange.Eval(board, move, board.SideToMove);

        Assert.Equal(100, score);
    }
}
