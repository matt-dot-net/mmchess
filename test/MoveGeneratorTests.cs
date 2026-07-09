using Xunit;
using System;

namespace mmchess;

public class MoveGeneratorTests{
    // [Fact]
    // public void WhiteKnightMovesFromStart(){
    //     var board = new Board();
    //     var list = new List<Move>();
    //     MoveGenerator.GenerateKnightMoves(board,list);

    //     Assert.Equal(4,list.Count);
    // } 
    // [Fact]
    // public void KingMovesFromStartIsZero(){
    //     var testBoard = new Board();
    //     var list = new List<Move>();
    //     MoveGenerator.GenerateKingMoves(testBoard,list);

    //     Assert.Equal(0, list.Count);
    // }       

    // [Fact]
    // public void PawnMovesFromStart(){
    //     var testBoard = new Board();
    //     var list = new List<Move>();
    //     MoveGenerator.GeneratePawnMoves(testBoard,list);

    //     Assert.Equal(16,list.Count);
    // }

    [Fact]
    public void PerfTLevel1(){
        var testBoard = new Board();

        Span<Move> buffer = stackalloc Move[MoveList.StackCapacity];
        var result = new MoveList(buffer);
        MoveGenerator.GenerateMoves(testBoard, ref result);
        Assert.Equal(20,result.Count);
    }

    [Fact]
    public void GeneratesBothCastlingSidesWhenBothRightsExist()
    {
        var board = Board.ParseFenString("r3k2r/8/8/8/8/8/8/R3K2R b kq - 0 1");

        Span<Move> buffer = stackalloc Move[MoveList.StackCapacity];
        var moves = new MoveList(buffer);
        MoveGenerator.GenerateMoves(board, ref moves);

        Assert.True(ContainsMove(moves, 4, 6));
        Assert.True(ContainsMove(moves, 4, 2));
    }

    [Fact]
    public void BlackQueensideCastleIsNotGeneratedThroughAttackedD8()
    {
        var board = Board.ParseFenString("r3k3/8/8/8/8/8/8/3R3K b q - 0 1");

        Span<Move> buffer = stackalloc Move[MoveList.StackCapacity];
        var moves = new MoveList(buffer);
        MoveGenerator.GenerateMoves(board, ref moves);

        Assert.False(ContainsMove(moves, 4, 2));
    }

    static bool ContainsMove(MoveList moves, int from, int to)
    {
        for (int i = 0; i < moves.Count; i++)
        {
            var move = moves[i];
            if (move.From == from && move.To == to)
                return true;
        }

        return false;
    }
}
