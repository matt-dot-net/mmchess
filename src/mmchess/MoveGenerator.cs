using System;
using System.Collections.Generic;

namespace mmchess
{
    public class MoveGenerator
    {
        static readonly ulong[] KnightMoves = new ulong[64];
        static readonly ulong[] KingMoves = new ulong[64];
        static readonly ulong[,] PawnMoves = new ulong[2,64];
        static readonly ulong [,] PawnAttacks = new ulong [2,64];  
        // static readonly ulong []DiagonalsA8toH1 = new ulong[64];
        // static readonly ulong[] DiagonalsH8toA1 = new ulong[64];     

        static MoveGenerator()
        {
            InitKnightMoves();
            InitKingMoves();
            InitPawnMoves();
        }

        public static IList<Move> GeneratePawnMoves(Board b, int sideToMove){
            ulong pawns = sideToMove == 1 ? b.BlackPawns : b.WhitePawns;
            ulong enemyPieces = sideToMove == 1? b.WhitePieces : b.BlackPieces;
            var returnList = new List<Move>();
            while(pawns > 0){
                int sq = pawns.BitScanForward();
                pawns ^= BitMask.Mask[sq];
                
                ulong moves = PawnMoves[sideToMove,sq] ;
                moves &= ~b.AllPieces; //remove any moves which are blocked

                moves |= b.EnPassant | (PawnAttacks[sideToMove,sq] & enemyPieces); // add any captures

                while(moves > 0){
                    int to = moves.BitScanForward();
                    moves ^= BitMask.Mask[to];

                    var m = new Move {
                        From = (byte)sq,
                        To = (byte)to,
                        Bits = (byte)((byte)MoveBits.Pawn | (byte)sideToMove)
                    };
                    var rank = to.Rank();
                    if(rank == 7 || rank == 0 ) //promotion
                    {
                        for(int i=0;i<4;i++)
                        {
                            var promoMove = new Move(m);
                            promoMove.Promotion=(byte)i;
                            returnList.Add(promoMove);
                        }
                    }
                    else
                    {
                        returnList.Add(m);
                    }
                }
            }

            return returnList;
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

        // static void InitDiagonals(){
        //     for(int i=0;i<64;i++){
        //         DiagonalsA8toH1[i] = BitMask.Mask[i];
        //         for(int j=1;j<8;j++){
        //             int sq = (j*7)+i;
        //             if(sq.File()-i.File() == 1)
        //                 DiagonalsA8toH1[sq]
        //         }
        //     }
        // }

        static void InitPawnMoves(){

            //white
            for(int i=8;i<56;i++){
                ulong moves=0;
                ulong attacks=0;
                moves |= BitMask.Mask[i+8];
                if(i<16)
                    moves|=BitMask.Mask[i+16];
                //captures
                if(i < 57 && i.File() - (i+7).File() == 1)
                    attacks|=BitMask.Mask[i+7] ;
                if(i < 55 && (i+9).File() - i.File() == 1)
                    attacks|=BitMask.Mask[i+9];
                PawnMoves[0,i]=moves;
                PawnAttacks[0,i]=attacks;
            }
            //black
            
             for(int i=55;i>7;i--){
                ulong moves=0;
                ulong attacks=0;
                moves |= BitMask.Mask[i-8];
                if(i>47)
                    moves|=BitMask.Mask[i-16];
                //captures
                if(i > 9 && i.File() - (i-9).File() == 1)
                    attacks|=BitMask.Mask[i-9] ;
                if(i > 7 && (i-7).File() - i.File() == 1)
                    attacks|=BitMask.Mask[i-7];
                PawnMoves[1,i]=moves;
                PawnAttacks[1,i]=attacks;
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