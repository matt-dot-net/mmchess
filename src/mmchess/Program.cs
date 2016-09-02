using System;

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
                    int depth=0;
                    bool parallel=false;
                    for(int i=1;i<cmd.Arguments.Length ;i++){
                        if(depth==0)
                            int.TryParse(cmd.Arguments[i],out depth);
                        if(cmd.Arguments[i].ToLower()=="parallel")
                            parallel=true;
                    }
                    if(parallel)
                        PerfT.PerftDivideParallel(b,depth);
                    else
                        PerfT.PerftDivide(b, depth);
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

                if(cmd.Value==CommandVal.Eval){
                    Console.WriteLine("Eval Score: {0}",Evaluator.Evaluate(b));
                }

                if (cmd.Value == CommandVal.Undo)
                {
                    b.UnMakeMove();
                }
            }
        }
      
    }
}