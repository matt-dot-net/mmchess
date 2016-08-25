

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

        public void Initialize(){
            WhitePawns = (0xff << 8);
            BlackPawns = (0xff << 48);

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
            ulong pieceboard=0,sidepieces;
            if((m.Bits & (byte)MoveBits.BlackPiece)>0)
            {
                sidepieces=BlackPieces;
                if((m.Bits & (byte)MoveBits.King) > 0)
                    pieceboard=BlackKing;
                if((m.Bits & (byte)MoveBits.Queen)>0)
                    pieceboard=BlackQueens;
                if((m.Bits & (byte)MoveBits.Bishop)>0)
                    pieceboard=BlackBishops;
                if((m.Bits & (byte)MoveBits.Knight)>0)
                    pieceboard=BlackKnights;
                if((m.Bits & (byte)MoveBits.Pawn)>0)
                    pieceboard=BlackPawns;
            }
            else{
                sidepieces=WhitePieces;
                if((m.Bits & (byte)MoveBits.King) > 0)
                    pieceboard=WhiteKing;
                if((m.Bits & (byte)MoveBits.Queen)>0)
                    pieceboard=WhiteQueens;
                if((m.Bits & (byte)MoveBits.Bishop)>0)
                    pieceboard=WhiteBishops;
                if((m.Bits & (byte)MoveBits.Knight)>0)
                    pieceboard=WhiteKnights;
                if((m.Bits & (byte)MoveBits.Pawn)>0)
                    pieceboard=WhitePawns;
            }

            var fromMask = BitMask.Mask[m.From];
            var toMask = BitMask.Mask[m.To];
            pieceboard ^= fromMask;
            pieceboard ^= toMask;
            sidepieces ^= fromMask;
            sidepieces ^= toMask; 
        }
    }
}