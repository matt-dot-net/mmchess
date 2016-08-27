using System;
using System.Collections.Generic;

namespace mmchess
{
    public class MoveGenerator
    {
        static readonly ulong[] KnightMoves = new ulong[64];
        static readonly ulong[] KingMoves = new ulong[64];
        static readonly ulong[,] PawnMoves = new ulong[2,64];
       

        static MoveGenerator()
        {
            InitKnightMoves();
            InitKingMoves();
            InitPawnMoves();
        }



        public static IList<Move> GenerateKingMoves(Board b, int sideToMove){
            ulong king = sideToMove == 1 ? b.BlackKing : b.WhiteKing;
            ulong sidepieces = sideToMove==1 ? b.BlackPieces : b.WhitePieces;
            var returnList = new List<Move>();
            //only one king
            int sq=king.BitScanForward();
            ulong moves = KingMoves[sq];

            while(moves > 0){
                int to = moves.BitScanForward();
                moves ^= BitMask.Mask[to];

                if ((sidepieces & BitMask.Mask[to]) > 0)
                continue;
                var m = new Move{
                    From = (byte)sq,
                    To = (byte)to,
                    Bits = (byte)((byte)MoveBits.King | (byte)sideToMove)
                };
                returnList.Add(m);
            }
            return returnList;
            
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

        static void InitPawnMoves(){
            ulong moves=0;
            //white
            for(int i=8;i<64;i++){
                moves |= BitMask.Mask[i+8];
                if(i<16)
                    moves|=BitMask.Mask[i+16];
                //captures
                if(i.File() - (i+7).File() == 1)
                    moves|=BitMask.Mask[i+7] ;
                if((i+9).File() - i.File() == 1)
                    moves|=BitMask.Mask[i+9];
                PawnMoves[0,i]=moves;
            }

             for(int i=55;i>=0;i--){
                moves |= BitMask.Mask[i-8];
                if(i>47)
                    moves|=BitMask.Mask[i-16];
                //captures
                if(i.File() - (i-8).File() == 1)
                    moves|=BitMask.Mask[i-9] ;
                if((i-7).File() - i.File() == 1)
                    moves|=BitMask.Mask[i-7];
                PawnMoves[1,i]=moves;
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