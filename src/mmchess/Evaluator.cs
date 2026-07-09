using System;
namespace mmchess;


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
    const int SemiOpenFileInFrontOfKingPenalty = -18;
    const int KingZoneAttackPenalty = -12;
    const int CenterKingWithEnemyQueenPenalty = -35;
    const int PassedPawnBonus = 20;
    const int BlockedPawnPenalty = -8;
    const int BlockedCentralPawnPenalty = -20;
    const int RookNotDevelopedPenalty = -15;
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

    //Chebyshev distance to the nearest of d4/e4/d5/e5 (0..3)
    static readonly int[] CenterDistance = new int[64];

    static Evaluator()
    {
        for (int sq = 0; sq < 64; sq++)
        {
            CenterDistance[sq] = Math.Min(
                Math.Min(SquareExtensions.KingDistance(sq, 27),   //d5
                         SquareExtensions.KingDistance(sq, 28)),  //e5
                Math.Min(SquareExtensions.KingDistance(sq, 35),   //d4
                         SquareExtensions.KingDistance(sq, 36))); //e4
        }

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

    //bit i of the result is set when side i has enough material to
    //possibly deliver mate. A side with a pawn or a major can always win;
    //without either it needs two minors that aren't both knights
    //(KNN v K cannot force mate).
    static int EvaluateWinners(Board b, Evaluation e)
    {
        int canWin = 3;

        for (int i = 0; i < 2; i++)
        {
            if (e.Majors[i] > 0 || b.Pawns[i] != 0)
                continue;

            if (e.Minors[i] < 2 ||
                (e.Minors[i] == 2 && b.Bishops[i] == 0))
                canWin &= ~(1 << i);
        }

        return canWin;
    }

    //KB+rook-pawn(s) vs bare king where the bishop doesn't control the
    //promotion corner and the defending king is in or next to it - a
    //classic fortress the attacker cannot break
    static bool IsWrongRookPawnDraw(Board b, Evaluation e, int side)
    {
        int xside = side ^ 1;
        if (e.Majors[side] > 0 || b.Knights[side] != 0)
            return false;
        if (b.Bishops[side].Count() != 1 || b.Pawns[side] == 0)
            return false;
        //only claim the fortress against a bare king
        if (b.PieceCount(xside) > 0 || b.Pawns[xside] != 0)
            return false;

        int file;
        if ((b.Pawns[side] & ~Board.FileMask[0]) == 0)
            file = 0;
        else if ((b.Pawns[side] & ~Board.FileMask[7]) == 0)
            file = 7;
        else
            return false;

        //promotion corner: rank 8 (squares 0-7) for white, rank 1 for black
        int corner = side == 0 ? file : 56 + file;
        if (b.Bishops[side].BitScanForward().IsLightSquare() == corner.IsLightSquare())
            return false; //right bishop - it controls the corner

        return SquareExtensions.KingDistance(b.King[xside].BitScanForward(), corner) <= 1;
    }

    //pure opposite-colored-bishop ending: one bishop each on opposite
    //colors and no other pieces - notoriously drawish even pawns down
    static bool IsOppositeBishopEnding(Board b, Evaluation e)
    {
        if (e.Majors[0] > 0 || e.Majors[1] > 0)
            return false;
        if (b.Knights[0] != 0 || b.Knights[1] != 0)
            return false;
        if (b.Bishops[0].Count() != 1 || b.Bishops[1].Count() != 1)
            return false;
        return b.Bishops[0].BitScanForward().IsLightSquare() !=
               b.Bishops[1].BitScanForward().IsLightSquare();
    }

    //a side that cannot mate can do no better than draw (cap at 0);
    //a side whose opponent cannot mate can never lose (floor at 0)
    static int ApplyWinnability(int eval, int canWin, int sideToMove)
    {
        if ((canWin & (1 << sideToMove)) == 0)
            eval = Math.Min(eval, 0);
        if ((canWin & (1 << (sideToMove ^ 1))) == 0)
            eval = Math.Max(eval, 0);
        return eval;
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
        for (int i = 0; i < 2; i++)
            if ((canWin & (1 << i)) != 0 && IsWrongRookPawnDraw(b, e, i))
                canWin &= ~(1 << i);
        var ocb = IsOppositeBishopEnding(b, e);

        //attempt a lazy exit (on the scaled+capped material score, so a
        //side that cannot win never lazy-exits with a winning score)
        var lazy = ApplyWinnability(ocb ? eval / 2 : eval, canWin, b.SideToMove);
        if (lazy <= alpha - 300 || lazy >= beta + 300)
            return lazy;

        e.PawnScore = EvaluatePawns(b);
        eval += e.PawnScore.Eval;
        eval += EvaluateKingSafety(b, e.PawnScore, gamePhase);
        if (gamePhase != GamePhase.EndGame)
        {
            eval += EvaluateDevelopment(b);
            eval += EvaluatePieces(e);
        }
        else
        {
            //a bare king facing a PURE piece mate (no pawns anywhere): score
            //the conversion (edge-driving + king proximity) instead of the
            //static PST. With pawns on the board the win runs through
            //promotion, not cornering - the drive term then gives bad
            //guidance and, on the defending side, steers the lone king off
            //its drawing squares (opposition/blockade), so fall back to the
            //king PST there and let PawnScore drive the pawn.
            int winner = -1;
            if (b.Pawns[0] == 0 && b.Pawns[1] == 0)
            {
                if (b.PieceCount(1) == 0 && (canWin & 1) != 0)
                    winner = 0;
                else if (b.PieceCount(0) == 0 && (canWin & 2) != 0)
                    winner = 1;
            }

            if (winner >= 0)
            {
                var conv = EvaluateMateConversion(b, winner);
                eval += b.SideToMove == winner ? conv : -conv;
            }
        }

        if (ocb)
            eval /= 2;

        return ApplyWinnability(eval, canWin, b.SideToMove);
    }

    //winner-relative score for converting a won bare-king ending: drive
    //the defender to the edge (or to a corner of the bishop's color when
    //mating with B+N) and bring the winning king up close
    static int EvaluateMateConversion(Board b, int winner)
    {
        int loser = winner ^ 1;
        var wk = b.King[winner].BitScanForward();
        var lk = b.King[loser].BitScanForward();

        var score = 20 * CenterDistance[lk]
                  + 10 * (7 - SquareExtensions.KingDistance(wk, lk));

        //KBN: mate can only be delivered in a corner of the bishop's color
        if (b.Majors(winner) == 0 && b.Pawns[winner] == 0 &&
            b.Bishops[winner].Count() == 1 && b.Knights[winner].Count() == 1)
        {
            var dark = !b.Bishops[winner].BitScanForward().IsLightSquare();
            int cornerA = dark ? 7 : 0;   //h8 : a8
            int cornerB = dark ? 56 : 63; //a1 : h1
            var cornerDist = Math.Min(
                SquareExtensions.KingDistance(lk, cornerA),
                SquareExtensions.KingDistance(lk, cornerB));
            score += 20 * (7 - cornerDist);
        }

        return score;
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
                if (e.PawnScore.GetFile(side, sq.File()) == 0)
                {
                    eval[side] += RookOnOpenFileBonus / 2;
                    if (e.PawnScore.GetFile(xside, sq.File()) == 0)
                        eval[side] += RookOnOpenFileBonus;
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

            //a rook still sitting on its original corner square hasn't
            //developed yet - distinct from (and not fully covered by) the
            //open-file/7th-rank bonuses in EvaluatePieces, which just don't
            //reward it rather than actively penalizing it
            pieces = b.Rooks[side];
            while (pieces > 0)
            {
                var sq = pieces.BitScanForward();
                pieces ^= BitMask.Mask[sq];

                bool onHomeCorner = side == 0
                    ? (sq == 56 || sq == 63)  // a1, h1
                    : (sq == 0 || sq == 7);   // a8, h8
                if (onHomeCorner)
                    eval[side] += RookNotDevelopedPenalty;
            }

            pieces = b.Pawns[side];
            while (pieces > 0)
            {
                var sq = pieces.BitScanForward();
                pieces ^= BitMask.Mask[sq];
                eval[side] += PawnDevelopment[side, sq];

                //a blocked d/e pawn costs more than a blocked wing pawn - it's
                //not just a stuck pawn, it's lost central space and a cramped
                //position for whichever minor pieces wanted to develop through
                //there. This is on top of the generic BlockedPawnPenalty
                //(EvaluateBlockedPawns), which still applies regardless of
                //file - this only adds the extra central-specific cost, and
                //only during Opening/MiddleGame (EvaluateDevelopment is
                //skipped entirely in EndGame, where it matters far less).
                var file = sq.File();
                if (file == 3 || file == 4)
                {
                    if (side == 0 && sq > 7 && (b.AllPieces & BitMask.Mask[sq - 8]) > 0)
                        eval[side] += BlockedCentralPawnPenalty;
                    else if (side == 1 && sq < 56 && (b.AllPieces & BitMask.Mask[sq + 8]) > 0)
                        eval[side] += BlockedCentralPawnPenalty;
                }
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

    static int EvaluateKingSafety(Board b, PawnScore pawnScore, GamePhase gamePhase)
    {
        if (gamePhase == GamePhase.EndGame)
            return EvaluateKingEndGame(b);

        int[] eval = new int[2];

        for (int side = 0; side < 2; side++)
        {
            var xside = side ^ 1;
            var opponentHasQueen = b.Queens[xside] > 0;
            var kingSq = b.King[side].BitScanForward();
            var kingZone = MoveGenerator.KingMoves[kingSq] | b.King[side];

            if ((kingside & b.King[side]) > 0)
            { // if my king is on the kingside

                if ((b.Rooks[side] & Board.FileMask[7]) > 0) // if I have a rook in the corner
                {
                    //if the opponent has a queen and rook on the board, evaluate castle status

                    if (opponentHasQueen && b.Rooks[xside] > 0)
                        eval[side] += NotCastledPenalty;
                }
                else
                {
                    //king looks safely tucked
                    //evaluate the pawns in front of my king
                    for (int f = 5; f < 8; f++)
                    {
                        //if I have no pawns on the file
                        if (pawnScore.GetFile(side, f) == 0)
                        {
                            eval[side] += ScaleKingSafetyPenalty(OpenFileInFrontOfCastledKingPenalty, opponentHasQueen);
                        }
                        else if (pawnScore.GetFile(xside, f) == 0)
                            eval[side] += ScaleKingSafetyPenalty(SemiOpenFileInFrontOfKingPenalty, opponentHasQueen);

                    }
                }
            }
            else if ((queenside & b.King[side]) > 0)
            {
                //my king is on the queenside
                if ((b.Rooks[side] & Board.FileMask[0]) > 0) //there is a rook in the corner
                {
                    //if the opponent has a queen and rook on the board, evaluate castle status

                    if (opponentHasQueen && b.Rooks[xside] > 0)
                        eval[side] += NotCastledPenalty;
                }
                else
                {
                    //king is tucked
                    //evaluate the pawns in front of my king
                    for (int f = 2; f >= 0; f--)
                    {
                        //if I have no pawns on the file
                        if (pawnScore.GetFile(side, f) == 0)
                        {
                            eval[side] += ScaleKingSafetyPenalty(OpenFileInFrontOfCastledKingPenalty, opponentHasQueen);
                        }
                        else if (pawnScore.GetFile(xside, f) == 0)
                            eval[side] += ScaleKingSafetyPenalty(SemiOpenFileInFrontOfKingPenalty, opponentHasQueen);

                    }
                }
            }
            else
            {
                //king is in the middle of the board
                if (opponentHasQueen && b.Rooks[xside] > 0)
                {
                    eval[side] += NotCastledPenalty;
                }

                if (opponentHasQueen)
                    eval[side] += CenterKingWithEnemyQueenPenalty;
            }

            var kingZoneAttackers = CountKingZoneAttackers(b, xside, kingZone);
            eval[side] += kingZoneAttackers * ScaleKingSafetyPenalty(KingZoneAttackPenalty, opponentHasQueen);
            if (kingZoneAttackers >= 3 && opponentHasQueen)
                eval[side] += KingUnderAttack;
        }
        return eval[b.SideToMove] - eval[b.SideToMove ^ 1];
    }

    static int ScaleKingSafetyPenalty(int penalty, bool opponentHasQueen)
    {
        return opponentHasQueen ? penalty : penalty / 2;
    }

    static int CountKingZoneAttackers(Board b, int attackerSide, ulong kingZone)
    {
        int attackers = 0;
        ulong pieces;

        pieces = b.Pawns[attackerSide];
        while (pieces > 0)
        {
            int sq = pieces.BitScanForward();
            pieces ^= BitMask.Mask[sq];
            if ((MoveGenerator.PawnAttacks[attackerSide, sq] & kingZone) > 0)
                attackers++;
        }

        pieces = b.Knights[attackerSide];
        while (pieces > 0)
        {
            int sq = pieces.BitScanForward();
            pieces ^= BitMask.Mask[sq];
            if ((MoveGenerator.KnightMoves[sq] & kingZone) > 0)
                attackers++;
        }

        pieces = b.Bishops[attackerSide];
        while (pieces > 0)
        {
            int sq = pieces.BitScanForward();
            pieces ^= BitMask.Mask[sq];
            if ((MoveGenerator.BishopAttacks(b, sq) & kingZone) > 0)
                attackers++;
        }

        pieces = b.Rooks[attackerSide];
        while (pieces > 0)
        {
            int sq = pieces.BitScanForward();
            pieces ^= BitMask.Mask[sq];
            if ((MoveGenerator.RookAttacks(b, sq) & kingZone) > 0)
                attackers++;
        }

        pieces = b.Queens[attackerSide];
        while (pieces > 0)
        {
            int sq = pieces.BitScanForward();
            pieces ^= BitMask.Mask[sq];
            if ((MoveGenerator.QueenAttacks(b, sq) & kingZone) > 0)
                attackers++;
        }

        return attackers;
    }

    // Pure pawn-structure score (doubled/passed pawns, per-file occupancy) -
    // a function of pawn placement only, so it's safe to cache by
    // Board.PawnHashKey. Deliberately excludes blocked-pawn status (see
    // EvaluateBlockedPawns) since that depends on Board.AllPieces, which can
    // change without any pawn moving. Eval is White-relative (White minus
    // Black); EvaluatePawns below converts to side-to-move-relative.
    static PawnScore EvaluatePawnStructure(Board b)
    {
        int whiteEval = 0;
        int blackEval = 0;
        var returnVal = new PawnScore();
        for (int side = 0; side < 2; side++)
        {
            ulong pawns = b.Pawns[side];
            int xside = side ^ 1;
            ulong opponentPawns = b.Pawns[side ^ 1];
            int sideEval = 0;

            //evaluate file by file
            for (int i = 0; i < 8; i++)
            {

                var pawnsOnFile = pawns & Board.FileMask[i];
                returnVal.SetFile(side, i, pawnsOnFile);
                returnVal.SetFile(xside, i, opponentPawns & Board.FileMask[i]);

                //evaluate my doubled pawns
                if (pawnsOnFile.Count() > 1)
                {
                    //doubled pawns
                    if (i > 0 && i < 7) //ignoring outside pawns
                                        //check for isolated doubled pawns
                        if ((pawns & Board.FileMask[i - 1]) == 0 &&
                            (pawns & Board.FileMask[i + 1]) == 0)
                        {
                            sideEval += 4 * DoubledPawnPenalty;
                        }
                        else
                        {
                            sideEval += 2 * DoubledPawnPenalty;
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
                    sideEval +=
                        PassedPawnBonus * distanceMultiplier;
                }
            }

            if (side == 0)
                whiteEval = sideEval;
            else
                blackEval = sideEval;
        }

        returnVal.Eval = whiteEval - blackEval;
        return returnVal;
    }

    // Not cacheable: whether a pawn is blocked depends on any piece (not
    // just pawns) sitting on the square ahead of it, so this has to be
    // recomputed every call rather than reused from the pawn hash table.
    // Cheap regardless - just a per-pawn bit check. Returns a White-relative
    // (White minus Black) score, same convention as EvaluatePawnStructure.
    static int EvaluateBlockedPawns(Board b)
    {
        int whiteEval = 0;
        int blackEval = 0;
        for (int side = 0; side < 2; side++)
        {
            int sideEval = 0;
            var p = b.Pawns[side];
            while (p > 0)
            {
                var pawnsq = p.BitScanForward();
                p ^= BitMask.Mask[pawnsq];

                if (side == 0 && pawnsq > 7)
                {
                    if ((b.AllPieces & BitMask.Mask[pawnsq - 8]) > 0)
                        sideEval += BlockedPawnPenalty;
                }
                else if (side == 1 && pawnsq < 56)
                {
                    if ((b.AllPieces & BitMask.Mask[pawnsq + 8]) > 0)
                        sideEval += BlockedPawnPenalty;
                }
            }

            if (side == 0)
                whiteEval = sideEval;
            else
                blackEval = sideEval;
        }
        return whiteEval - blackEval;
    }

    static PawnScore EvaluatePawns(Board b)
    {
        if (!PawnHashTable.Instance.TryProbe(b.PawnHashKey, out var structure))
        {
            structure = EvaluatePawnStructure(b);
            PawnHashTable.Instance.Store(b.PawnHashKey, structure);
        }

        var whiteRelativeEval = structure.Eval + EvaluateBlockedPawns(b);

        // Don't mutate the cached entry: the returned struct copy carries the
        // same inline file data plus this call's side-to-move-relative eval.
        structure.Eval = b.SideToMove == 0 ? whiteRelativeEval : -whiteRelativeEval;
        return structure;
    }
}
