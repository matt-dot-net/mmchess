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
        public static readonly Int16 NOT_CASTLED_PENALTY = -30;
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
            -10, 20, 25, 25, 25, 25, 25, -10,
            -15, 20, 25, 25, 25, 25, 25, -15,
            -18, 20, 20, 30, 30, 20, 20, -18,
            -20, 20, 10, 30, 30, 20, 20, -20,
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

        public static int Evaluate(Board b)
        {
            int eval = 0;
            eval += EvaluateMaterial(b);

            eval += EvaluateDevelopment(b);

            return eval;
        }

        static int EvaluateDevelopment(Board b)
        {
            int [] eval = new int[2];
            for (int side = 0; side < 2; side++)
            {
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

                if (((b.CastleStatus >> (2 * side)) & 3) > 0)
                    eval[side] += NOT_CASTLED_PENALTY;
            }
            return eval[b.SideToMove]-eval[b.SideToMove^1];
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