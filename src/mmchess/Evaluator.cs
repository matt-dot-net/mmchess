using System;
namespace mmchess
{


    public enum PieceValues
    {
        Rook = 500,
        Knight = 300,
        Bishop = 310,
        Pawn = 100,
        Queen = 900
    }



    public static class Evaluator
    {
        public static readonly Int16 NOT_CASTLED_PENALTY = -50;
        public static readonly Int16[,] PawnDevelopment = new Int16[2, 64]
        {
            {
             0 ,0 ,0 ,0 ,0 ,0 ,0 , 0,
             24,24,24,24,24,24,24,24,
             16,16,16,16,16,16,16,16,
             8 ,10,12,16,16,12,10, 8,
             2 , 4, 4, 8, 8, 4, 4, 2,
             2 , 4, 4, 4, 4, 4, 4, 2,
            -2 ,-2,-2,-2,-2,-2,-2,-2,
             0 , 0, 0, 0, 0, 0, 0, 0
            },
            {
             0 ,0 ,0 ,0 ,0 ,0 ,0 , 0,
            -2 ,-2,-2,-2,-2,-2,-2,-2,
             2 , 4, 4, 4, 4, 4, 4, 2,
             2 , 4, 4, 8, 8, 4, 4, 2,
             8 ,10,12,16,16,12,10, 8,
             16,16,16,16,16,16,16,16,
             24,24,24,24,24,24,24,24,
             0 ,0 ,0 ,0 ,0 ,0 ,0 , 0,
            },

        };

        public static readonly Int16[,] KnightDevelopment = new Int16[2, 64]
        {
            {
            -5,  15, 20, 20, 20, 20, 15, -5,
            -10, 20, 25, 25, 25, 25, 25,-10,
            -15, 20, 25, 25, 25, 25, 25,-15,
            -18, 20, 20, 30, 30, 20, 20,-18,
            -20, 20, 10, 30, 30, 20, 20,-20,
            -20, 05, 05, 05 ,05, 05, 05,-20,
            -20, 00, 00, 00 ,00, 00, 00,-20,
            -33,-33,-33,-33,-33,-33,-33,-33
            },
            {
            -33,-33,-33,-33,-33,-33,-33,-33,
            -20, 00, 00, 00 ,00, 00, 00,-20,
            -20, 05, 05, 05 ,05, 05, 05,-20,
            -20, 20, 10, 30, 30, 20, 20, -20,
            -18, 20, 20, 30, 30, 20, 20, -18,
            -15, 20, 25, 25, 25, 25, 25, -15,
            -10, 20, 25, 25, 25, 25, 25, -10,
            -5,  15, 20, 20, 20, 20, 15, -5
            }
        };

        public static readonly Int16[,] BishopDevelopment = new Int16[2, 64]
        {
            {
             00, 16, 16, 16, 16, 16, 16, 00,
             00, 16, 16, 16, 16, 16, 16, 00,
             00, 16, 16, 16, 16, 16, 16, 00,
             00, 16, 16, 16, 16, 16, 16, 00,
             00, 08, 16, 16, 16, 16, 08, 00,
             00, 04, 08, 08, 08, 08, 04, 00,
             04, 16, 04, 04, 04, 04, 16, 04,
             00, 00,-10, 00 ,00,-10, 00, 00
            },
            {
            00, 00,-10, 00 ,00,-10, 00, 00,
            04, 16, 04, 04, 04, 04, 16, 04,
            00, 04, 08, 08, 08, 08, 04, 00,
            00, 08, 16, 16, 16, 16, 08, 00,
            00, 16, 16, 16, 16, 16, 16, 00,
            00, 16, 16, 16, 16, 16, 16, 00,
            00, 16, 16, 16, 16, 16, 16, 00,
            00, 16, 16, 16, 16, 16, 16, 00,
            }
        };

        public static int MovingPieceValue(MoveBits bits)
        {
            if ((bits & MoveBits.Bishop) > 0)
                return (int)PieceValues.Bishop;
            if ((bits & MoveBits.Knight) > 0)
                return (int)PieceValues.Knight;
            if ((bits & MoveBits.Rook) > 0)
                return (int)PieceValues.Rook;
            if ((bits & MoveBits.Queen) > 0)
                return (int)PieceValues.Queen;
            if ((bits & MoveBits.Pawn) > 0)
                return (int)PieceValues.Pawn;
            return int.MaxValue; // king
        }

        public static int PieceValueOnSquare(Board b, int sq)
        {

            if (((b.Rooks[0] | b.Rooks[1]) & BitMask.Mask[sq]) > 0)
                return (int)PieceValues.Rook;
            if (((b.Queens[0] | b.Queens[1]) & BitMask.Mask[sq]) > 0)
                return (int)PieceValues.Rook;
            if (((b.Bishops[0] | b.Bishops[1]) & BitMask.Mask[sq]) > 0)
                return (int)PieceValues.Bishop;
            if (((b.Knights[0] | b.Knights[1]) & BitMask.Mask[sq]) > 0)
                return (int)PieceValues.Knight;
            if (((b.Pawns[0] | b.Pawns[1]) & BitMask.Mask[sq]) > 0)
                return (int)PieceValues.Pawn;
            if (((b.King[0] | b.King[1]) & BitMask.Mask[sq]) > 0)
                return int.MaxValue;

            return 0;

        }

        public static int Evaluate(Board b)
        {
            int eval = 0;
            eval += EvaluateMaterial(b);

            eval += EvaluateDevelopment(b);

            return eval;
        }

        static int EvaluateDevelopment(Board b)
        {
            int[] eval = new int[2];
            for (int side = 0; side < 2; side++)
            {
                int xside = side ^ 1;
                ulong pieces;

                pieces = b.Knights[side];
                while (pieces > 0)
                {
                    var sq = pieces.BitScanForward();
                    pieces ^= BitMask.Mask[sq];

                    eval[side] += KnightDevelopment[side, sq];
                }

                pieces = b.Bishops[side];
                while (pieces > 0)
                {
                    var sq = pieces.BitScanForward();
                    pieces ^= BitMask.Mask[sq];

                    eval[side] += BishopDevelopment[side, sq];
                }

                pieces = b.Pawns[side];
                while (pieces > 0)
                {
                    var sq = pieces.BitScanForward();
                    pieces ^= BitMask.Mask[sq];
                    eval[side] += PawnDevelopment[side, sq];
                }

                //if the opponent has a queen on the board, evaluate castle status
                var kingSq = b.King[side].BitScanForward();
                if ((b.Queens[xside] > 0) && (b.Rooks[xside] > 0) && (2 < kingSq.File() && kingSq.File() < 6) &&
                    ((b.CastleStatus >> (2 * side)) & 3) > 0)
                    eval[side] += NOT_CASTLED_PENALTY;
            }
            return eval[b.SideToMove] - eval[b.SideToMove ^ 1];
        }

        static int EvaluateMaterial(Board b)
        {
            int eval = 0;
            int xside = b.SideToMove ^ 1;
            int side = b.SideToMove;
            eval += (b.Rooks[side].Count() - b.Rooks[xside].Count()) * (int)PieceValues.Rook;
            eval += (b.Bishops[side].Count() - b.Bishops[xside].Count()) * (int)PieceValues.Bishop;
            eval += (b.Queens[side].Count() - b.Queens[xside].Count()) * (int)PieceValues.Queen;
            eval += (b.Knights[side].Count() - b.Knights[xside].Count()) * (int)PieceValues.Knight;
            eval += (b.Pawns[side].Count() - b.Pawns[xside].Count()) * (int)PieceValues.Pawn;

            return eval;

        }
    }
}