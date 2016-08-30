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
    }
}