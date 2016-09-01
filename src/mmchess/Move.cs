using System;

namespace mmchess
{
    public enum MoveBits
    {

        Capture = 1,
        King = 2,
        Pawn = 4,
        Knight = 8,
        Bishop = 16,
        Rook = 32,
        Queen = 64
    }

    public enum Piece
    {
        Knight = 0,
        Bishop = 1,
        Rook = 2,
        Queen = 3
    }
    public class Move
    {
        public byte From { get; set; }
        public byte To { get; set; }
        public byte Bits { get; set; }
        public byte Promotion { get; set; }

        public Move() { }

        public Move(Move m)
        {
            From = m.From;
            To = m.To;
            Bits = m.Bits;
            //Promotion=m.Promotion;
        }

        public override string ToString()
        {
            return Board.SquareNames[From] + Board.SquareNames[To];
        }

        public static Move ParseMove(Board b, string moveString){
            var from = moveString.Substring(0,2);
            var to = moveString.Substring(2,2);
            if(moveString.Length != 4)
                return null;
            int fromIndex =-1, toIndex=-1;
            for(int i=0;i<64;i++)
            {   
                if(Board.SquareNames[i]==from)
                    fromIndex=i;
                if(Board.SquareNames[i]==to)
                    toIndex=i;
            }

            if(fromIndex==-1 || toIndex==-1)
                return null;
            
            MoveBits bits=0;
            if((b.Knights[b.SideToMove] & BitMask.Mask[fromIndex])>0)
                bits |= MoveBits.Knight;
            else if ((b.Bishops[b.SideToMove] & BitMask.Mask[fromIndex])>0)
                bits |= MoveBits.Bishop;
            else if ((b.Rooks[b.SideToMove] & BitMask.Mask[fromIndex])>0)
                bits |= MoveBits.Rook;
            else if ((b.Queens[b.SideToMove] & BitMask.Mask[fromIndex])>0)
                bits |= MoveBits.Queen;
            else if ((b.Pawns[b.SideToMove] & BitMask.Mask[fromIndex])>0)
                bits |= MoveBits.Pawn;
            else if ((b.King[b.SideToMove] & BitMask.Mask[fromIndex])>0)
                bits |= MoveBits.King;
            else 
                return null;

            if((b.AllPieces & BitMask.Mask[toIndex]) > 0){
                //let's make sure we are capturing the right Piece
                if((b.Pieces[b.SideToMove] & BitMask.Mask[toIndex])>0)
                    return null;

                bits |= MoveBits.Capture;
            }

            return new Move{
                From=(byte)fromIndex,
                To=(byte)toIndex,
                Bits = (byte)bits
            };
        }
    }


}