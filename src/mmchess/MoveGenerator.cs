using System;
using System.Collections.Generic;

namespace mmchess
{
    public class MoveGenerator
    {
        static readonly ulong[] KnightMoves = new ulong[64];
        static readonly ulong[] KingMoves = new ulong[64];

        static MoveGenerator()
        {
            //initialize KnightMoves
            InitKnightMoves();
            InitKingMoves();
        }

        public static IList<Move> GenerateKnightMoves(Board b, int sideToMove)
        {
            ulong knights = sideToMove == 1 ? b.BlackKnights : b.WhiteKnights;
            ulong sidepieces = sideToMove == 1 ? b.BlackPieces : b.WhitePieces;
            var returnList = new List<Move>();
            while (knights > 0)
            {
                int sq = knights.BitScanForward();
                knights ^= BitMask.Mask[sq];

                ulong moves = KnightMoves[sq];

                while (moves > 0)
                {
                    int to = moves.BitScanForward();
                    moves ^= BitMask.Mask[to];

                    if ((sidepieces & BitMask.Mask[to]) > 0)
                        continue;

                    var m = new Move
                    {
                        From = (byte)sq,
                        To = (byte)to,
                        Bits = (byte)((byte)MoveBits.Knight | sideToMove)
                    };
                    returnList.Add(m);
                }
            }
            return returnList;
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
                    if (i < 0 || i > 63)
                        continue;
                    
                    if (Math.Abs(proposed.Rank() - i.Rank()) > 2)
                        continue;

                    if (Math.Abs(proposed.File() - i.File()) > 2)
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