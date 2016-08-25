namespace mmchess{
    public class Board
    {
        public ulong WhitePawns{get;set;}
        ulong WhiteKnights{get;set;}
        ulong WhiteBishops{get;set;}
        ulong WhiteRooks{get;set;}
        ulong WhiteQueens{get;set;}

        ulong BlackPawns;
        ulong BlackKnights;
        ulong BlackBishops;
        ulong BlackRooks;
        ulong BlackQueens;
        ulong BlackKing;
        ulong WhiteKing;

        ulong _allpieces;
        ulong _wpieces;
        ulong _bpieces;


        public void Reset(){
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

            _wpieces = WhitePawns | WhiteRooks | WhiteKnights | WhiteBishops | WhiteQueens | WhiteKing;
            _bpieces = BlackPawns | BlackRooks | BlackKnights | BlackBishops | BlackQueens | BlackKing;

            _allpieces = _wpieces | _bpieces;
        }
    }
}