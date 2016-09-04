using Xunit;
namespace mmchess
{
    public class HashKeyTests
    {

        [Fact]
        public void MoveGeneratorDoesNotChangeHashKey(){
            var b = new Board();
            var expected = TranspositionTable.GetHashKeyForPosition(b);
            MoveGenerator.GenerateMoves(b);
            Assert.Equal(expected,b.HashKey);
        }
   
        [Fact]
        public void GetHashKeyAgreesWithMakeMove1(){
            var testBoard = new Board();

            var moves = MoveGenerator.GenerateMoves(testBoard);

            foreach(var m in moves)
            {
                var expected = testBoard.HashKey;
                if(!testBoard.MakeMove(m))
                    continue;
                testBoard.UnMakeMove();
                Assert.Equal(expected,testBoard.HashKey);
            }
            var result = testBoard.HashKey;
            
        }

        [Fact]
        public void HashKeyRestoredAfterUnMakeMove(){
            var testBoard = new Board();
            var moves = MoveGenerator.GenerateMoves(testBoard);

            foreach(var m in moves){
                var expected = testBoard.HashKey;
                if(!testBoard.MakeMove(m))
                    continue;
                testBoard.UnMakeMove();
                Assert.Equal(expected,testBoard.HashKey);
            }
        }
        
    }
}