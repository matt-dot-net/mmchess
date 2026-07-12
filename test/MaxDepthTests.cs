using Xunit;

namespace mmchess.Test;

public class MaxDepthTests
{
    [Fact]
    public void SearchAtMaxDepthDoesNotUnmakeCallerMove()
    {
        var board = Board.ParseFenString("4k3/8/8/8/8/8/4P3/4K3 w - - 0 1");
        var move = new Move { From = 52, To = 44, Bits = (byte)MoveBits.Pawn };
        Assert.True(board.MakeMove(move));
        var hashAfterCallerMove = board.HashKey;
        var historyCountAfterCallerMove = board.History.Count;
        var state = new GameState { GameBoard = board };
        var ab = new AlphaBeta(state, () => { });
        
        var context = new AlphaBetaContext(state, board)
        {
            Ply = AlphaBeta.MAX_DEPTH
        };
        ab.Search(context, -10000, 10000, 1);

        Assert.Equal(hashAfterCallerMove, board.HashKey);
        Assert.Equal(historyCountAfterCallerMove, board.History.Count);
        Assert.Equal(1, board.SideToMove);
    }

    [Fact]
    public void QuiesceAtMaxDepthDoesNotUnmakeCallerMove()
    {
        var board = Board.ParseFenString("4k3/8/8/8/8/8/4P3/4K3 w - - 0 1");
        var move = new Move { From = 52, To = 44, Bits = (byte)MoveBits.Pawn };
        Assert.True(board.MakeMove(move));
        var hashAfterCallerMove = board.HashKey;
        var historyCountAfterCallerMove = board.History.Count;
        var state = new GameState { GameBoard = board };
        var ab = new AlphaBeta(state, () => { });
        
        var context = new AlphaBetaContext(state, board)
        {
            Ply = AlphaBeta.MAX_DEPTH
        };
        AlphaBeta.Quiesce(context, -10000, 10000);

        Assert.Equal(hashAfterCallerMove, board.HashKey);
        Assert.Equal(historyCountAfterCallerMove, board.History.Count);
        Assert.Equal(1, board.SideToMove);
    }
}
