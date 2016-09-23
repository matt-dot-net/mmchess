using System;
using System.Runtime.InteropServices;

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
        Empty = 0,
        Knight = 1,
        Bishop = 2,
        Rook = 3,
        Queen = 4,
        Pawn = 5,
        King = 6
    }
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public class Move
    {

        public static Piece GetPiece(MoveBits bits)
        {
            if(bits == 0)
                return Piece.Empty;

            if ((bits & MoveBits.Bishop) > 0)
                return Piece.Bishop;
            else if ((bits & MoveBits.King) > 0)
                return Piece.King;
            else if ((bits & MoveBits.Knight) > 0)
                return Piece.Knight;
            else if ((bits & MoveBits.Pawn) > 0)
                return Piece.Pawn;
            else if ((bits & MoveBits.Queen) > 0)
                return Piece.Queen;
            else if ((bits & MoveBits.Rook) > 0)
                return Piece.Rook;
            else
                throw new Exception(String.Format("Unhandled bits {0}", (int)bits));

        }
        public static Piece GetPieceFromMoveBits(MoveBits bits)
        {
            if ((bits & MoveBits.Bishop) > 0)
                return Piece.Bishop;
            if ((bits & MoveBits.Knight) > 0)
                return Piece.Knight;
            if ((MoveBits.Queen & bits) > 0)
                return Piece.Queen;
            if ((MoveBits.Rook & bits) > 0)
                return Piece.Rook;
            if ((MoveBits.King & bits) > 0)
                return Piece.King;
            if ((MoveBits.Pawn & bits) > 0)
                return Piece.Pawn;

            return Piece.Empty;
        }

        static readonly char[] outputRanks = new char[] { '8', '7', '6', '5', '4', '3', '2', '1' };
        static readonly char[] outputFiles = new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' };
        [FieldOffset(0)]
        public byte From;
        [FieldOffset(1)]
        public byte To;
        [FieldOffset(2)]
        public byte Bits;
        [FieldOffset(3)]
        public byte Promotion;
        [FieldOffset(0)]
        public UInt32 Value;

        public Move() { }

        public Move(Move m)
        {
            Value = m.Value;
        }

        public Move(uint value)
        {
            Value = value;
        }

        public override string ToString()
        {
            var output = Board.SquareNames[From] + Board.SquareNames[To];
            if (Promotion > 0)
            {
                output += "=";
                switch ((Piece)Promotion)
                {
                    case Piece.Knight:
                        output += "N";
                        break;
                    case Piece.Bishop:
                        output += "B";
                        break;
                    case Piece.Rook:
                        output += "R";
                        break;
                    case Piece.Queen:
                        output += "Q";
                        break;
                }
            }
            return output;
        }

        public string ToAlegbraicNotation(Board b)
        {
            //find Piece
            String output = String.Empty;
            MoveBits bits = 0;
            if ((b.Knights[b.SideToMove] & BitMask.Mask[From]) > 0)
                bits = MoveBits.Knight;
            else if ((b.Bishops[b.SideToMove] & BitMask.Mask[From]) > 0)
                bits = MoveBits.Bishop;
            else if ((b.Rooks[b.SideToMove] & BitMask.Mask[From]) > 0)
                bits = MoveBits.Rook;
            else if ((b.Queens[b.SideToMove] & BitMask.Mask[From]) > 0)
                bits = MoveBits.Queen;
            else if ((b.Pawns[b.SideToMove] & BitMask.Mask[From]) > 0)
                bits = MoveBits.Pawn;
            else if ((b.King[b.SideToMove] & BitMask.Mask[From]) > 0)
                bits = MoveBits.King;

            if (bits == MoveBits.Pawn)
            {
                if ((Bits & (byte)MoveBits.Capture) > 0)
                {
                    output += outputFiles[From.File()];
                }
            }
            else if (bits == MoveBits.Knight)
            {
                output += 'N';
                var sqs = MoveGenerator.KnightMoves[To] & b.Knights[b.SideToMove];
                if ((sqs).Count() > 1)
                    output += FileOrRank(b, bits);
            }
            else if (bits == MoveBits.Bishop)
            {
                output += 'B';
                var sqs = MoveGenerator.BishopAttacks(b, To) & b.Bishops[b.SideToMove];
                if (sqs.Count() > 1)
                    output += FileOrRank(b, bits);                
            }
            else if (bits == MoveBits.Rook)
            {
                output += 'R';
                var sqs = MoveGenerator.RookAttacks(b, To) & b.Rooks[b.SideToMove];
                if (sqs.Count() > 1)
                    output += FileOrRank(b, bits);
                
            }
            else if (bits == MoveBits.Queen)
            {
                var sqs = MoveGenerator.QueenAttacks(b, To) & b.Queens[b.SideToMove];
                output += 'Q';
                if (sqs.Count() > 1)
                    output += FileOrRank(b,bits);
                
            }
            else if (bits == MoveBits.King)
            {
                //if we are castling
                if (Math.Abs(To.File() - From.File()) > 1)
                {
                    if (To == 6 || To == 62)
                        return "O-O";
                    else
                        return "O-O-O";
                }

                output += 'K';
            }
            else
                output = Board.SquareNames[From];

            if ((Bits & (byte)MoveBits.Capture) > 0)
                output += 'x';

            output += Board.SquareNames[To];

            if (Promotion > 0)
            {
                output += "=";
                switch ((Piece)Promotion)
                {
                    case Piece.Knight:
                        output += "N";
                        break;
                    case Piece.Bishop:
                        output += "B";
                        break;
                    case Piece.Rook:
                        output += "R";
                        break;
                    case Piece.Queen:
                        output += "Q";
                        break;
                }
            }

            b.MakeMove(this);
            if (b.InCheck(b.SideToMove))
                output += "+";
            b.UnMakeMove();

            return output;

        }

        String FileOrRank(Board b, MoveBits bits)
        {
            ulong bitboard = 0;
            switch (bits)
            {
                case MoveBits.Bishop:
                    bitboard = b.Bishops[b.SideToMove];
                    break;
                case MoveBits.Rook:
                    bitboard = b.Rooks[b.SideToMove];
                    break;
                case MoveBits.Queen:
                    bitboard = b.Queens[b.SideToMove];
                    break;
                case MoveBits.Knight:
                    bitboard = b.Knights[b.SideToMove];
                    break;
                default:
                    return String.Empty;
            }
            if ((Board.FileMask[From.File()] & bitboard).Count() == 1)
                return outputFiles[From.File()].ToString();
            if ((Board.RankMask[From.Rank()] & bitboard).Count() == 1)
                return outputRanks[From.Rank()].ToString();
            return Board.SquareNames[From];


        }

        public static Move ParseMove(Board b, string moveString)
        {
            if (moveString.Length < 4)
                return null;
            var from = moveString.Substring(0, 2);
            var to = moveString.Substring(2, 2);
            int fromIndex = -1, toIndex = -1;
            for (int i = 0; i < 64; i++)
            {
                if (Board.SquareNames[i] == from)
                    fromIndex = i;
                if (Board.SquareNames[i] == to)
                    toIndex = i;
            }

            if (fromIndex == -1 || toIndex == -1)
                return null;

            MoveBits bits = 0;
            Piece promotion = 0;
            if ((b.Knights[b.SideToMove] & BitMask.Mask[fromIndex]) > 0)
                bits |= MoveBits.Knight;
            else if ((b.Bishops[b.SideToMove] & BitMask.Mask[fromIndex]) > 0)
                bits |= MoveBits.Bishop;
            else if ((b.Rooks[b.SideToMove] & BitMask.Mask[fromIndex]) > 0)
                bits |= MoveBits.Rook;
            else if ((b.Queens[b.SideToMove] & BitMask.Mask[fromIndex]) > 0)
                bits |= MoveBits.Queen;
            else if ((b.Pawns[b.SideToMove] & BitMask.Mask[fromIndex]) > 0)
            {
                //make sure we are making a legal pawn move
                if (Math.Abs(toIndex.File() - fromIndex.File()) > 0)
                {
                    if ((b.Pieces[b.SideToMove ^ 1] & BitMask.Mask[toIndex]) == 0 &&
                        BitMask.Mask[toIndex] != b.EnPassant)
                        return null;
                }
                bits |= MoveBits.Pawn;

                if (moveString.Length == 6)
                {
                    //Promotion
                    if (moveString[4] == '=')
                    {
                        switch (moveString[5].ToString().ToUpper())
                        {
                            case "Q":
                                promotion = Piece.Queen;
                                break;
                            case "R":
                                promotion = Piece.Rook;
                                break;
                            case "B":
                                promotion = Piece.Bishop;
                                break;
                            case "N":
                                promotion = Piece.Knight;
                                break;
                        }
                    }
                }
            }
            else if ((b.King[b.SideToMove] & BitMask.Mask[fromIndex]) > 0)
                bits |= MoveBits.King;
            else
                return null;

            if ((b.AllPieces & BitMask.Mask[toIndex]) > 0)
            {
                //let's make sure we are capturing the right Piece
                if ((b.Pieces[b.SideToMove] & BitMask.Mask[toIndex]) > 0)
                    return null;

                bits |= MoveBits.Capture;
            }
            else if((bits & MoveBits.Pawn)>0 && BitMask.Mask[toIndex]==b.EnPassant)
                bits |= MoveBits.Capture;
            

            return new Move
            {
                From = (byte)fromIndex,
                To = (byte)toIndex,
                Bits = (byte)bits,
                Promotion = (byte)promotion
            };
        }
    }


}