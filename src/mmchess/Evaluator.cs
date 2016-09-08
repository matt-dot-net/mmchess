using System;
namespace mmchess
{

    public static class Evaluator
    {
        public static readonly int[] PieceValues = new int [7]{
            0,  //empty
            300, //knight
            310, //bishop
            500, //rook
            900, //queen
            100, //pawn  
            10000 //king
        };

        static readonly Int16 NotCastledPenalty = -50;
        static readonly int MinorMaterialInbalanceBonus = 100;
        static readonly int DoubledPawnPenalty = -8;
        static readonly int OpenFileInFrontOfCastledKingPenalty = -50;
        static readonly int KingUnderAttack = -150;

        static readonly ulong kingside = Board.FileMask[7] | Board.FileMask[6] | Board.FileMask[5];
        static readonly ulong queenside = Board.FileMask[0] | Board.FileMask[1] | Board.FileMask[2];

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
            return (int)PieceValues[(int)Move.GetPiece(bits)];
        }

        public static int PieceValueOnSquare(Board b, int sq)
        {

            if (((b.Rooks[0] | b.Rooks[1]) & BitMask.Mask[sq]) > 0)
                return PieceValues[(int)Piece.Rook];
            if (((b.Queens[0] | b.Queens[1]) & BitMask.Mask[sq]) > 0)
                return PieceValues[(int)Piece.Queen];
            if (((b.Bishops[0] | b.Bishops[1]) & BitMask.Mask[sq]) > 0)
                return PieceValues[(int)Piece.Bishop];
            if (((b.Knights[0] | b.Knights[1]) & BitMask.Mask[sq]) > 0)
                return PieceValues[(int)Piece.Knight];
            if (((b.Pawns[0] | b.Pawns[1]) & BitMask.Mask[sq]) > 0)
                return PieceValues[(int)Piece.Pawn];
            if (((b.King[0] | b.King[1]) & BitMask.Mask[sq]) > 0)
                return PieceValues[(int)Piece.King];

            return 0;

        }

        public static int Evaluate(Board b)
        {
            int eval = 0;

            if (EvaluateDraw(b))
                return 0;
            var e = new Evaluation(b);
            eval = e.Material;
            eval += EvaluateDevelopment(b);
            
            var pawnScore = EvaluatePawns(b);
            eval += pawnScore.Eval;

            eval += EvaluateKingSafety(b, pawnScore);

            //don't need to do these things if score is already bad
            if(eval > - PieceValues[(int)Piece.Knight]) // if i am down a knight or more, don't worry about these
            {
                eval += EvaluatePieces(e); 
            }
            return eval;
        }

        static int EvaluatePieces(Evaluation e)
        {
            //evaluate material imbalance...
            //only one of us can have this so it won't be evaluated twice
            int eval = 0;
            int side = e.Board.SideToMove;
            int xside = side ^1;
            // a rook+pawn for two pieces is not good
            if (e.Material < PieceValues[(int)Piece.Pawn])
            {
                if (e.Board.Rooks[xside].Count() == 2 &&     //opp has too rooks
                e.Board.Rooks[side].Count() == 1 &&    //i have one rook
                e.Board.Minors(xside).Count() == 2 &&  //opponent has two minors
                e.Board.Minors(side).Count() == 4) //i have four minors 
                    // I have 2 minors for one rook and a pawn
                   {
                    eval += MinorMaterialInbalanceBonus;
                }
            }

            return eval;

        }

        static bool EvaluateDraw(Board b)
        {
            if (b.History.IsGameDrawn(b.HashKey))
                return true;

            //if only the kings remain
            if (b.AllPieces.Count() == 2)
                return true;

            //if there are no pawns
            if ((b.Pawns[0] | b.Pawns[1]) == 0)
            {

                if ((b.Rooks[0] | b.Rooks[1] | b.Queens[0] | b.Queens[1]) > 0)
                    return false;

                if ((b.Knights[0] | b.Knights[1] | b.Bishops[0] | b.Bishops[1]).Count() == 1)
                    return true;
            }
            else
            {
                //todo calculate if king is in front of pawn...
            }
            return false;
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


            }
            return eval[b.SideToMove] - eval[b.SideToMove ^ 1];
        }

        public static int EvaluateMaterial(Board b)
        {
            int eval = 0;
            int xside = b.SideToMove ^ 1;
            int side = b.SideToMove;
            eval += (b.Rooks[side].Count() - b.Rooks[xside].Count()) * PieceValues[(int)Piece.Rook];
            eval += (b.Bishops[side].Count() - b.Bishops[xside].Count()) * PieceValues[(int)Piece.Bishop];
            eval += (b.Queens[side].Count() - b.Queens[xside].Count()) * PieceValues[(int)Piece.Queen];
            eval += (b.Knights[side].Count() - b.Knights[xside].Count()) * PieceValues[(int)Piece.Knight];
            eval += (b.Pawns[side].Count() - b.Pawns[xside].Count()) * PieceValues[(int)Piece.Pawn];

            return eval;

        }

        static int EvaluateKingSafety(Board b, PawnScore pawnScore)
        {

            int[] eval = new int[2];

            for (int side = 0; side < 2; side++)
            {
                var xside = side ^ 1;
                if ((kingside & b.King[side]) > 0)
                { // if my king is on the kingside

                    if ((b.Rooks[side] & Board.FileMask[7]) > 0) // if I have a rook in the corner
                    {
                        //if the opponent has a queen and rook on the board, evaluate castle status

                        if ((b.Queens[xside] > 0) &&
                            (b.Rooks[xside] > 0))
                            eval[side] += NotCastledPenalty;
                    }
                    else
                    {
                        //king looks safely tucked
                        //evaluate the pawns in front of my king
                        for (int f = 5; f < 8; f++)
                        {
                            //if I have no pawns on the file
                            if (pawnScore.Files[side, f] == 0)
                            {
                                eval[side] += OpenFileInFrontOfCastledKingPenalty;

                                //look for case where the opponent has an opening
                                //and a major piece, and has not castled in that direction
                                if (pawnScore.Files[xside, f] == 0 && //opponent has opened  the file  
                                    ((b.Rooks[xside] | b.Queens[xside]) & Board.FileMask[f]) > 0 && //heavypiece
                                    ((b.King[xside] & queenside) > 0))//opponents king is safely on the other side
                                {
                                    //major probjem
                                    eval[side] += KingUnderAttack;
                                }
                            }

                        }
                    }
                }
                else if ((queenside & b.King[side]) > 0)
                {
                    //my king is on the queenside
                    if ((b.Rooks[side] & Board.FileMask[0]) > 0) //there is a rook in the corner
                    {
                        //if the opponent has a queen and rook on the board, evaluate castle status

                        if ((b.Queens[xside] > 0) &&
                            (b.Rooks[xside] > 0))
                            eval[side] += NotCastledPenalty;
                    }
                    else
                    {
                        //king is tucked
                        //evaluate the pawns in front of my king
                        for (int f = 2; f >= 0; f--)
                        {
                            //if I have no pawns on the file
                            if (pawnScore.Files[side, f] == 0)
                            {
                                eval[side] += OpenFileInFrontOfCastledKingPenalty;

                                //look for case where the opponent has an opening
                                //and a major piece, and has not castled in that direction
                                if (pawnScore.Files[xside, f] == 0 && //opponent has opened  the file  
                                    ((b.Rooks[xside] | b.Queens[xside]) & kingside) > 0 && //heavypiece
                                    ((b.King[xside] & kingside) > 0)) //opponents king is safely on the other side
                                {
                                    //major probjem
                                    eval[side] += KingUnderAttack;
                                }
                            }

                        }
                    }
                }
                else
                {
                    //king is in the middle of the board
                    if ((b.Queens[xside] > 0) &&
                        (b.Rooks[xside] > 0))
                    {
                        eval[side] += NotCastledPenalty;
                    }
                }
            }
            return eval[b.SideToMove] - eval[b.SideToMove ^ 1];
        }

        static PawnScore EvaluatePawns(Board b)
        {
            int[] eval = new int[2];
            var fileMask = Board.FileMask;
            var returnVal = new PawnScore();
            for (int side = 0; side < 2; side++)
            {
                ulong pawns = b.Pawns[side];
                int xside = side ^ 1;
                ulong opponentPawns = b.Pawns[side ^ 1];


                //evaluate file by file
                for (int i = 0; i < 8; i++)
                {

                    returnVal.Files[side, i] = pawns & Board.FileMask[i];
                    returnVal.Files[xside, i] = opponentPawns & Board.FileMask[i];


                    //evaluate my doubled pawns
                    if (returnVal.Files[side, i].Count() > 1)
                    {
                        //doubled pawns
                        if (i > 0 && i < 7) //ignoring outside pawns
                                            //check for isolated doubled pawns
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
                }
            }

            returnVal.Eval = eval[b.SideToMove] - eval[b.SideToMove ^ 1];
            return returnVal;
        }
    }
}