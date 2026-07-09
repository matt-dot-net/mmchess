using Xunit;
using System;

namespace mmchess.Test;


public class BoardTests{
    [Fact]
    public void ResetWhiteKnights(){
        var testBoard = new Board();
        Assert.True((testBoard.Knights[0] & BitMask.Mask[57])>0, "White Knight missing from g1");
        Assert.True((testBoard.Knights[0] & BitMask.Mask[62])>0, "White knight missing from b1");
        var testMask = BitMask.Mask[57] | BitMask.Mask[62];

        Assert.True((testBoard.Pieces[0] & testMask)>0,"White Pieces missing white knight(s)");
        Assert.True((testBoard.AllPieces & testMask)>0,"AllPieces missing white knight(s)");
    }

    [Fact]
    public void ResetAllPieces(){
        var testBoard = new Board();

        var testMask = 0xFFFF00000000FFFF;
        Assert.Equal(testMask,testBoard.AllPieces);
    }

    [Fact]
    public void ResetAllPawns(){
        var testBoard = new Board();
        Assert.Equal((ulong)0xff00, testBoard.Pawns[1]);
        Assert.Equal((ulong)0xFF000000000000, testBoard.Pawns[0]);

    }

    [Fact]
    public void CapturingEnemyRookOnItsHomeSquareRevokesCastlingRights(){
        // White rook h1 captures black's rook on h8 (Rxh8). Black's rook never
        // moved itself, but it's gone now, so black's kingside castling right
        // must be revoked - otherwise the engine can later generate an O-O
        // move with no rook to actually castle with.
        var board = Board.ParseFenString("4k2r/8/8/8/8/8/8/4K2R w Kk - 0 1");
        var move = new Move { From = 63, To = 7, Bits = (byte)(MoveBits.Rook | MoveBits.Capture) }; // Rh1xh8

        board.MakeMove(move);

        Assert.Equal(0, board.CastleStatus & 4); // black kingside ('k') right must be cleared
    }

    [Fact]
    public void RealGameWhereQueenCapturesRookOnH8ExcludesLaterOO(){
        // Full replay of a real game (mmchess vs Dorky 4.8, 2026-07-04) where
        // white's queen captures black's h8 rook with Qxh8+ at move 20. Black
        // later tried to play O-O and cutechess flagged it as illegal, since
        // the rook needed to castle with was long gone. This replays the
        // actual move sequence (not a hand-built FEN) to confirm move
        // generation no longer offers O-O (e8g8) once we reach that point.
        var board = new Board();
        string[] moves = {
            "d2d4","d7d5","c2c4","c7c6","b1c3","g8f6","e2e3","a7a6","f1d3","d5c4",
            "d3c4","b7b5","c4b3","e7e6","g1f3","f8e7","e1g1","c8b7","e3e4","c6c5",
            "e4e5","f6d5","c3d5","e6d5","d4c5","e7c5","c1g5","d8d7","a1c1","c5b6",
            "e5e6","f7e6","f3e5","d7d6","d1h5","g7g6","e5g6","h7g6","h5h8","d6f8",
            "h8h3","f8f2","f1f2","b6f2","g1f2"
        };
        foreach (var moveStr in moves)
        {
            var m = Move.ParseMove(board, moveStr);
            Assert.True(!m.IsNull && board.MakeMove(m), $"move {moveStr} should have been legal");
        }

        Span<Move> buffer = stackalloc Move[MoveList.StackCapacity];
        var legalMoves = new MoveList(buffer);
        MoveGenerator.GenerateMoves(board, ref legalMoves);
        var hasCastle = false;
        for (int i = 0; i < legalMoves.Count; i++)
        {
            var m = legalMoves[i];
            if (m.From == 4 && m.To == 6) // e8g8 = black O-O
                hasCastle = true;
        }

        Assert.False(hasCastle, "O-O should not be offered - the h8 rook was captured");
    }
}
