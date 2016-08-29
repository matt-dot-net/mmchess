using Xunit;
using System.Collections.Generic;

namespace mmchess{

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
    }
}