using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace mmchess
{
    class PerfTMove
    {
        public Move Move { get; set; }
        public int Nodes { get; set; }
    };

    public static class PerfT
    {

        public static void PerftDivide(Board b, int depth)
        {
            var startTime = DateTime.Now;
            var moves = MoveGenerator.GenerateMoves(b,true);
            int total = 0;
            int i = 0;
            int moveCount = 0;
            foreach (var m in moves)
            {
                if (!b.MakeMove(m))
                    continue;
                i++;
                var nodes = Perft(b, depth - 1);

                moveCount++;
                total += nodes;
                b.UnMakeMove();
                Console.WriteLine(String.Format("{0}: {1}", m.ToAlegbraicNotation(b), nodes));
            }
            Console.WriteLine("Moves: {0}", moveCount);
            Console.WriteLine("Total: {0}", total);
            var endTime = DateTime.Now;
            Console.WriteLine("Completed in {0}ms", (endTime - startTime).TotalMilliseconds);
        }
        public static void PerftDivideParallel(Board b, int depth)
        {
            var startTime = DateTime.Now;
            var moves = MoveGenerator.GenerateMoves(b);
            var legalMoves = new List<PerfTMove>();
            //before we parallelize, let's remove any illegal moves
            foreach (var m in moves)
            {
                if (!b.MakeMove(m))
                    continue;
                legalMoves.Add(new PerfTMove { Move = m, Nodes = 0 });
                b.UnMakeMove();
            }


            Parallel.ForEach(legalMoves, (m) =>
             {
                 Board newBoard = new Board(b);
                 if (!newBoard.MakeMove(m.Move))
                     return;

                 var nodes = Perft(newBoard, depth - 1);

                 m.Nodes += nodes;
                 newBoard.UnMakeMove();
                 Console.WriteLine(String.Format("{0}: {1}", m.Move.ToAlegbraicNotation(newBoard), nodes));
             });
            Console.WriteLine("Moves: {0}", legalMoves.Count);
            Console.WriteLine("Total: {0}", legalMoves.Sum(x => x.Nodes));
            var endTime = DateTime.Now;
            Console.WriteLine("Completed in {0}ms", (endTime - startTime).TotalMilliseconds);
        }

        static int Perft(Board b, int depth)
        {
            int nodes = 0;

            if (depth == 0)
                return 1;

            var moves = MoveGenerator.GenerateMoves(b);
            var nMoves = moves.Count;
            foreach (var m in moves)
            {
                if (b.MakeMove(m))
                {
                    nodes += Perft(b, depth - 1);
                    b.UnMakeMove();
                }
            }
            return nodes;
        }
    }
}