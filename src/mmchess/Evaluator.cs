using System;
namespace mmchess
{

    public static class Evaluator
    {
        enum GamePhase
        {
            Opening,
            MiddleGame,
            EndGame
        };

        public static readonly int[] PieceValues = new int[7]{
            0,  //empty
            300, //knight
            310, //bishop
            500, //rook
            900, //queen
            100, //pawn  
            10000 //king
        };

        static readonly int[] RookBonusPawnCount = new int[17] {
            20,20,20,20, //0-3 pawns on the board
            15,15,15,15, //4-7 pawns on the board
            10,10,10,10, //8-11 pawns on the board
            0, 0, 0, 0, 0 //12-16 pawns on the board
        };

        const int KnightOnOutpostBonus = 66;
        const int RookOnOpenFileBonus = 25;
        const int RookOnSeventhBonus = 25;
        const int BishopPairBonus = 50;
        const int NotCastledPenalty = -50;
        const int MinorMaterialInbalanceBonus = 100;
        const int DoubledPawnPenalty = -8;
        const int OpenFileInFrontOfCastledKingPenalty = -50;
        const int KingUnderAttack = -150;
        const int PassedPawnBonus = 20;
        const int BlockedPawnPenalty = -8;
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

        static readonly Int16[,] KingEndgame = new Int16[2, 64]
        {
            {
             -25, 32, 32, 32, 32, 32, 32,-25,
             -25, 32, 32, 32, 32, 32, 32,-25,
             -25, 24, 24, 24, 24, 24, 24,-25,
             -25, 16, 16, 16, 16, 16, 16,-25,
             -25,-25, 08, 16, 16, 08,-25,-25,
             -25,-25,-10, 08, 08,-10,-25,-25,
             -25,-25,-25,-25,-25,-25,-25,-25,
             -40,-40,-40,-40,-40,-40,-40,-40
            },
            {
            -40,-40,-40,-40,-40,-40,-40,-40,
            -25,-25,-25,-25,-25,-25,-25,-25,
            -25,-25,-10, 08, 08,-10,-25,-25,
            -25,-25, 08, 16, 16, 08,-25,-25,
            -25, 16, 16, 16, 16, 16, 16,-25,
            -25, 24, 24, 24, 24, 24, 24,-25,
            -25, 32, 32, 32, 32, 32, 32,-25,
            -25, 32, 32, 32, 32, 32, 32,-25,
            }
        };

        public static ulong[,] PassedPawnMask = new ulong[2, 64];

        static Evaluator()
        {
            //white pawns
            for (int sq = 8; sq < 56; sq++)
            {
                if (Math.Abs((sq - 9).File() - sq.File()) == 1)
                {
                    for (int minus9 = sq - 9; minus9 > 7; minus9 -= 8)
                        PassedPawnMask[0, sq] |= BitMask.Mask[minus9];
                }
                if (Math.Abs((sq - 7).File() - sq.File()) == 1)
                {
                    for (int minus7 = sq - 7; minus7 > 7; minus7 -= 8)
                        PassedPawnMask[0, sq] |= BitMask.Mask[minus7];
                }
                for (int minus8 = sq - 8; minus8 > 7; minus8 -= 8)
                    PassedPawnMask[0, sq] |= BitMask.Mask[minus8];
            }

            //black pawns
            for (int sq = 8; sq < 56; sq++)
            {
                if (Math.Abs((sq + 9).File() - sq.File()) == 1)
                {
                    for (int plus9 = sq + 9; plus9 < 56; plus9 += 8)
                        PassedPawnMask[1, sq] |= BitMask.Mask[plus9];
                }
                if (Math.Abs((sq + 7).File() - sq.File()) == 1)
                {
                    for (int plus7 = sq + 7; plus7 < 56; plus7 += 8)
                        PassedPawnMask[1, sq] |= BitMask.Mask[plus7];
                }
                for (int plus8 = sq + 8; plus8 < 56; plus8 += 8)
                    PassedPawnMask[1, sq] |= BitMask.Mask[plus8];
            }
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

        static GamePhase CalculateGamePhase(Board b)
        {
            if (b.CastleStatus > 0)
                return GamePhase.Opening;
            else if (b.PieceCount(b.SideToMove) >= 2 && b.PieceCount(b.SideToMove ^ 1) >= 2)
                return GamePhase.MiddleGame;
            return GamePhase.EndGame;
        }

        static int EvaluateWinners(Board b, Evaluation e)
        {
            int canWin=3;

            //if there is major material then everyone can win
            if(e.Majors[0]>0 && e.Majors[1] > 0)
                return canWin;
            for(int i=0;i<2;i++)
            {
                var opp = i^1;
                //a major piece means i can win
                if(e.Majors[i]>0)
                    continue;

                if(b.Pawns[i]==0)
                {
                    //if I have no pawns, i need two minors
                    if(e.Minors[i] < 2){
                        canWin &= 1<<opp;
                        continue;
                    }
                }    
            }

            return canWin;

        }

        //evaluate returns a score that is from the perspective
        //of the side to move.
        public static int Evaluate(Board b, int alpha, int beta)
        {
            int eval = 0;
            var gamePhase = CalculateGamePhase(b);
            
            var e = new Evaluation(b);
            eval = e.Material;

            if(b.History.IsPositionDrawn(b.HashKey))
                return 0;

            var canWin = EvaluateWinners(b,e);
            if(b.SideToMove == 0 && (canWin&2)==0)
                return 0;
            if(b.SideToMove == 1 && (canWin&1)==0)
                return 0;
            

            //attempt a lazy exit
            if (eval <= alpha - 300 || eval >= beta + 300)
                return eval;

            e.PawnScore = EvaluatePawns(b);
            eval += e.PawnScore.Eval;
            if (gamePhase != GamePhase.EndGame)
            {
                eval += EvaluateDevelopment(b);
                eval += EvaluateKingSafety(b, e.PawnScore);
                eval += EvaluatePieces(e);
            }
            else
            {
                eval += EvaluateKingEndGame(b);
            }


            return eval;
        }

        static int EvaluateKingEndGame(Board b)
        {
            int[] eval = new int[2];
            for (int i = 0; i < 2; i++)
            {
                var sq = b.King[i].BitScanForward();
                eval[i] = KingEndgame[i, sq];
            }

            return eval[b.SideToMove] - eval[b.SideToMove ^ 1];
        }

        static int EvaluatePieces(Evaluation e)
        {
            //evaluate things like material imbalance,
            //bishop pair, knights supported by pawns, etc.

            int[] eval = new int[2];
            for (int side = 0; side < 2; side++)
            {
                int xside = side ^ 1;

                //evaluate material imbalances
                // a rook+pawn for two pieces is not good

                //if side has only one rook and opponent has two
                if (e.Board.Rooks[side] > 0 &&
                    e.Board.Rooks[xside] > 0 &&
                    e.Board.Rooks[side].Count() == 1 &&
                    e.Board.Rooks[xside].Count() == 2)
                {
                    //if opposite side has two less minors then we get a bonus
                    if (e.Board.Minors(side).Count() - e.Board.Minors(xside).Count() == 2)
                        eval[side] += MinorMaterialInbalanceBonus;
                }

                //bishop pair bonus
                eval[side] += (e.Board.Bishops[side].Count() == 2) ? BishopPairBonus : 0;

                //increase the value of rooks as pawns come off the board
                int totalPawns = e.Board.Pawns[0].Count() + e.Board.Pawns[1].Count();
                eval[side] += RookBonusPawnCount[totalPawns];

                //rooks
                var rooks = e.Board.Rooks[side];
                while (rooks > 0)
                {
                    int sq = rooks.BitScanForward();
                    rooks ^= BitMask.Mask[sq];

                    //open file bonus
                    if (e.PawnScore.Files[side, sq.File()] == 0)
                    {
                        eval[side] += RookOnOpenFileBonus / 2;
                        if (e.PawnScore.Files[xside, sq.File()] == 0)
                            eval[side] = RookOnOpenFileBonus;
                    }

                    //rooks on the seventh (or eigth)
                    if (side == 0 && sq.Rank() <= 1)
                        eval[0] += RookOnSeventhBonus;
                    else if (side == 1 && sq.Rank() >= 6)
                        eval[1] += RookOnSeventhBonus;
                }

                //knights
                var knights = e.Board.Knights[side];
                while (knights > 0)
                {
                    int sq = knights.BitScanForward();
                    knights ^= BitMask.Mask[sq];

                    //only looking for forward knights
                    if (side == 0 && sq.Rank() > 3)
                        continue;
                    else if (side == 1 && sq.Rank() < 3)
                        continue;

                    //look for knights on outposts
                    if ((MoveGenerator.PawnAttacks[xside, sq] & e.Board.Pawns[side]) > 0)
                    {
                        //knight is defended by a pawn
                        //use the passed pawn lookup - minus the file to see if
                        //we have an ouput
                        if ((PassedPawnMask[side, sq] & ~Board.FileMask[sq.File()] & e.Board.Pawns[xside]) == 0)
                            eval[side] += KnightOnOutpostBonus;

                    }

                    EvalTrappedKnights(e, eval, side, sq);
                }

                var bishops = e.Board.Bishops[side];
                while (bishops > 0)
                {
                    int sq = bishops.BitScanForward();
                    bishops ^= BitMask.Mask[sq];

                    //evaluate trapped bishops
                    if (side == 0)
                    {
                        if (sq == 23 && //white bishop on h6
                            (e.Board.Pawns[1] & BitMask.Mask[30]) > 0 &&
                            (e.Board.Pawns[1] & BitMask.Mask[21]) > 0)
                            eval[side] -= PieceValues[(int)Piece.Bishop] / 3;

                        else if (sq == 16 && //white bishop on a6
                            (e.Board.Pawns[1] & BitMask.Mask[25]) > 0 &&
                            (e.Board.Pawns[1] & BitMask.Mask[18]) > 0)
                            eval[side] -= PieceValues[(int)Piece.Bishop] / 3;
                    }
                    else
                    {
                        if (sq == 47 && //blackbishop on h3
                          (e.Board.Pawns[0] & BitMask.Mask[38]) > 0 &&
                          (e.Board.Pawns[0] & BitMask.Mask[47]) > 0)
                            eval[side] -= PieceValues[(int)Piece.Bishop] / 3;
                        else if (sq == 40 && //black bishop on a3
                          (e.Board.Pawns[0] & BitMask.Mask[33]) > 0 &&
                          (e.Board.Pawns[0] & BitMask.Mask[42]) > 0)
                            eval[side] -= PieceValues[(int)Piece.Bishop] / 3;
                    }

                    //mobility score
                    var mobilitySquares = MoveGenerator.BishopAttacks(e.Board, sq);
                    eval[side] += mobilitySquares.Count();
                }

            }

            return eval[e.Board.SideToMove] - eval[e.Board.SideToMove ^ 1];
        }

        private static void EvalTrappedKnights(Evaluation e, int[] eval, int side, int sq)
        {
            //look for trapped knights
            if (side == 0)
            {
                if (sq == 7) // white knight on h8
                {
                    if ((e.Board.Pawns[1] & (BitMask.Mask[13] | BitMask.Mask[15])) > 0)
                        eval[0] -= PieceValues[(int)Piece.Knight] / 2; //this trap costs us half a knight
                }
                else if (sq == 0)
                {  //white knight on a8
                    if ((e.Board.Pawns[1] & (BitMask.Mask[8] | BitMask.Mask[10])) > 0)
                        eval[0] -= PieceValues[(int)Piece.Knight] / 2; //this trap costs us half a knight

                }
                else if (sq == 15) //white knight on h7
                {
                    if ((e.Board.Pawns[1] & BitMask.Mask[14]) > 0 &&
                        (e.Board.Pawns[1] & (BitMask.Mask[23] | BitMask.Mask[21])) > 0)
                        eval[0] -= PieceValues[(int)Piece.Knight] / 2; //this trap costs us half a knight
                }
                else if (sq == 08) //white knight on a7
                {
                    if ((e.Board.Pawns[1] & BitMask.Mask[9]) > 0 &&
                        (e.Board.Pawns[1] & (BitMask.Mask[16] | BitMask.Mask[18])) > 0)
                        eval[0] -= PieceValues[(int)Piece.Knight] / 2; //this trap costs us half a knight
                }

            }
            else
            {
                if (sq == 63) // black knight on h1
                {
                    if ((e.Board.Pawns[0] & (BitMask.Mask[53] | BitMask.Mask[55])) > 0)
                        eval[1] -= PieceValues[(int)Piece.Knight] / 2; //this trap costs us half a knight
                }
                else if (sq == 56)
                { //black knight on a1
                    if ((e.Board.Pawns[0] & (BitMask.Mask[48] | BitMask.Mask[50])) > 0)
                        eval[1] -= PieceValues[(int)Piece.Knight] / 2; //this trap costs us half a knight
                }
                else if (sq == 55) //black knight on h2
                {
                    if ((e.Board.Pawns[0] & BitMask.Mask[54]) > 0 &&
                         (e.Board.Pawns[0] & (BitMask.Mask[45] | BitMask.Mask[47])) > 0)
                        eval[1] -= PieceValues[(int)Piece.Knight] / 2; //this trap costs us half a knight
                }
                else if (sq == 48)//black knight on a2
                {
                    if ((e.Board.Pawns[0] & BitMask.Mask[49]) > 0 &&
                        (e.Board.Pawns[0] & (BitMask.Mask[40] | BitMask.Mask[42])) > 0)
                        eval[1] -= PieceValues[(int)Piece.Knight] / 2; //this trap costs us half a knight

                }
            }
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

                                // //look for case where the opponent has an opening
                                // //and a major piece, and has not castled in that direction
                                // if (pawnScore.Files[xside, f] == 0 && //opponent has opened  the file  
                                //     ((b.Rooks[xside] | b.Queens[xside]) & Board.FileMask[f]) > 0 && //heavypiece
                                //     ((b.King[xside] & queenside) > 0))//opponents king is safely on the other side
                                // {
                                //     //major probjem
                                //     eval[side] += KingUnderAttack;
                                // }
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

                                // //look for case where the opponent has an opening
                                // //and a major piece, and has not castled in that direction
                                // if (pawnScore.Files[xside, f] == 0 && //opponent has opened  the file  
                                //     ((b.Rooks[xside] | b.Queens[xside]) & kingside) > 0 && //heavypiece
                                //     ((b.King[xside] & kingside) > 0)) //opponents king is safely on the other side
                                // {
                                //     //major probjem
                                //     eval[side] += KingUnderAttack;
                                // }
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

                //individual pawns
                var p = pawns;
                while (p > 0)
                {
                    var pawnsq = p.BitScanForward();
                    p ^= BitMask.Mask[pawnsq];
                    //passed pawns
                    if ((PassedPawnMask[side, pawnsq] & opponentPawns) == 0)
                    {
                        var distanceMultiplier = side == 1 ? pawnsq.Rank() : 8 - pawnsq.Rank();
                        eval[side] +=
                            PassedPawnBonus * distanceMultiplier;
                    }

                    //blocked pawns
                    if (side == 0 && pawnsq > 7)
                        if ((b.AllPieces & BitMask.Mask[pawnsq - 8]) > 0)
                            eval[side] += BlockedPawnPenalty;
                        else if (side == 1 && pawnsq < 56)
                            if ((b.AllPieces & BitMask.Mask[pawnsq + 8]) > 0)
                                eval[side] += BlockedPawnPenalty;
                }
            }

            returnVal.Eval = eval[b.SideToMove] - eval[b.SideToMove ^ 1];
            return returnVal;
        }
    }
}