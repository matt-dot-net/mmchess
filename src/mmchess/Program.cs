using System;

namespace mmchess{
    public class Program{
        public static void Main(string [] args)
        {
            var b = new Board();
            Console.WriteLine("Enter depth");
            var limitStr = Console.ReadLine();
            int limit = int.Parse(limitStr);
            var start = DateTime.Now;
            for(int i=0;i<limit;i++){
                Console.WriteLine(String.Format("Perft ({0}): {1}",i+1,Perft(b, i)));            
            }
            var end = DateTime.Now;
            Console.WriteLine(String.Format("Completed in {0}ms",(end-start).TotalMilliseconds));
                        
        }

        static int Perft(Board b, int depth){
            
            var moves = MoveGenerator.GenerateMoves(b);
            if(depth == 0)
                return moves.Count;

            int depthTotal=0;
            foreach(var m in moves){
                b.MakeMove(m);
                depthTotal += Perft(b,depth-1);
                b.UnMakeMove();
            }
            
            return depthTotal;
            
        }
    }
}