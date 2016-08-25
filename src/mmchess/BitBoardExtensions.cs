using System;

namespace mmchess{

    public static class BitBoardExtensions {
    
    static readonly ulong debruijn64 = 0x03f79d71b4cb0a89;
    static readonly  int [] deBruijnIndex = new int[64] 
    {
         0, 47,  1, 56, 48, 27,  2, 60,
        57, 49, 41, 37, 28, 16,  3, 61,
        54, 58, 35, 52, 50, 42, 21, 44,
        38, 32, 29, 23, 17, 11,  4, 62,
        46, 55, 26, 59, 40, 36, 15, 53,
        34, 51, 20, 43, 31, 22, 10, 45,
        25, 39, 14, 33, 19, 30,  9, 24,
        13, 18,  8, 12,  7,  6,  5, 63
    };

        public static int BitScanForward(this ulong bb)
        {   
            
            return deBruijnIndex[((bb ^ (bb-1)) * debruijn64) >> 58];
        }


        public static Boolean IsSet(this ulong bitboard, int index)
        {
            return 0 < (bitboard & ((ulong)1<<index));
        }



        public static int Count(this ulong bitboard){
            return GetSetBitsCount((uint)(bitboard >> 32)) + 
                GetSetBitsCount((uint)(bitboard & 0x00000000FFFFFFFF));
        }


        private static int GetSetBitsCount(uint i)
        {
            i = i - ((i >> 1) & 0x55555555);
            i = (i & 0x33333333) + ((i >> 2) & 0x33333333);
            return (int) ((((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24);
        }

    }
}