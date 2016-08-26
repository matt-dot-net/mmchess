namespace mmchess
{
    public enum MoveBits{
            BlackPiece=1,
            Capture=2,
            King=4,
            Pawn=8,
            Knight=16,
            Bishop=32,
            Rook=64,
            Queen=128
        }
    public class Move
    {
        public byte From{get;set;}
        public byte To {get;set;}
        public byte Bits{get;set;}
        public byte Promotion{get;set;}
    }
}