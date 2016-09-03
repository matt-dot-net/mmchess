using Xunit;
namespace  mmchess.Test
{
    public class MakeMoveTests{
        [Fact]
        public void MakeMoveUpdatesAllPieces(){
            var testBoard = new Board();

            testBoard.MakeMove(new Move
             {
                 From=1,
                 To=18,
                 Bits=(byte)MoveBits.Knight
             });
            Assert.True((testBoard.AllPieces & BitMask.Mask[18]) > 0, "To Square is not set");     
            Assert.True((testBoard.AllPieces & BitMask.Mask[1]) == 0,"From square is set");
        }

        [Fact]
        public void WhitePawnMoveEnPassant(){
            var testBoard = new Board();

            testBoard.MakeMove(new Move{
                From = 48,
                To = 32,
                Bits = (byte)MoveBits.Pawn
            });

            Assert.Equal(BitMask.Mask[40],BitMask.Mask[40] & testBoard.EnPassant);
        }
    }
}