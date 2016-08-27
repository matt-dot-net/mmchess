namespace mmchess
{
    public enum MoveBits{
            Black=1,
            Capture=2,
            King=4,
            Pawn=8,
            Knight=16,
            Bishop=32,
            Rook=64,
            Queen=128
        }

    public enum Piece{
        Knight=0,
        Bishop=1,
        Rook=2,
        Queen=3
    }
    public class Move
    {
        public byte From{get;set;}
        public byte To {get;set;}
        public byte Bits{get;set;}
        public byte Promotion{get;set;}

        public Move(){}

        public Move(Move m){
            From = m.From;
            To = m.To;
            Bits = m.Bits;
            //Promotion=m.Promotion;
        }
    }
}