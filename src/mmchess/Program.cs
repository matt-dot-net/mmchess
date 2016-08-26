using System;

namespace mmchess{
    public class Program{
        public static void Main(string [] args)
        {
        
            var testBoard = new Board();

            testBoard.MakeMove(new Move
             {
                 From=1,
                 To=18,
                 Bits=(byte)MoveBits.Knight
             });
        }
    }
}