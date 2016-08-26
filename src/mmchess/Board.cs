

using System;

namespace mmchess{
    public class Board
    {
        public ulong WhitePawns{get;set;}
        public ulong WhiteKnights{get;set;}
        public ulong WhiteBishops{get;set;}
        public ulong WhiteRooks{get;set;}
        public ulong WhiteQueens{get;set;}

        public ulong BlackPawns{get;set;}
        public ulong BlackKnights{get;set;}
        public ulong BlackBishops{get;set;}
        public ulong BlackRooks{get;set;}
        public ulong BlackQueens{get;set;}
        public ulong BlackKing{get;set;}
        public ulong WhiteKing{get;set;}

        public ulong AllPieces{get;set;}
        public ulong WhitePieces{get;set;}
        public ulong BlackPieces{get;set;}


        public Board(Board b)
        {
            throw new NotImplementedException();
        }

        public Board(){
            Initialize();
        }

        public void Initialize(){
            WhitePawns = 0xff;
            BlackPawns = 0xff00000000000000;

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
        }

        public void MakeMove(Move m)
        {
            var moveMask = BitMask.Mask[m.From] | BitMask.Mask[m.To];

            if((m.Bits & (byte)MoveBits.BlackPiece)>0)
            {
                BlackPieces ^= moveMask;
                if((m.Bits & (byte)MoveBits.King) > 0)
                    BlackKing ^=moveMask;
                if((m.Bits & (byte)MoveBits.Queen)>0)
                    BlackQueens ^= moveMask;
                if((m.Bits & (byte)MoveBits.Bishop)>0)
                    BlackBishops ^= moveMask;
                if((m.Bits & (byte)MoveBits.Knight)>0)
                    BlackKnights^=moveMask;
                if((m.Bits & (byte)MoveBits.Pawn)>0)
                    BlackPawns^=moveMask;
            }
            else{
                WhitePieces ^= moveMask;
                if((m.Bits & (byte)MoveBits.King) > 0)
                    WhiteKing ^= moveMask;
                if((m.Bits & (byte)MoveBits.Queen)>0)
                    WhiteQueens^=moveMask;
                if((m.Bits & (byte)MoveBits.Bishop)>0)
                    WhiteBishops^=moveMask;
                if((m.Bits & (byte)MoveBits.Knight)>0)
                    WhiteKnights^=moveMask;
                if((m.Bits & (byte)MoveBits.Pawn)>0)
                    WhitePawns^=moveMask;
            }

            AllPieces ^= moveMask;
        }
    }
}