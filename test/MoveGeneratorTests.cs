using Xunit;
namespace mmchess{

    public class MoveGeneratorTests{
        [Fact]
        public void WhiteKnightMovesFromStart(){
            var board = new Board();
            var results = MoveGenerator.GenerateKnightMoves(board,0);

            Assert.Equal(4,results.Count);
        } 
        [Fact]
        public void KingMovesFromStartIsZero(){
            var testBoard = new Board();
            var results = MoveGenerator.GenerateKingMoves(testBoard,0);

            Assert.Equal(0, results.Count);
        }       
    }
}