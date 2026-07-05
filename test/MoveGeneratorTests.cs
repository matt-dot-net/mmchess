using Xunit;
using System.Collections.Generic;

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
        
        var result = MoveGenerator.GenerateMoves(testBoard);
        Assert.Equal(20,result.Count);
    }

    [Fact]
    public void GeneratesBothCastlingSidesWhenBothRightsExist()
    {
        var board = Board.ParseFenString("r3k2r/8/8/8/8/8/8/R3K2R b kq - 0 1");

        var moves = MoveGenerator.GenerateMoves(board);

        Assert.Contains(moves, m => m.From == 4 && m.To == 6);
        Assert.Contains(moves, m => m.From == 4 && m.To == 2);
    }

    [Fact]
    public void BlackQueensideCastleIsNotGeneratedThroughAttackedD8()
    {
        var board = Board.ParseFenString("r3k3/8/8/8/8/8/8/3R3K b q - 0 1");

        var moves = MoveGenerator.GenerateMoves(board);

        Assert.DoesNotContain(moves, m => m.From == 4 && m.To == 2);
    }
}
