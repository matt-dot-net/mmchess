using System;

namespace mmchess{

    public static class BitBoardExtensions {
    

        public static Boolean IsSet(this ulong bitboard, int index)
        {
            return 0 < (bitboard & ((ulong)1<<index));
        }

    }
}