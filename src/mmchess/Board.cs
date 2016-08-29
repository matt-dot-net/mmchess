
using System;

namespace mmchess
{
    public class Board
    {
        public ulong WhitePawns { get; set; }
        public ulong WhiteKnights { get; set; }
        public ulong WhiteBishops { get; set; }
        public ulong WhiteRooks { get; set; }
        public ulong WhiteQueens { get; set; }
        public ulong BlackPawns { get; set; }
        public ulong BlackKnights { get; set; }
        public ulong BlackBishops { get; set; }
        public ulong BlackRooks { get; set; }
        public ulong BlackQueens { get; set; }
        public ulong BlackKing { get; set; }
        public ulong WhiteKing { get; set; }
        public ulong AllPieces { get; set; }
        public ulong AllPiecesR90{get;set;}
        public ulong AllPiecesR45{get;set;}
        public ulong AllPiecesL45{get;set;}
        public ulong WhitePieces { get; set; }
        public ulong BlackPieces { get; set; }
        public ulong EnPassant {get;set;}
        public int SideToMove{get;set;}

        public static readonly byte [] RotatedR45Map= new byte[64] {
 0,1,3,6,10,15,21,28,
 2,4,7,11,16,22,29,36,
 5,8,12,17,23,30,37,43,
 9,13,18,24,31,38,44,49,
 14,19,25,32,39,45,50,54,
 20,26,33,40,46,51,55,58,
 27,34,41,47,52,56,59,61,
 35,42,48,53,57,60,62,63
        };

        public static readonly byte[] RotatedL45Map = new byte[64] {
  28,21,15,10,6,3,1,0,
 36,29,22,16,11,7,4,2,
 43,37,30,23,17,12,8,5,
 49,44,38,31,24,18,13,9,
 54,50,45,39,32,25,19,14,
 58,55,51,46,40,33,26,20,
 61,59,56,52,47,41,34,27,
 63,62,60,57,53,48,42,35            
        };

public static readonly byte[] Rotated90Map = new byte[64]{
	0,8,16,24,32,40,48,56,
	1,9,17,25,33,41,49,57,
	2,10,18,26,34,42,50,58,
	3,11,19,27,35,43,51,59,
	4,12,20,28,36,44,52,60,
	5,13,21,29,37,45,53,61,
	6,14,22,30,38,46,54,62,
	7,15,23,31,39,47,55,63    
};

public static readonly byte[] DiagShiftsL45 = new byte[64]{
    		28,21,15,10,6,3,1,0,
		36,28,21,15,10,6,3,1,
		43,36,28,21,15,10,6,3,
		49,43,36,28,21,15,10,6,
		54,49,43,36,28,21,15,10,
		58,54,49,43,36,28,21,15,
		61,58,54,49,43,36,28,21,
		63,61,58,54,49,43,36,28
};

public static readonly byte[] DiagShiftsR45 = new byte[64]{
    0,1,3,6,10,15,21,28,
		1,3,6,10,15,21,28,36,
		3,6,10,15,21,28,36,43,
		6,10,15,21,28,36,43,49,
		10,15,21,28,36,43,49,54,
		15,21,28,36,43,49,54,58,
		21,28,36,43,49,54,58,61,
		28,36,43,49,54,58,61,63
};

public static readonly byte[] DiagAndsL45 = new byte[64]{
	255,127, 63, 31,15,7,3,1,
	127,255,127,63,31,15,7,3,
	63,127,255,127,63,31,15,7,
	31,63,127,255,127,63,31,15,
	15,31,63,127,255,127,63,31,
	7,15,31,63,127,255,127,63,
	3,7,15,31,63,127,255,127,
	1,3,7,15,31,63,127,255
};

public static readonly byte[] DiagAndsR45 = new byte[64]{
    	1,3,7,15,31,63,127,255,
	3,7,15,31,63,127,255,127,
	7,15,31,63,127,255,127,63,
	15,31,63,127,255,127,63,31,
	31,63,127,255,127,63,31,15,
	63,127,255,127,63,31,15,7,
	127,255,127,63,31,15,7,3,
	255,127, 63, 31,15,7,3,1
};

        public Board(Board b)
        {
            throw new NotImplementedException();
        }

        public Board()
        {
            Initialize();
        }

        public void Initialize()
        {
            WhitePawns = 0xff00;
            BlackPawns = 0x00ff000000000000;

            WhiteRooks |= BitMask.Mask[0] | BitMask.Mask[7];
            BlackRooks |= BitMask.Mask[63] | BitMask.Mask[56];

            WhiteKnights |= BitMask.Mask[1] | BitMask.Mask[6];
            BlackKnights |= BitMask.Mask[62] | BitMask.Mask[57];

            WhiteBishops |= BitMask.Mask[2] | BitMask.Mask[5];
            BlackBishops |= BitMask.Mask[61] | BitMask.Mask[58];

            WhiteQueens |= BitMask.Mask[3];
            BlackQueens |= BitMask.Mask[60];

            WhiteKing = BitMask.Mask[4];
            BlackKing = BitMask.Mask[59];

            WhitePieces = WhitePawns | WhiteRooks | WhiteKnights | WhiteBishops | WhiteQueens | WhiteKing;
            BlackPieces = BlackPawns | BlackRooks | BlackKnights | BlackBishops | BlackQueens | BlackKing;

            AllPieces = WhitePieces | BlackPieces;

            BuildRotatedBoards(this);
        }

        private static void BuildRotatedBoards(Board b){
            for(int i=0;i<64;i++){
                if(b.AllPieces.IsSet(i)){
                    b.AllPiecesR90 |= BitMask.Mask[Rotated90Map[i]];
                    b.AllPiecesL45 |= BitMask.Mask[RotatedL45Map[i]];
                    b.AllPiecesR45 |= BitMask.Mask[RotatedR45Map[i]];
                }
            }
        }

        public void MakeMove(Move m)
        {
            var moveMask = BitMask.Mask[m.From] | BitMask.Mask[m.To];

            if ((m.Bits & (byte)MoveBits.Black) > 0)
                UpdateBlackBoards(m, moveMask);
            else
                UpdateWhiteBoards(m, moveMask);

            AllPieces ^= moveMask;
            AllPiecesL45 ^= (BitMask.Mask[RotatedL45Map[m.From]] | BitMask.Mask[RotatedL45Map[m.To]]);
            AllPiecesR45 ^= (BitMask.Mask[RotatedR45Map[m.From]] | BitMask.Mask[RotatedR45Map[m.To]]);
            AllPiecesR90 ^= (BitMask.Mask[Rotated90Map[m.From]] | BitMask.Mask[Rotated90Map[m.To]]);

            SideToMove^=1;
        }

        public void UnMakeMove(Move m){
            throw new NotImplementedException();
        }

        private void UpdateBlackBoards(Move m, ulong moveMask)
        {
            BlackPieces ^= moveMask;
            if ((m.Bits & (byte)MoveBits.King) > 0)
                BlackKing ^= moveMask;
            if ((m.Bits & (byte)MoveBits.Queen) > 0)
                BlackQueens ^= moveMask;
            if ((m.Bits & (byte)MoveBits.Bishop) > 0)
                BlackBishops ^= moveMask;
            if ((m.Bits & (byte)MoveBits.Knight) > 0)
                BlackKnights ^= moveMask;
            if ((m.Bits & (byte)MoveBits.Pawn) > 0)
            {
                BlackPawns ^= moveMask;
                if(m.From-m.To == 16)
                    EnPassant = BitMask.Mask[m.From-8];
            }
        }

        private void UpdateWhiteBoards(Move m, ulong moveMask)
        {
            WhitePieces ^= moveMask;
            if ((m.Bits & (byte)MoveBits.King) > 0)
                WhiteKing ^= moveMask;
            if ((m.Bits & (byte)MoveBits.Queen) > 0)
                WhiteQueens ^= moveMask;
            if ((m.Bits & (byte)MoveBits.Bishop) > 0)
                WhiteBishops ^= moveMask;
            if ((m.Bits & (byte)MoveBits.Knight) > 0)
                WhiteKnights ^= moveMask;
            if ((m.Bits & (byte)MoveBits.Pawn) > 0){
                WhitePawns ^= moveMask;
                if(m.To - m.From == 16)
                    EnPassant = BitMask.Mask[m.From+8];
            }
        }
    }
}