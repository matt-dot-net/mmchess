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
    }


}