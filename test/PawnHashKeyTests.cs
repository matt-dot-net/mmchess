using Xunit;
namespace mmchess;

public class PawnHashKeyTests
{
    [Fact]
    public void PawnHashKeyMatchesFreshComputationAfterEachMove()
    {
        var testBoard = new Board();
        var moves = MoveGenerator.GenerateMoves(testBoard);

        foreach (var m in moves)
        {
            if (!testBoard.MakeMove(m))
                continue;

            var expected = TranspositionTable.GetPawnHashKeyForPosition(testBoard);
            Assert.Equal(expected, testBoard.PawnHashKey);

            testBoard.UnMakeMove();
        }
    }

    [Fact]
    public void PawnHashKeyRestoredAfterUnMakeMove()
    {
        var testBoard = new Board();
        var moves = MoveGenerator.GenerateMoves(testBoard);

        foreach (var m in moves)
        {
            var expected = testBoard.PawnHashKey;
            if (!testBoard.MakeMove(m))
                continue;
            testBoard.UnMakeMove();
            Assert.Equal(expected, testBoard.PawnHashKey);
        }
    }

    [Fact]
    public void PawnHashKeyTracksEnPassantCapture()
    {
        // White pawn e5, black just played ...d7-d5, so e5xd6 en passant is
        // legal - the captured pawn disappears from d5, not the destination
        // square d6, which is exactly the case UpdateCapture's en-passant
        // branch has to handle for PawnHashKey too (it returns early, before
        // the shared post-capture code the other capture types fall through to).
        var board = Board.ParseFenString("4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 1");
        var m = Move.ParseMove(board, "e5d6");

        Assert.True(board.MakeMove(m));
        Assert.Equal(TranspositionTable.GetPawnHashKeyForPosition(board), board.PawnHashKey);

        var beforeUnmake = board.PawnHashKey;
        board.UnMakeMove();
        Assert.Equal(TranspositionTable.GetPawnHashKeyForPosition(board), board.PawnHashKey);
        Assert.NotEqual(beforeUnmake, board.PawnHashKey);
    }

    [Fact]
    public void PawnHashKeyTracksPromotion()
    {
        // White pawn on a7 promotes on a8 (capturing black's rook there) -
        // the pawn has to leave the pawn hash entirely (it's not a pawn
        // anymore), while the promoted queen must not be added to it (the
        // pawn hash only ever tracks pawns).
        var board = Board.ParseFenString("r3k3/P7/8/8/8/8/8/4K3 w - - 0 1");
        var m = Move.ParseMove(board, "a7a8q");

        Assert.True(board.MakeMove(m));
        Assert.Equal(TranspositionTable.GetPawnHashKeyForPosition(board), board.PawnHashKey);
        Assert.Equal(0UL, board.PawnHashKey); // no pawns left on the board at all

        board.UnMakeMove();
        Assert.Equal(TranspositionTable.GetPawnHashKeyForPosition(board), board.PawnHashKey);
    }
}
