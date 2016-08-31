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
            PerftDivide(b,limit);
            var end = DateTime.Now;
            Console.WriteLine(String.Format("Completed in {0}ms",(end-start).TotalMilliseconds));
                        
        }

        static void PerftDivide(Board b, int depth){
            var moves = MoveGenerator.GenerateMoves(b);
            ulong total=0;
            foreach(var m in moves){
                b.MakeMove(m);
                var nodes=Perft(b,depth-1);
                total+=nodes;
                Console.WriteLine(String.Format("{0}: {1}",m,nodes));
                b.UnMakeMove();
            }
            Console.WriteLine("Total: {0}",total);
        }

        static ulong Perft(Board b, int depth){
            ulong nodes=0;
            
            if(depth == 0)
                return 1;

            var moves = MoveGenerator.GenerateMoves(b);
            var nMoves = moves.Count;
            foreach(var m in moves){
                b.MakeMove(m);
                nodes += Perft(b,depth-1);
                b.UnMakeMove();
            }
            return nodes;
        }
    }
}