namespace mmchess
{
    public static class SquareExtensions
    {
        public static int Rank(this int square)
        {
            return square % 8;
        }

        public static int File(this int square)
        {
            return square >> 3;
        }
    }
}