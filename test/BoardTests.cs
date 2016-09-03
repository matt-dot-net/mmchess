using Xunit;

namespace mmchess.Test
{

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
    }
}