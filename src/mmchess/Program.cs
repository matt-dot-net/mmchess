using System;

namespace mmchess{
    public class Program{
        public static void Main(string [] args)
        {
            var b = new Board();
            MoveGenerator.GeneratePawnMoves(b,0);
        }
    }
}