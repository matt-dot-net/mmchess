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
        Knight = 0,
        Bishop = 1,
        Rook = 2,
        Queen = 3,
        Pawn = 4,
        King = 5,
        Empty
    }
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public class Move
    {
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
        From = m.From;
        To = m.To;
        Bits = m.Bits;
        //Promotion=m.Promotion;
    }

    public Move (uint value){
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
            {
                output += Board.SquareNames[From];
            }
        }
        else if (bits == MoveBits.Bishop)
        {
            output += 'B';
        }
        else if (bits == MoveBits.Rook)
        {
            output += 'R';
            var sqs = MoveGenerator.RookAttacks(b, To) & b.Rooks[b.SideToMove];
            if (sqs.Count() > 1)
            {
                output += Board.SquareNames[From];
            }
        }
        else if (bits == MoveBits.Queen)
        {
            var sqs = MoveGenerator.QueenAttacks(b, To) & b.Queens[b.SideToMove];
            if (sqs.Count() > 1)
                output += Board.SquareNames[From];
            output += 'Q';
        }
        else if (bits == MoveBits.King)
        {
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

        return output;

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
                    switch (moveString[5])
                    {
                        case 'Q':
                            promotion = Piece.Queen;
                            break;
                        case 'R':
                            promotion = Piece.Rook;
                            break;
                        case 'B':
                            promotion = Piece.Bishop;
                            break;
                        case 'N':
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