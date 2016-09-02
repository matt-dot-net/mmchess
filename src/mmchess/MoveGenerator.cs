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

        static readonly int[,] Directions = new int[64, 64];

        static readonly byte[] diagnumsH8A1 = new byte[64]
        {
    0,1,2,3,4,5,6,7,
    1,2,3,4,5,6,7,8,
    2,3,4,5,6,7,8,9,
    3,4,5,6,7,8,9,10,
    4,5,6,7,8,9,10,11,
    5,6,7,8,9,10,11,12,
    6,7,8,9,10,11,12,13,
    7,8,9,10,11,12,13,14
        };

        static readonly byte[] diagnumsA8H1 = new byte[]
        {
     7, 6, 5, 4, 3, 2, 1, 0,
     8, 7, 6, 5, 4, 3, 2, 1,
     9, 8, 7, 6, 5, 4, 3, 2,
    10, 9, 8, 7, 6, 5, 4, 3,
    11,10, 9, 8, 7, 6, 5, 4,
    12,11,10, 9, 8, 7, 6, 5,
    13,12,11,10, 9, 8, 7, 6,
    14,13,12,11,10, 9, 8, 7
        };
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

        static ulong InterposeSquares(int check_direction, int king_square, int checking_square)
        {
            ulong target;

            switch (check_direction)
            {
                case +1:
                    target = _plus1dir[king_square - 1] ^ _plus1dir[checking_square];
                    break;
                case +7:
                    target = _plus7dir[king_square - 7] ^ _plus7dir[checking_square];
                    break;
                case +8:
                    target = _plus8dir[king_square - 8] ^ _plus8dir[checking_square];
                    break;
                case +9:
                    target = _plus9dir[king_square - 9] ^ _plus9dir[checking_square];
                    break;
                case -1:
                    target = _minus1dir[king_square + 1] ^ _minus1dir[checking_square];
                    break;
                case -7:
                    target = _minus7dir[king_square + 7] ^ _minus7dir[checking_square];
                    break;
                case -8:
                    target = _minus8dir[king_square + 8] ^ _minus8dir[checking_square];
                    break;
                case -9:
                    target = _minus9dir[king_square + 9] ^ _minus9dir[checking_square];
                    break;
                default:
                    target = 0;
                    break;
            }
            return (target);
        }
        static MoveGenerator()
        {
            InitKnightMoves();
            InitKingMoves();
            InitPawnMoves();
            InitRankAndFileMoves();
            InitDiagMoves();
            InitDirections();
            InitInterposeSqs();
        }

        static Boolean PinnedOnKing(Board b, int sq)
        {
            int ray;
            int kingSq;
            if (b.SideToMove == 0)
            {
                kingSq = b.King[0].BitScanForward();
                ray = Directions[sq, kingSq];
                if (ray == 0)
                    return false;

                switch (Math.Abs(ray))
                {
                    case 1:
                        if ((RankAttacks(b, sq) & b.King[0]) > 0)
                            return ((RankAttacks(b, sq) & (b.Queens[1] | b.Rooks[1])) != 0);
                        else
                            return false;
                    case 7:
                        if ((DiagRAttacks(b, sq) & b.King[0]) > 0)
                            return ((DiagRAttacks(b, sq) & (b.Queens[1] | b.Bishops[1])) != 0);
                        else return false;
                    case 8:
                        if ((FileAttacks(b, sq) & b.King[0]) > 0)
                            return ((FileAttacks(b, sq) & (b.Queens[1] | b.Rooks[1])) != 0);
                        else return false;
                    case 9:
                        if ((DiagLAttacks(b, sq) & b.King[0]) > 0)
                            return ((DiagLAttacks(b, sq) & (b.Queens[1] | b.Bishops[1])) != 0);
                        else return false;
                }
            }
            else
            {
                kingSq = b.King[1].BitScanForward();
                ray = Directions[sq, kingSq];
                if (ray == 0)
                    return false;

                switch (Math.Abs(ray))
                {
                    case 1:
                        if ((RankAttacks(b, sq) & b.King[1]) > 0)
                            return ((RankAttacks(b, sq) & (b.Queens[0] | b.Rooks[0])) != 0);
                        else
                            return false;
                    case 7:
                        if ((DiagRAttacks(b, sq) & b.King[1]) > 0)
                            return ((DiagRAttacks(b, sq) & (b.Queens[0] | b.Bishops[0])) != 0);
                        else return false;
                    case 8:
                        if ((FileAttacks(b, sq) & b.King[1]) > 0)
                            return ((FileAttacks(b, sq) & (b.Queens[0] | b.Rooks[0])) != 0);
                        else return false;
                    case 9:
                        if ((DiagLAttacks(b, sq) & b.King[1]) > 0)
                            return ((DiagLAttacks(b, sq) & (b.Queens[0] | b.Bishops[0])) != 0);
                        else return false;
                }
            }
            return false;
        }

        public static ulong DiagLAttacks(Board b, int sq)
        {
            return DiagLMoves[sq, DiagAndsL45[sq] & (b.AllPiecesL45 >> DiagShiftsL45[sq])];
        }

        public static ulong DiagRAttacks(Board b, int sq)
        {
            return DiagRMoves[sq, DiagAndsR45[sq] & (b.AllPiecesR45 >> DiagShiftsR45[sq])];
        }

        public static ulong Attacks(Board b, int sq)
        {
            ulong returnVal = 0;
            if ((b.Pawns[0] | b.Pawns[1]) > 0)
                returnVal |= (PawnAttacks[0, sq] & b.Pawns[1]) | (PawnAttacks[1, sq] & b.Pawns[0]);
            if ((b.Knights[0] | b.Knights[1]) > 0)
                returnVal |= (KnightMoves[sq] & (b.Knights[0] | b.Knights[1]));
            if ((b.Bishops[0] | b.Bishops[1]) > 0)
                returnVal |= (BishopAttacks(b, sq) & (b.Bishops[0] | b.Bishops[1]));
            if ((b.Rooks[0] | b.Rooks[1]) > 0)
                returnVal |= (RookAttacks(b, sq) & (b.Rooks[0] | b.Rooks[1]));
            if ((b.Queens[0] | b.Queens[1]) > 0)
                returnVal |= (QueenAttacks(b, sq) & (b.Queens[0] | b.Queens[1]));
            returnVal |= (KingMoves[sq] & (b.King[0] | b.King[1]));

            return returnVal;
        }

        public static ulong BishopAttacks(Board b, int sq)
        {
            return DiagLAttacks(b, sq) | DiagRAttacks(b, sq);
        }

        public static ulong RankAttacks(Board b, int sq)
        {
            int index = 0xff & (int)(b.AllPieces >> (8 * sq.Rank()));
            return RankMoves[sq, index];
        }

        public static ulong FileAttacks(Board b, int sq)
        {
            int index = 0xff & (int)(b.AllPiecesR90 >> (8 * sq.File()));
            return FileMoves[sq, index];
        }

        public static ulong RookAttacks(Board b, int sq)
        {
            return RankAttacks(b, sq) | FileAttacks(b, sq);
        }

        public static ulong QueenAttacks(Board b, int sq)
        {
            return BishopAttacks(b, sq) | RookAttacks(b, sq);
        }

        public static void GenerateEvasions(Board b, IList<Move> list)
        {

            var xSideToMove = b.SideToMove ^ 1;
            var kingSq = b.King[b.SideToMove].BitScanForward();
            var xkingSq = b.King[xSideToMove].BitScanForward();
            var attackers = Attacks(b, kingSq) & b.Pieces[xSideToMove];
            var checkers = attackers.Count();
            int check_direction1 = 0, check_direction2 = 0;
            int checking_sq;
            ulong target;

            if (checkers == 1)
            {
                checking_sq = attackers.BitScanForward();
                if ((BitMask.Mask[checking_sq] & b.Pawns[xSideToMove]) == 0)
                    check_direction1 = Directions[checking_sq, kingSq];
                target = InterposeSquares(check_direction1, kingSq, checking_sq);
                target |= attackers;
                target |= BitMask.Mask[xkingSq];
            }
            else
            {
                target = BitMask.Mask[xkingSq];
                checking_sq = attackers.BitScanForward();
                if ((BitMask.Mask[checking_sq] & b.Pawns[xSideToMove]) == 0)
                    check_direction1 = Directions[checking_sq, kingSq];
                attackers ^= BitMask.Mask[checking_sq];
                checking_sq = attackers.BitScanForward();
                if ((BitMask.Mask[checking_sq] & b.Pawns[xSideToMove]) == 0)
                    check_direction2 = Directions[checking_sq, kingSq];
            }

            var fromi = kingSq;
            ulong moves = MoveGenerator.KingMoves[fromi] & ~b.Pieces[b.SideToMove];
            while (moves > 0)
            {
                var toi = moves.BitScanForward();
                moves ^= BitMask.Mask[toi];
                var capture = BitMask.Mask[toi] & b.Pieces[xSideToMove];
                if ((Attacks(b, toi) & b.Pieces[xSideToMove]) == 0 && (Directions[fromi, toi] != check_direction1) && (Directions[fromi, toi] != check_direction2))
                {
                    list.Add(new Move()
                    {
                        From = (byte)fromi,
                        To = (byte)toi,
                        Bits = (byte)((byte)MoveBits.King | (capture > 0 ? (byte)MoveBits.Capture : (byte)0)),
                    });
                }
            }


            if (checkers == 0)
                return;

            var pieces = b.Knights[b.SideToMove];
            while (pieces > 0)
            {
                fromi = pieces.BitScanForward();
                pieces ^= BitMask.Mask[fromi];

                if (PinnedOnKing(b, fromi))
                    continue;

                moves = KnightMoves[fromi] & target;
                while (moves > 0)
                {
                    var toi = moves.BitScanForward();
                    moves ^= BitMask.Mask[toi];
                    var capture = BitMask.Mask[toi] & b.Pieces[xSideToMove];

                    list.Add(new Move()
                    {
                        From = (byte)fromi,
                        To = (byte)toi,
                        Bits = (byte)((byte)MoveBits.Knight | (capture > 0 ? (byte)MoveBits.Capture : (byte)0)),
                    });
                }


            }

            pieces = b.Bishops[b.SideToMove];
            while (pieces > 0)
            {
                fromi = pieces.BitScanForward();
                pieces ^= BitMask.Mask[fromi];

                if (PinnedOnKing(b, fromi))
                    continue;

                moves = BishopAttacks(b, fromi) & target;
                while (moves > 0)
                {
                    var toi = moves.BitScanForward();
                    moves ^= BitMask.Mask[toi];
                    var capture = BitMask.Mask[toi] & b.Pieces[xSideToMove];

                    list.Add(new Move()
                    {
                        From = (byte)fromi,
                        To = (byte)toi,
                        Bits = (byte)((byte)MoveBits.Bishop | (capture > 0 ? (byte)MoveBits.Capture : (byte)0)),
                    });
                }

            }

            pieces = b.Rooks[b.SideToMove];
            while (pieces > 0)
            {
                fromi = pieces.BitScanForward();
                pieces ^= BitMask.Mask[fromi];

                if (PinnedOnKing(b, fromi))
                    continue;

                moves = RookAttacks(b, fromi) & target;
                while (moves > 0)
                {
                    var toi = moves.BitScanForward();
                    moves ^= BitMask.Mask[toi];
                    var capture = BitMask.Mask[toi] & b.Pieces[xSideToMove];

                    list.Add(new Move()
                    {
                        From = (byte)fromi,
                        To = (byte)toi,
                        Bits = (byte)((byte)MoveBits.Rook | (capture > 0 ? (byte)MoveBits.Capture : (byte)0)),
                    });
                }

            }

            pieces = b.Queens[b.SideToMove];
            while (pieces > 0)
            {
                fromi = pieces.BitScanForward();
                pieces ^= BitMask.Mask[fromi];

                if (PinnedOnKing(b, fromi))
                    continue;

                moves = QueenAttacks(b, fromi) & target;
                while (moves > 0)
                {
                    var toi = moves.BitScanForward();
                    moves ^= BitMask.Mask[toi];
                    var capture = BitMask.Mask[toi] & b.Pieces[xSideToMove];

                    list.Add(new Move()
                    {
                        From = (byte)fromi,
                        To = (byte)toi,
                        Bits = (byte)((byte)MoveBits.Queen | (capture > 0 ? (byte)MoveBits.Capture : (byte)0)),
                    });
                }

            }

            //pawns are different because of the way it moves when it captures
            var empty = ~b.AllPieces;
            var targetp = target & empty;
            if (b.SideToMove == 0)
            {
                var padvances1 = b.Pawns[0] >> 8 & targetp;
                var padvances1_all = b.Pawns[0] >> 8 & empty;
                var padvances2 = (padvances1_all >> 8) & empty & targetp;
                var pvictims = ((b.Pawns[0] & ~Board.FileMask[0]) >> 9) & b.Pieces[1] & target;
                pvictims |= ((b.Pawns[0] & ~Board.FileMask[7]) >> 7) & b.Pieces[1] & target;

                if (b.EnPassant > 0)
                {
                    var epSq = b.EnPassant.BitScanForward();
                    pvictims |= (BitMask.Mask[epSq + 8] & b.AllPieces & target) > 0 ? b.EnPassant : 0;
                }

                while (padvances2 > 0)
                {
                    var toi = padvances2.BitScanForward();
                    padvances2 ^= BitMask.Mask[toi];
                    var capture = BitMask.Mask[toi] & b.Pieces[xSideToMove];

                    if (toi.Rank() < 4)
                        continue;
                    if (PinnedOnKing(b, toi + 16))
                        continue;

                    list.Add(new Move()
                    {
                        From = (byte)(toi + 16),
                        To = (byte)toi,
                        Bits = (byte)MoveBits.Pawn
                    });
                }
                while (padvances1 > 0)
                {
                    var toi = padvances1.BitScanForward();
                    padvances1 ^= BitMask.Mask[toi];

                    if (toi.Rank() < 4)
                        continue;
                    if (PinnedOnKing(b, toi + 8))
                        continue;
                    var newMove = new Move()
                    {
                        From = (byte)(toi + 8),
                        To = (byte)toi,
                        Bits = (byte)MoveBits.Pawn
                    };
                    if (toi < 8)
                        GeneratePromotions(list, newMove);
                    else
                        list.Add(newMove);
                }
                while (pvictims > 0)
                {
                    var toi = pvictims.BitScanForward();
                    pvictims ^= BitMask.Mask[toi];
                    if ((BitMask.Mask[toi + 7] & b.Pawns[0]) > 0 &&
                        !PinnedOnKing(b, toi + 7) && SquareExtensions.FileDistance(toi, toi + 7) == 1)
                    {
                        if (toi < 8)

                            GeneratePromotions(list, new Move { From = (byte)(toi + 7), To = (byte)toi });
                        else
                        {
                            list.Add(new Move
                            {
                                From = (byte)(toi + 7),
                                To = (byte)toi,
                                Bits = (byte)(MoveBits.Pawn | MoveBits.Capture),
                            });

                        }
                    }
                    if ((BitMask.Mask[toi + 9] & b.Pawns[0]) > 0 &&
                        !PinnedOnKing(b, toi + 9) && SquareExtensions.FileDistance(toi, toi + 9) == 1)
                    {
                        if (toi < 8)
                            GeneratePromotions(list, new Move
                            {
                                From = (byte)(toi + 9),
                                To = (byte)toi,

                            });

                        else
                        {

                            list.Add(new Move
                            {
                                From = (byte)(toi + 9),
                                To = (byte)toi,
                                Bits = (byte)(MoveBits.Pawn | MoveBits.Capture),
                            });
                        }
                    }

                }

            }
            else
            {
                var padvances1 = b.Pawns[1] << 8 & targetp;
                var padvances1_all = b.Pawns[1] << 8 & empty;
                var padvances2 = (padvances1_all << 8) & empty & targetp;
                var pvictims = ((b.Pawns[1] & ~Board.FileMask[7]) << 9) & b.Pieces[0] & target;
                pvictims |= ((b.Pawns[1] & ~Board.FileMask[0]) << 7) & b.Pieces[0] & target;

                if (b.EnPassant > 0)
                {
                    var epSq = b.EnPassant.BitScanForward();
                    pvictims |= (BitMask.Mask[epSq - 8] & b.AllPieces & target) > 0 ? b.EnPassant : 0;
                }

                while (padvances2 > 0)
                {
                    var toi = padvances2.BitScanForward();
                    padvances2 ^= BitMask.Mask[toi];

                    if (toi.Rank() > 3)
                        continue;
                    if (PinnedOnKing(b, toi - 16))
                        continue;

                    list.Add(new Move()
                    {
                        From = (byte)(toi - 16),
                        To = (byte)toi,
                        Bits = (byte)MoveBits.Pawn
                    });
                }
                while (padvances1 > 0)
                {
                    var toi = padvances1.BitScanForward();
                    padvances1 ^= BitMask.Mask[toi];

                    if (PinnedOnKing(b, toi - 8))
                        continue;
                    var newMove = new Move()
                    {
                        From = (byte)(toi - 8),
                        To = (byte)toi,
                        Bits = (byte)MoveBits.Pawn
                    };
                    if (toi > 55)
                        GeneratePromotions(list, newMove);
                    else
                        list.Add(newMove);
                }
                while (pvictims > 0)
                {
                    var toi = pvictims.BitScanForward();
                    pvictims ^= BitMask.Mask[toi];

                    var capture = BitMask.Mask[toi] & (b.Pieces[xSideToMove] | b.EnPassant);

                    if ((BitMask.Mask[toi - 7] & b.Pawns[1]) > 0 &&
                        !PinnedOnKing(b, toi - 7) && SquareExtensions.FileDistance(toi, toi - 7) == 1)
                    {
                        if (toi > 55)

                            GeneratePromotions(list, new Move { From = (byte)(toi - 7), To = (byte)toi });
                        else
                        {

                            list.Add(new Move
                            {
                                From = (byte)(toi + 7),
                                To = (byte)toi,
                                Bits = (byte)((byte)MoveBits.Pawn | (capture > 0 ? (byte)MoveBits.Capture : (byte)0)),
                            });

                        }
                    }
                    if ((BitMask.Mask[toi - 9] & b.Pawns[1]) > 0 &&
                        !PinnedOnKing(b, toi + 9) && SquareExtensions.FileDistance(toi, toi - 9) == 1)
                    {
                        if (toi < 8)
                            GeneratePromotions(list, new Move
                            {
                                From = (byte)(toi - 9),
                                To = (byte)toi,

                            });

                        else
                        {
                            list.Add(new Move
                            {
                                From = (byte)(toi - 9),
                                To = (byte)toi,
                                Bits = (byte)((byte)MoveBits.Pawn | (capture > 0 ? (byte)MoveBits.Capture : (byte)0)),
                            });
                        }
                    }

                }
            }
        }

        public static IList<Move> GenerateMoves(Board b)
        {

            List<Move> list = new List<Move>();

            if (b.InCheck(b.SideToMove))
            {
                GenerateEvasions(b, list);
                return list;
            }

            GenerateQueenMoves(b, list);
            GenerateRookMoves(b, list);
            GenerateBishopMoves(b, list);
            GenerateKnightMoves(b, list);
            GeneratePawnMoves(b, list);
            GenerateKingMoves(b, list);

            return list;
        }

        static void InitDirections()
        {
            for (int i = 0; i < 64; i++)
            {
                for (int j = 0; j < 64; j++)
                {
                    if (i.Rank() == j.Rank())
                    {
                        if (i < j)
                            Directions[i, j] = 1;
                        else if (i > j)
                            Directions[i, j] = -1;
                    }
                    if (i.File() == j.File())
                    {
                        if (i < j)
                            Directions[i, j] = 8;
                        else if (i > j)
                            Directions[i, j] = -8;
                    }

                    if ((diagnumsH8A1[j] == diagnumsH8A1[i]) && j < i)
                        _minus7dir[i] |= BitMask.Mask[j];

                    if ((diagnumsH8A1[j] == diagnumsH8A1[i]) && j > i)
                        _plus7dir[i] |= BitMask.Mask[j];

                    if ((diagnumsA8H1[j] == diagnumsA8H1[i]) && j < i)
                        _minus9dir[i] |= BitMask.Mask[j];

                    if ((diagnumsA8H1[j] == diagnumsA8H1[i]) && j > i)
                        _plus9dir[i] |= BitMask.Mask[j];

                    if (diagnumsA8H1[i] == diagnumsA8H1[j])
                    {
                        if (i < j)
                            Directions[i, j] = 9;
                        else if (i > j)
                            Directions[i, j] = -9;
                    }
                    if (diagnumsH8A1[i] == diagnumsH8A1[j])
                    {
                        if (i < j)
                            Directions[i, j] = 7;
                        else if (i > j)
                            Directions[i, j] = -7;
                    }
                }
            }
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
                    var capture = (BitMask.Mask[toSq] & b.AllPieces) > 0;
                    list.Add(new Move
                    {
                        From = (byte)sq,
                        To = (byte)toSq,
                        Bits = (byte)((byte)which | (capture ? (byte)MoveBits.Capture : 0))
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
                    var capture = (BitMask.Mask[toSq] & b.AllPieces) > 0;
                    list.Add(new Move
                    {
                        From = (byte)sq,
                        To = (byte)toSq,
                        Bits = (byte)((byte)which | (capture ? (byte)MoveBits.Capture : 0))
                    });
                }
            }
        }

        static void GeneratePawnMoves(Board b, IList<Move> list)
        {
            ulong pawns = b.Pawns[b.SideToMove];
            ulong enemyPieces = b.Pieces[b.SideToMove ^ 1];
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
                    var capture = (BitMask.Mask[to] & (b.AllPieces | b.EnPassant)) > 0;
                    var m = new Move
                    {
                        From = (byte)sq,
                        To = (byte)to,
                        Bits = (byte)((byte)MoveBits.Pawn | (byte)(capture ? (byte)MoveBits.Capture : (byte)0))
                    };
                    var rank = to.Rank();
                    if (rank == 7 || rank == 0) //promotion
                    {
                        GeneratePromotions(list, m);
                    }
                    else
                    {
                        list.Add(m);
                    }
                }
            }
        }

        private static void GeneratePromotions(IList<Move> list, Move m)
        {
            for (int i = 0; i < 4; i++)
            {
                var promoMove = new Move(m);
                promoMove.Promotion = (byte)i;
                list.Add(promoMove);
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

                var capture = (BitMask.Mask[to] & b.AllPieces) > 0;
                var m = new Move
                {
                    From = (byte)sq,
                    To = (byte)to,
                    Bits = (byte)((byte)MoveBits.King | (capture ? (byte)MoveBits.Capture : 0))

                };
                list.Add(m);
            }

            //generate castling moves
            var bits = (b.CastleStatus & (b.SideToMove * 2));
            if(bits > 0)
                GenerateCastleMoves(b, list, sq, bits);
        }

        private static void GenerateCastleMoves(Board b, IList<Move> list, int sq, int bits)
        {
            if ((bits & 1) > 0)
            {
                //kingside
                //see if intermediate squares are blocked
                if (b.SideToMove == 0)
                {
                    if (!((((BitMask.Mask[61] | BitMask.Mask[62]) & b.AllPieces) > 0) ||
                        ((Attacks(b, 61) | Attacks(b, 62)) & b.Pieces[b.SideToMove ^ 1]) > 0))
                    {
                        list.Add(new Move
                        {
                            From = (byte)sq,
                            To = (byte)62,
                            Bits = (byte)MoveBits.King
                        });
                    }
                }
                else
                {
                    if (!((((BitMask.Mask[05] | BitMask.Mask[06]) & b.AllPieces) > 0) ||
                    ((Attacks(b, 05) | Attacks(b, 06)) & b.Pieces[b.SideToMove ^ 1]) > 0))
                    {
                        list.Add(new Move
                        {
                            From = (byte)sq,
                            To = (byte)06,
                            Bits = (byte)MoveBits.King
                        });
                    }
                }


            }
            else if ((bits & 2) > 0)
            {
                //see if intermediate squares are blocked
                if (b.SideToMove == 0)
                {
                    if (!((((BitMask.Mask[59] | BitMask.Mask[58] | BitMask.Mask[57]) & b.AllPieces) > 0) ||
                        ((Attacks(b, 59) | Attacks(b, 58) | Attacks(b, 57)) & b.Pieces[b.SideToMove ^ 1]) > 0))
                    {
                        list.Add(new Move
                        {
                            From = (byte)sq,
                            To = (byte)58,
                            Bits = (byte)MoveBits.King
                        });
                    }
                }
                else
                {
                    if (!((((BitMask.Mask[05] | BitMask.Mask[06]) & b.AllPieces) > 0) ||
                    ((Attacks(b, 05) | Attacks(b, 06) & b.Pieces[b.SideToMove ^ 1]) > 0)))
                    {
                        list.Add(new Move
                        {
                            From = (byte)sq,
                            To = (byte)02,
                            Bits = (byte)MoveBits.King
                        });
                    }
                }



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
                    var capture = (BitMask.Mask[to] & b.AllPieces) > 0;
                    var m = new Move
                    {
                        From = (byte)sq,
                        To = (byte)to,
                        Bits = (byte)((byte)MoveBits.Knight | (capture ? (byte)MoveBits.Capture : 0))
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
                for (int j = 0; j < 256; j++)
                {
                    int diff=1;
                    for (int x = (i % 8) - 1; x >= 0; x--,diff++)
                    {
                        RankMoves[i, j] |= BitMask.Mask[i - diff];
                        if (((1 << x) & j) != 0)
                            break; //found a piece, we'll "capture" it but stop sliding
                    }
                    diff=1;
                    for (int x = (i % 8) + 1; x < 8; x++,diff++)
                    {
                        RankMoves[i, j] |= BitMask.Mask[i + diff];
                        if (((1 << x) & j) != 0)
                            break; //found a piece, we'll "capture" it but stop sliding
                    }
                }
                //foreach possible file setup
                for (int j = 0; j < 256; j++)
                {

                    int diff=1;
                    for (int x = (i >> 3) - 1; x >= 0; x--,diff++)
                    {
                        FileMoves[i, j] |= BitMask.Mask[i-(8*diff)];
                        if (((1 << x) & j) != 0)
                            break; //found a piece, we'll "capture" it but stop sliding
                    }
                    diff=1;
                    for (int x = (i >> 3) + 1; x < 8; x++,diff++)
                    {
                        FileMoves[i, j] |= BitMask.Mask[i+(8*diff)];
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
                for (int j = 0; j < 256; j++)
                {
                    if (0 == (j & DiagAndsL45[i])) //make sure we aren't beyond the length of this diag
                        continue;

                    for (int x = DiagPosL45[i] + 1; x < 8; x++)
                    {
                        if (1 << x >= DiagAndsL45[i])
                            break;
                        DiagLMoves[i, j] |= (ulong)1 << (i + (9 * (x - DiagPosL45[i])));
                        if (((1 << x) & j) != 0)
                            break; //found a piece, we'll "capture" it but stop sliding
                    }
                    for (int x = DiagPosL45[i] - 1; x >= 0; x--)
                    {
                        DiagLMoves[i, j] |= (ulong)1 << (i + (-9 * (DiagPosL45[i] - x)));
                        if (((1 << x) & j) != 0)
                            break; //found a piece, we'll "capture" it but stop sliding                        
                    }
                }

                for (int j = 0; j < 256; j++)
                {
                    if (0 == (j & DiagAndsR45[i]))
                        continue;

                    for (int x = DiagPosR45[i] + 1; x < 8; x++)
                    {
                        if (1 << x >= DiagAndsR45[i])
                            break;
                        DiagRMoves[i, j] |= (ulong)1 << (i + (7 * (x - DiagPosR45[i])));
                        if (((1 << x) & j) != 0)
                            break; //found a piece, we'll "capture" it but stop sliding                              
                    }
                    for (int x = DiagPosR45[i] - 1; x >= 0; x--)
                    {
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

        static readonly ulong[] _plus1dir = new ulong[64];
        static readonly ulong[] _plus7dir = new ulong[64];
        static readonly ulong[] _plus9dir = new ulong[64];
        static readonly ulong[] _plus8dir = new ulong[64];
        static readonly ulong[] _minus1dir = new ulong[64];
        static readonly ulong[] _minus7dir = new ulong[64];
        static readonly ulong[] _minus8dir = new ulong[64];
        static readonly ulong[] _minus9dir = new ulong[64];
        static void InitInterposeSqs()
        {
            for (int i = 0; i < 64; i++)
            {
                _plus1dir[i] = _plus7dir[i] = _plus8dir[i] = _plus9dir[i] =
                    _minus1dir[i] = _minus7dir[i] = _minus8dir[i] = _minus9dir[i] = 0;
                int j;
                for (j = i + 8; j <= 63; j += 8)
                    _plus8dir[i] |= BitMask.Mask[j];

                for (j = i - 8; j >= 0; j -= 8)
                    _minus8dir[i] |= BitMask.Mask[j];

                for (j = i + 1; j <= (i.Rank() * 8) + 7; j++)
                    _plus1dir[i] |= BitMask.Mask[j];

                for (j = i - 1; j > (i.Rank() * 8) - 1; j--)
                    _minus1dir[i] |= BitMask.Mask[j];

                for (j = 0; j < 64; j++)
                {
                    if (diagnumsA8H1[i] == diagnumsA8H1[j] && j < i)
                        _minus9dir[i] |= BitMask.Mask[j];
                    if (diagnumsA8H1[i] == diagnumsA8H1[j] && j > i)
                        _plus9dir[i] |= BitMask.Mask[j];
                    if (diagnumsH8A1[i] == diagnumsH8A1[j] && j < i)
                        _minus7dir[i] |= BitMask.Mask[j];
                    if (diagnumsH8A1[i] == diagnumsH8A1[j] && j > i)
                        _plus7dir[i] |= BitMask.Mask[j];
                }
            }
        }
    }
}