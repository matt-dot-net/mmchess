using System;
using System.Threading.Tasks;

namespace mmchess
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var b = new Board();

            Command cmd = null;

            while (cmd == null || cmd.Value != CommandVal.Quit)
            {
                Console.Write("> ");
                var input = Console.ReadLine();
                cmd = CommandParser.ParseCommand(input);

                if (cmd.Value == CommandVal.PERFT)
                {
                    int depth = int.Parse(cmd.Arguments[1]);
                    PerftDivide(b, depth);
                }

                if (cmd.Value == CommandVal.MoveInput)
                {
                    var m = Move.ParseMove(b, cmd.Arguments[0]);
                    if (m == null || !b.MakeMove(m))
                    {
                        Console.WriteLine("Invalid Move");
                        continue;
                    }
                }

                if (cmd.Value == CommandVal.Undo)
                {
                    b.UnMakeMove();
                }
            }
        }

        static void PerftDivide(Board b, int depth)
        {
            var startTime = DateTime.Now;
            var moves = MoveGenerator.GenerateMoves(b);
            ulong total = 0;
            int i = 0;
            int moveCount = 0;
            foreach (var m in moves)
            {
                if (!b.MakeMove(m))
                    return;

                var nodes = Perft(b, depth - 1);

                moveCount++;
                total += nodes;

                Console.WriteLine(String.Format("{0}: {1}", moves[i], nodes));
                b.UnMakeMove();
            }
            Console.WriteLine("Moves: {0}", moveCount);
            Console.WriteLine("Total: {0}", total);
            var endTime = DateTime.Now;
            Console.WriteLine("Completed in {0}ms", (endTime - startTime).TotalMilliseconds);
        }

        static ulong Perft(Board b, int depth)
        {
            ulong nodes = 0;

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