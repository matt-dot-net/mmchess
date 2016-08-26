using Xunit;

namespace mmchess.Test
{

    public class BoardTests{
        [Fact]
        public void ResetWhiteKnights(){
            var testBoard = new Board();
            Assert.True((testBoard.WhiteKnights & BitMask.Mask[1])>0, "White Knight missing from g1");
            Assert.True((testBoard.WhiteKnights & BitMask.Mask[6])>0, "White knight missing from b1");
            var testMask = BitMask.Mask[1] | BitMask.Mask[6];

            Assert.True((testBoard.WhitePieces & testMask)>0,"White Pieces missing white knight(s)");
            Assert.True((testBoard.AllPieces & testMask)>0,"AllPieces missing white knight(s)");
        }

        [Fact]
        public void ResetAllPieces(){
            var testBoard = new Board();

            var testMask = 0xFF000000000000FF;
            Assert.Equal(testMask,testBoard.AllPieces);
        }
        
        
    }
}