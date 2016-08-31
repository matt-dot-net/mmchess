using System;
using System.Collections.Generic;

namespace mmchess
{
    public class MoveGenerator
    {
        public static readonly ulong[] KnightMoves = new ulong[64];
        static readonly ulong[] KingMoves = new ulong[64];
        static readonly ulong[,] PawnMoves = new ulong[2, 64];
        public static readonly ulong[,] PawnAttacks = new ulong[2, 64];
        static readonly ulong[,] RankMoves = new ulong[64, 256];
        static readonly ulong[,] FileMoves = new ulong[64, 256];
        static readonly ulong[,] DiagLMoves = new ulong[64, 256];
        static readonly ulong[,] DiagRMoves = new ulong[64, 256];

        static readonly byte[] DiagShiftsL45 = new byte[64]{
        28,21,15,10,6,3,1,0,
        36,28,21,15,10,6,3,1,
        43,36,28,21,15,10,6,3,
        49,43,36,28,21,15,10,6,
        54,49,43,36,28,21,15,10,
        58,54,49,43,36,28,21,15,
        61,58,54,49,43,36,28,21,
        63,61,58,54,49,43,36,28
};

        static readonly byte[] DiagShiftsR45 = new byte[64]{
        0,1,3,6,10,15,21,28,
        1,3,6,10,15,21,28,36,
        3,6,10,15,21,28,36,43,
        6,10,15,21,28,36,43,49,
        10,15,21,28,36,43,49,54,
        15,21,28,36,43,49,54,58,
        21,28,36,43,49,54,58,61,
        28,36,43,49,54,58,61,63
};

        static readonly byte[] DiagAndsL45 = new byte[64]{
    255,127, 63, 31,15,7,3,1,
    127,255,127,63,31,15,7,3,
    63,127,255,127,63,31,15,7,
    31,63,127,255,127,63,31,15,
    15,31,63,127,255,127,63,31,
    7,15,31,63,127,255,127,63,
    3,7,15,31,63,127,255,127,
    1,3,7,15,31,63,127,255
};

        static readonly byte[] DiagAndsR45 = new byte[64]{
    1,3,7,15,31,63,127,255,
    3,7,15,31,63,127,255,127,
    7,15,31,63,127,255,127,63,
    15,31,63,127,255,127,63,31,
    31,63,127,255,127,63,31,15,
    63,127,255,127,63,31,15,7,
    127,255,127,63,31,15,7,3,
    255,127, 63, 31,15,7,3,1
};


        static readonly byte[] DiagPosL45 = new byte[64]

        {
    0,0,0,0,0,0,0,0,
    0,1,1,1,1,1,1,1,
    0,1,2,2,2,2,2,2,
    0,1,2,3,3,3,3,3,
    0,1,2,3,4,4,4,4,
    0,1,2,3,4,5,5,5,
    0,1,2,3,4,5,6,6,
    0,1,2,3,4,5,6,7
        };


        static readonly byte[] DiagPosR45 = new byte[64]

        {
    0,0,0,0,0,0,0,0,
    1,1,1,1,1,1,1,0,
    2,2,2,2,2,2,1,0,
    3,3,3,3,3,2,1,0,
    4,4,4,4,3,2,1,0,
    5,5,5,4,3,2,1,0,
    6,6,5,4,3,2,1,0,
    7,6,5,4,3,2,1,0
        };

        static MoveGenerator()
        {
            InitKnightMoves();
            InitKingMoves();
            InitPawnMoves();
            InitRankAndFileMoves();
            InitDiagMoves();
        }

        public static ulong BishopAttacks(Board b, int sq)
        {
            return DiagLMoves[sq, DiagAndsL45[sq] & (b.AllPiecesL45 >> DiagShiftsL45[sq])] |
                   DiagRMoves[sq, DiagAndsR45[sq] & (b.AllPiecesR45 >> DiagShiftsR45[sq])];
        }

        public static ulong RookAttacks(Board b, int sq)
        {
            ulong moves=0;
            int index = 0xff & (int)(b.AllPieces >> (8 * sq.Rank()));
            moves |= RankMoves[sq, index];

            index = 0xff & (int)(b.AllPiecesR90 >> (8 * sq.File()));
            moves |= FileMoves[sq, index];
            return moves;
        }

        public static ulong QueenAttacks(Board b, int sq)
        {
            return BishopAttacks(b, sq) | RookAttacks(b, sq);
        }

        public static IList<Move> GenerateEvasions(Board b){
            throw new NotImplementedException();
        }

        public static IList<Move> GenerateMoves(Board b)
        {

            List<Move> list = new List<Move>();

            GenerateQueenMoves(b, list);
            GenerateRookMoves(b, list);
            GenerateBishopMoves(b, list);
            GenerateKnightMoves(b, list);
            GeneratePawnMoves(b, list);
            GenerateKingMoves(b, list);

            return list;
        }

        static void GenerateQueenMoves(Board b, IList<Move> list)
        {
            ulong queens = b.Queens[b.SideToMove];
            GenerateRankAndFileMoves(b, queens, MoveBits.Queen, list);
            GenerateDiagonalMoves(b, queens, MoveBits.Queen, list);

        }
        static void GenerateBishopMoves(Board b, IList<Move> list)
        {
            ulong bishops = b.Bishops[b.SideToMove];
            GenerateDiagonalMoves(b, bishops, MoveBits.Bishop, list);
        }
        static void GenerateRookMoves(Board b, IList<Move> list)
        {
            ulong rooks = b.Rooks[b.SideToMove];
            GenerateRankAndFileMoves(b, rooks, MoveBits.Rook, list);
        }

        static void GenerateRankAndFileMoves(Board b, ulong pieces, MoveBits which, IList<Move> list)
        {
            ulong sidePieces = b.Pieces[b.SideToMove];
            var returnVal = new List<Move>();
            while (pieces > 0)
            {
                int sq = pieces.BitScanForward();
                pieces ^= BitMask.Mask[sq];

                ulong moves = RookAttacks(b, sq);
                moves &= ~sidePieces;

                while (moves > 0)
                {
                    int toSq = moves.BitScanForward();
                    moves ^= BitMask.Mask[toSq];

                    list.Add(new Move
                    {
                        From = (byte)sq,
                        To = (byte)toSq,
                        Bits = (byte)((byte)which | (byte)b.SideToMove)
                    });
                }

            }
        }

        static void GenerateDiagonalMoves(Board b, ulong pieces, MoveBits which, IList<Move> list)
        {
            ulong sidePieces = b.Pieces[b.SideToMove];

            while (pieces > 0)
            {
                int sq = pieces.BitScanForward();
                pieces ^= BitMask.Mask[sq];

                var moves = BishopAttacks(b, sq);
                moves &= ~sidePieces;

                while (moves > 0)
                {
                    int toSq = moves.BitScanForward();
                    moves ^= BitMask.Mask[toSq];

                    list.Add(new Move
                    {
                        From = (byte)sq,
                        To = (byte)toSq,
                        Bits = (byte)((byte)which | (byte)b.SideToMove)
                    });
                }
            }
        }

        static void GeneratePawnMoves(Board b, IList<Move> list)
        {
            ulong pawns = b.Pawns[b.SideToMove];
            ulong enemyPieces = b.Pieces[b.SideToMove^1];
            var returnList = new List<Move>();
            while (pawns > 0)
            {
                int sq = pawns.BitScanForward();
                pawns ^= BitMask.Mask[sq];

                ulong moves = PawnMoves[b.SideToMove, sq];
                moves &= ~b.AllPieces; //remove any moves which are blocked

                moves |= (PawnAttacks[b.SideToMove, sq] & (enemyPieces | b.EnPassant)); // add any captures

                while (moves > 0)
                {
                    int to = moves.BitScanForward();
                    moves ^= BitMask.Mask[to];

                    if (Math.Abs(to - sq) == 16)
                    {
                        //make sure intermediate square isn't blocked
                        if (b.SideToMove == 0)
                        {
                            if ((b.AllPieces & BitMask.Mask[to + 8]) > 0)
                                continue;
                        }

                        else
                        {
                            if ((b.AllPieces & BitMask.Mask[to - 8]) > 0)
                                continue;
                        }
                    }
                    var m = new Move
                    {
                        From = (byte)sq,
                        To = (byte)to,
                        Bits = (byte)MoveBits.Pawn 
                    };
                    var rank = to.Rank();
                    if (rank == 7 || rank == 0) //promotion
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            var promoMove = new Move(m);
                            promoMove.Promotion = (byte)i;
                            list.Add(promoMove);
                        }
                    }
                    else
                    {
                        list.Add(m);
                    }
                }
            }
        }

        static void GenerateKingMoves(Board b, IList<Move> list)
        {
            ulong king = b.King[b.SideToMove];
            ulong sidepieces = b.Pieces[b.SideToMove];

            //only one king
            int sq = king.BitScanForward();
            ulong moves = KingMoves[sq];
            moves &= ~sidepieces;

            while (moves > 0)
            {
                int to = moves.BitScanForward();
                moves ^= BitMask.Mask[to];


                var m = new Move
                {
                    From = (byte)sq,
                    To = (byte)to,
                    Bits = (byte)((byte)MoveBits.King | (byte)b.SideToMove)
                };
                list.Add(m);
            }
        }
        static void GenerateKnightMoves(Board b, IList<Move> list)
        {
            ulong knights = b.Knights[b.SideToMove];
            ulong sidepieces = b.Pieces[b.SideToMove];

            while (knights > 0)
            {
                int sq = knights.BitScanForward();
                knights ^= BitMask.Mask[sq];

                ulong moves = KnightMoves[sq];
                moves &= ~sidepieces;

                while (moves > 0)
                {
                    int to = moves.BitScanForward();
                    moves ^= BitMask.Mask[to];

                    var m = new Move
                    {
                        From = (byte)sq,
                        To = (byte)to,
                        Bits = (byte)((byte)MoveBits.Knight | b.SideToMove)
                    };
                    list.Add(m);
                }
            }
        }

        static void InitRankAndFileMoves()
        {
            //for each square on the board (having a piece) 
            for (int i = 0; i < 64; i++)
            {
                //and for each possible rank setup
                for (int j = 1; j < 256; j++)
                {
                    if ((j & (1 << (i % 8))) == 0)
                        continue; // if there is no piece on this square
                    //int val = 0;
                    for (int x = (i % 8) - 1; x >= 0; x--)
                    {
                        RankMoves[i, j] |= (ulong)((ulong)1 << (8 * i.Rank() + x));
                        if (((1 << x) & j) != 0)
                            break; //found a piece, we'll "capture" it but stop sliding
                    }
                    for (int x = (i % 8) + 1; x < 8; x++)
                    {
                        RankMoves[i, j] |= (ulong)((ulong)1 << (8 * i.Rank() + x));
                        if (((1 << x) & j) != 0)
                            break; //found a piece, we'll "capture" it but stop sliding
                    }
                }
                //foreach possible file setup
                for (int j = 1; j < 256; j++)
                {
                    if ((j & (1 << (i >> 3))) == 0)
                        continue; // if there is no piece on this square
                    //int val = 0;
                    for (int x = (i >> 3) - 1; x >= 0; x--)
                    {
                        FileMoves[i, j] |= (ulong)((ulong)1 << (i - (8 * (7 - x))));
                        if (((1 << x) & j) != 0)
                            break; //found a piece, we'll "capture" it but stop sliding
                    }
                    for (int x = (i >> 3) + 1; x < 8; x++)
                    {
                        FileMoves[i, j] |= (ulong)((ulong)1 << (i + (8 * x)));
                        if (((1 << x) & j) != 0)
                            break; //found a piece, we'll "capture" it but stop sliding
                    }
                }
            }
        }

        static void InitDiagMoves()
        {
            for (int i = 0; i < 64; i++)
            {
                for (int j = 1; j < 256; j++)
                {
                    if (0 == (j & DiagAndsL45[i])) //make sure we aren't beyond the length of this diag
                        continue;

                    if ((j & (1 << DiagPosL45[i])) == 0)
                        continue;   //make sure this square is represented in the occupation index

                    for (int x = DiagPosL45[i] + 1; x < 8; x++)
                    {
                        if (1 << x >= DiagAndsL45[i])
                            break;
                        DiagLMoves[i, j] |= (ulong)1 << (i + (9 * (DiagPosL45[i] + x)));
                        if (((1 << x) & j) != 0)
                            break; //found a piece, we'll "capture" it but stop sliding
                    }
                    for (int x = DiagPosL45[i] - 1; x >= 0; x--)
                    {
                        if (1 << x >= DiagAndsL45[i])
                            break;
                        DiagLMoves[i, j] |= (ulong)1 << (i + (-9 * (DiagPosL45[i] - x)));
                        if (((1 << x) & j) != 0)
                            break; //found a piece, we'll "capture" it but stop sliding                        
                    }
                }

                for (int j = 1; j < 256; j++)
                {
                    if (0 == (j & DiagAndsR45[i]))
                        continue;

                    if ((j & 1 << DiagPosR45[i]) == 0)
                        continue;   //make sure this square is represented in the occupation index

                    for (int x = DiagPosR45[i] + 1; x < 8; x++)
                    {
                        if (1 << x >= DiagAndsR45[i])
                            break;
                        DiagRMoves[i, j] |= (ulong)1 << (i + (7 * (DiagPosR45[i] + x)));
                        if (((1 << x) & j) != 0)
                            break; //found a piece, we'll "capture" it but stop sliding                              
                    }
                    for (int x = DiagPosR45[i] - 1; x >= 0; x--)
                    {
                        if (1 << x >= DiagAndsR45[i])
                            break;
                        DiagRMoves[i, j] |= (ulong)1 << (i + (-7 * (DiagPosR45[i] - x)));
                        if (((1 << x) & j) != 0)
                            break; //found a piece, we'll "capture" it but stop sliding                              
                    }
                }
            }
        }

        static void InitPawnMoves()
        {

            //black
            for (int i = 8; i < 56; i++)
            {
                ulong moves = 0;
                ulong attacks = 0;
                moves |= BitMask.Mask[i + 8];
                if (i < 16)
                    moves |= BitMask.Mask[i + 16];
                //captures
                if (i < 56 && i.File() - (i + 7).File() == 1)
                    attacks |= BitMask.Mask[i + 7];
                if (i < 55 && (i + 9).File() - i.File() == 1)
                    attacks |= BitMask.Mask[i + 9];
                PawnMoves[1, i] = moves;
                PawnAttacks[1, i] = attacks;
            }

            //white
            for (int i = 55; i > 7; i--)
            {
                ulong moves = 0;
                ulong attacks = 0;
                moves |= BitMask.Mask[i - 8];
                if (i > 47)
                    moves |= BitMask.Mask[i - 16];
                //captures
                if (i > 8 && i.File() - (i - 9).File() == 1)
                    attacks |= BitMask.Mask[i - 9];
                if ((i - 7).File() - i.File() == 1)
                    attacks |= BitMask.Mask[i - 7];
                PawnMoves[0, i] = moves;
                PawnAttacks[0, i] = attacks;
            }
        }

        static void InitKingMoves()
        {
            var moves = new int[] { -9, -8, -7, -1, 1, 7, 8, 9 };
            for (int i = 0; i < 64; i++)
            {
                ulong mask = 0;
                foreach (var offset in moves)
                {
                    var proposed = i + offset;
                    if (proposed < 0 || proposed > 63)
                        continue;

                    if (Math.Abs(proposed.Rank() - i.Rank()) > 1)
                        continue;

                    if (Math.Abs(proposed.File() - i.File()) > 1)
                        continue;

                    mask |= BitMask.Mask[proposed];
                }
                KingMoves[i] = mask;
            }

        }
        static void InitKnightMoves()
        {
            var moves = new int[] { -17, -15, -10, -6, 6, 10, 15, 17 };
            for (int i = 0; i < 64; i++)
            {
                ulong mask = 0;
                foreach (var offset in moves)
                {
                    var proposed = i + offset;

                    if (proposed < 0 || proposed > 63)
                        continue;

                    if (Math.Abs(proposed.Rank() - i.Rank()) > 2)
                        continue;

                    if (Math.Abs(proposed.File() - i.File()) > 2)
                        continue;

                    mask |= BitMask.Mask[proposed];
                }
                KnightMoves[i] = mask;
            }
        }
    }
}