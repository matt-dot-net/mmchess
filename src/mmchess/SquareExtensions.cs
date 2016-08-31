using System;

namespace mmchess
{
    public static class SquareExtensions
    {
        public static int File(this int square)
        {
            return square & 7;
        }

        public static int Rank(this int square)
        {
            return square >> 3;
        }

        public static int FileDistance(int a, int b)
        {
            return Math.Abs(b.File() - a.File());
        }
    }
}