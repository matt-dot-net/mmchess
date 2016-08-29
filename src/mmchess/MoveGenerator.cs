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
        public static readonly ulong [,] Sliders = new ulong[64,256];




static readonly byte [] diag_shiftsL45 = new byte[64] 

{

		28,21,15,10,6,3,1,0,

		36,28,21,15,10,6,3,1,

		43,36,28,21,15,10,6,3,

		49,43,36,28,21,15,10,6,

		54,49,43,36,28,21,15,10,

		58,54,49,43,36,28,21,15,

		61,58,54,49,43,36,28,21,

		63,61,58,54,49,43,36,28

		

};



static readonly byte[] diag_shiftsR45 = new byte[64]

{

		0,1,3,6,10,15,21,28,

		1,3,6,10,15,21,28,36,

		3,6,10,15,21,28,36,43,

		6,10,15,21,28,36,43,49,

		10,15,21,28,36,43,49,54,

		15,21,28,36,43,49,54,58,

		21,28,36,43,49,54,58,61,

		28,36,43,49,54,58,61,63

};



static readonly byte[] diag_andsL45 = new byte[64]

{

	255,127, 63, 31,15,7,3,1,

	127,255,127,63,31,15,7,3,

	63,127,255,127,63,31,15,7,

	31,63,127,255,127,63,31,15,

	15,31,63,127,255,127,63,31,

	7,15,31,63,127,255,127,63,

	3,7,15,31,63,127,255,127,

	1,3,7,15,31,63,127,255

};

static readonly byte[]diag_andsR45 = new byte[64]

{

	1,3,7,15,31,63,127,255,

	3,7,15,31,63,127,255,127,

	7,15,31,63,127,255,127,63,

	15,31,63,127,255,127,63,31,

	31,63,127,255,127,63,31,15,

	63,127,255,127,63,31,15,7,

	127,255,127,63,31,15,7,3,

	255,127, 63, 31,15,7,3,1

};

        static MoveGenerator()
        {
            InitKnightMoves();
            InitKingMoves();
            InitPawnMoves();
            InitSliders();
        }

        public static IList<Move> GenerateMoves(Board b){

            List<Move> list = new List<Move>();            
            
            GenerateQueenMoves(b,list);
            GenerateRookMoves(b,list);
            GenerateBishopMoves(b,list);
            GenerateKnightMoves(b,list);
            GeneratePawnMoves(b,list);
            GenerateKingMoves(b,list);
            
            return list;
        }

        public static void GenerateQueenMoves(Board b, IList<Move> list){
            ulong queens = b.SideToMove == 1 ? b.BlackQueens : b.WhiteQueens;
            GenerateRankAndFileMoves(b,queens,MoveBits.Queen,list);
            GenerateDiagonalMoves(b,queens,MoveBits.Queen,list);
            
        }
        public static void GenerateBishopMoves(Board b, IList<Move>list){
            ulong bishops = b.SideToMove == 1 ? b.BlackBishops : b.WhiteBishops;
            GenerateDiagonalMoves(b,bishops,MoveBits.Bishop,list);
        }
        public static void GenerateRookMoves(Board b, IList<Move> list){
            ulong rooks = b.SideToMove == 1 ? b.BlackRooks : b.WhiteRooks;

            GenerateRankAndFileMoves(b,rooks, MoveBits.Rook,list);
        }

        static void GenerateRankAndFileMoves(Board b, ulong pieces, MoveBits which, IList<Move> list){
            ulong sidePieces = b.SideToMove == 1 ? b.BlackPieces : b.WhitePieces;
            var returnVal = new List<Move>();
            while(pieces > 0){
                int sq = pieces.BitScanForward();
                pieces ^= BitMask.Mask[sq];
                ulong moves=0;

                //ranks moves require no rotation
                int index = (int)(b.AllPieces >> (8*sq.Rank())); 
                moves  |= Sliders[sq,index];
                moves &= ~sidePieces;
                while(moves>0){
                    int toSq = moves.BitScanForward();
                    moves ^= BitMask.Mask[toSq];

                    list.Add(new Move{
                        From = (byte)sq,
                        To = (byte)toSq,
                        Bits = (byte)((byte)which | (byte)b.SideToMove)
                    });
                }

                //files require 90degree rotation
                index = (int)(b.AllPiecesR90 >> (8*sq.File()));
                moves |= Sliders[sq,index];
                moves &= ~sidePieces;
                while(moves >0){
                    int toSq =moves.BitScanForward();
                    moves ^= BitMask.Mask[toSq];
                    toSq = Board.Rotated90Map[toSq];

                    list.Add(new Move{
                        From = (byte)sq,
                        To = (byte)toSq,
                        Bits = (byte)((byte)which | (byte)b.SideToMove)
                    });                    
                }

            }         
        }

        static void GenerateDiagonalMoves(Board b,ulong pieces, MoveBits which, IList<Move> list){
            ulong sidePieces = b.SideToMove == 1 ? b.BlackPieces : b.WhitePieces;
            
            while(pieces > 0){
                int sq = pieces.BitScanForward();
                pieces ^= BitMask.Mask[sq];
                
                //start with Left rotated 45
                var index = (b.AllPiecesL45 >> diag_shiftsL45[sq]) & diag_andsL45[sq];
                var moves = Sliders[sq,index];
                moves &= ~sidePieces;
                while(moves >0){
                    int toSq =moves.BitScanForward();
                    moves ^= BitMask.Mask[toSq];
                    toSq = Board.RotatedL45Map[toSq];

                    list.Add(new Move{
                        From = (byte)sq,
                        To = (byte)toSq,
                        Bits = (byte)((byte)which | (byte)b.SideToMove)
                    });                    
                }

                index = (b.AllPiecesR45 >> diag_shiftsR45[sq]) & diag_andsR45[sq];
                moves = Sliders[sq,index];
                moves &= ~sidePieces;
                while(moves >0){
                    int toSq =moves.BitScanForward();
                    moves ^= BitMask.Mask[toSq];
                    toSq = Board.RotatedR45Map[toSq];

                    list.Add(new Move{
                        From = (byte)sq,
                        To = (byte)toSq,
                        Bits = (byte)((byte)which | (byte)b.SideToMove)
                    });                    
                }    
            }
        }

        public static void GeneratePawnMoves(Board b, IList<Move> list){
            ulong pawns = b.SideToMove == 1 ? b.BlackPawns : b.WhitePawns;
            ulong enemyPieces = b.SideToMove == 1? b.WhitePieces : b.BlackPieces;
            var returnList = new List<Move>();
            while(pawns > 0){
                int sq = pawns.BitScanForward();
                pawns ^= BitMask.Mask[sq];
                
                ulong moves = PawnMoves[b.SideToMove,sq] ;
                moves &= ~b.AllPieces; //remove any moves which are blocked

                moves |= b.EnPassant | (PawnAttacks[b.SideToMove,sq] & enemyPieces); // add any captures

                while(moves > 0){
                    int to = moves.BitScanForward();
                    moves ^= BitMask.Mask[to];

                    var m = new Move {
                        From = (byte)sq,
                        To = (byte)to,
                        Bits = (byte)((byte)MoveBits.Pawn | (byte)b.SideToMove)
                    };
                    var rank = to.Rank();
                    if(rank == 7 || rank == 0 ) //promotion
                    {
                        for(int i=0;i<4;i++)
                        {
                            var promoMove = new Move(m);
                            promoMove.Promotion=(byte)i;
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

        public static void GenerateKingMoves(Board b, IList<Move> list){
            ulong king = b.SideToMove == 1 ? b.BlackKing : b.WhiteKing;
            ulong sidepieces = b.SideToMove==1 ? b.BlackPieces : b.WhitePieces;
            
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
                    Bits = (byte)((byte)MoveBits.King | (byte)b.SideToMove)
                };
                list.Add(m);
            }
        }
        public static void GenerateKnightMoves(Board b, IList<Move> list)
        {
            ulong knights = b.SideToMove == 1 ? b.BlackKnights : b.WhiteKnights;
            ulong sidepieces = b.SideToMove == 1 ? b.BlackPieces : b.WhitePieces;
            
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
                        Bits = (byte)((byte)MoveBits.Knight | b.SideToMove)
                    };
                    list.Add(m);
                }
            }
        }

        static void InitSliders(){
            //for each square on the board (having a piece) 
            for(int i=0;i<64;i++)
            {
                //and for each possible rank setup
                for(int j=1;j<256;j++){
                    if((j & (1 << (i%8))) == 0)
                        continue; // if there is no piece on this square
                    int val = 0;
                    for(int x=(i%8) - 1; x >= 0; i--)
                    {
                        var check = (1<<x) & j;
                        val |= (1<<x);
                        if(check!=0)
                            break; //found a piece, we'll "capture" it but stop sliding
                    }
                    for(int x=(i%8)+1; x< 8;x++)
                    {
                        var check = (1<<x) & j;
                        val |= (1<<x);
                        if(check!=0)
                            break; //found a piece, we'll "capture" it but stop sliding
                    }
                    Sliders[i,j] = (ulong)((ulong)val << (8*(i>>3)));    
                }
            }
        }

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