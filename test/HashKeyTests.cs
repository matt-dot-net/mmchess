using Xunit;
namespace mmchess
{
    public class HashKeyTests
    {
   
        [Fact]
        public void GetHashKeyAgreesWithMakeMove1(){
            var testBoard = new Board();

            var moves = MoveGenerator.GenerateMoves(testBoard);

            testBoard.MakeMove(moves[0]);
            var result = testBoard.HashKey;
            var expected = TranspositionTable.GetHashKey(testBoard);

            Assert.Equal(expected,result); 
        }
    }
}