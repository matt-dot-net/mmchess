using System;

namespace mmchess{
    public enum CommandVal{
        NoOp,
        PERFT,
        Quit,
        Undo,
        Eval,
        Go,
        MoveInput
    }
    public class Command{
        public CommandVal Value {get;set;}
        public string[] Arguments;
        
    }

    public static class CommandParser
    {

        public static Command ParseCommand(string input){
            CommandVal cmd;
            var buffer = input.Split(' ');
            input = buffer[0];
            if(Enum.TryParse(input, true, out cmd))
                return new Command{Value=cmd,Arguments=buffer};
            return new Command{Value=CommandVal.MoveInput,Arguments=new string[]{input}};
        }

        public static void DoCommand(Command cmd, Board b){
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
                if (cmd.Value == CommandVal.Go){
                    SearchRoot.Iterate(b);
                }
                if (cmd.Value == CommandVal.MoveInput)
                {
                    var m = Move.ParseMove(b, cmd.Arguments[0]);
                    if (m == null || !b.MakeMove(m))
                    {
                        Console.WriteLine("Invalid Move");
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