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
        static readonly Int16 NotCastledPenalty = -50;

        static readonly int DoubledPawnPenalty = -8;
        static readonly int OpenFileInFrontOfCastledKingPenalty = -50;


        static readonly Int16[,] PawnDevelopment = new Int16[2, 64]
       {
            {
             0 ,0 ,0 ,0 ,0 ,0 ,0 , 0,
             24,24,24,24,24,24,24,24,
             16,16,16,16,16,16,16,16,
             8 ,10,12,16,16,12,10, 8,
             2 , 4, 4,16,16, 4, 4, 2,
             2 , 4, 4, 4, 4, 4, 4, 2,
             0 , 0, 0,-12,-12,0,0, 0,
             0 , 0, 0, 0, 0, 0, 0, 0
            },
            {
             0 ,0 ,0 ,0 ,0 ,0 ,0 , 0,
             0 , 0, 0,-12,-12,0,0, 0,
             2 , 4, 4, 4, 4, 4, 4, 2,
             2 , 4, 4,16,16, 4, 4, 2,
             8 ,10,12,16,16,12,10, 8,
             16,16,16,16,16,16,16,16,
             24,24,24,24,24,24,24,24,
             0 ,0 ,0 ,0 ,0 ,0 ,0 , 0,
            },

       };

        static readonly Int16[,] KnightDevelopment = new Int16[2, 64]
       {
             {
             0,  0,  0,  0,  0,  0,  0, 0,
             4, 16, 16, 16, 16, 16, 16, 4,
             4, 16, 16, 16, 16, 16, 16, 4,
             -8, 16, 16, 16, 16, 16, 16, -8,
             -8, 08, 16, 16, 16, 16, 08, -8,
             -20, 04, 08, 08, 08, 08, 04, -20,
             -25, 16, 04, 04, 04, 04, 16, -25,
            -25,-25,-25,-25,-25,-25,-25,-25
            },
            {
            -25,-25,-25,-25,-25,-25,-25,-25,
            -25, 16, 04, 04, 04, 04, 16, -25,
            -20, 04, 08, 08, 08, 08, 04, -20,
            -8, 08, 16, 16, 16, 16, 08, -8,
            -8, 16, 16, 16, 16, 16, 16, -8,
            4, 16, 16, 16, 16, 16, 16, 4,
            4, 16, 16, 16, 16, 16, 16, 4,
            0,  0,  0,  0,  0,  0,  0, 0,
            }
       };

        static readonly Int16[,] BishopDevelopment = new Int16[2, 64]
       {
            {
             00, 16, 16, 16, 16, 16, 16, 00,
             00, 16, 16, 16, 16, 16, 16, 00,
             00, 16, 16, 16, 16, 16, 16, 00,
             00, 16, 16, 16, 16, 16, 16, 00,
             00, 08, 16, 16, 16, 16, 08, 00,
             00, 04, 08, 08, 08, 08, 04, 00,
             04, 16, 04, 04, 04, 04, 16, 04,
             00, 00,-25, 00 ,00,-25, 00, 00
            },
            {
            00, 00,-25, 00 ,00,-25, 00, 00,
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
                if ((b.Queens[xside] > 0) && (b.Rooks[xside] > 0) && (2 < kingSq.File() && kingSq.File() < 6))
                    eval[side] += NotCastledPenalty;
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

        static int EvaluatePawns(Board b)
        {
            int[] eval = new int[2];
            var fileMask = Board.FileMask;

            for (int side = 0; side < 2; side++)
            {
                ulong pawns = b.Pawns[side];
                int xside = side ^ 1;
                ulong opponentPawns = b.Pawns[side ^ 1];
                int[] halfOpenFiles = new int[8];

                //evaluate file by file
                for (int i = 0; i < 8; i++)
                {

                    var myPawns = pawns & Board.FileMask[i];
                    var theirPawns = b.Pawns[xside] & Board.FileMask[i];
                    var bothPawns = (myPawns | theirPawns);

                    //evaluate my doubled pawns
                    //note we like the opponent to have doubled pawns
                    if (myPawns.Count() > 1)
                    {
                        //doubled pawns
                        if (i > 0 && i < 8) //ignoring outside pawns
                            if ((pawns & Board.FileMask[i - 1]) == 0 &&
                                (pawns & Board.FileMask[i + 1]) == 0)
                            {
                                eval[side] += 4 * DoubledPawnPenalty;
                            }
                            else
                            {
                                eval[side] += 2 * DoubledPawnPenalty;
                            }
                    }

                    //open files in front of king
                    if (i < 3 && i > 4)
                    {
                        if (myPawns == 0 && (b.King[side] & fileMask[i]) > 0)
                        {
                            if(bothPawns == 0)
                                eval[side] += 2*OpenFileInFrontOfCastledKingPenalty;
                            else
                                eval[side] += OpenFileInFrontOfCastledKingPenalty; //half open file
                        }
                    }
                }
            }

            return eval[b.SideToMove] - eval[b.SideToMove ^ 1];
        }
    }
}