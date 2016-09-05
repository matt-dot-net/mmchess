using System;
using System.IO;

namespace mmchess{
    public enum CommandVal{
        NoOp,
        PERFT,
        Quit,
        Undo,
        Eval,
        Go,
        SetBoard,
        EpdTest,
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

        public static void DoCommand(Command cmd, ref Board b){
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
                else if (cmd.Value == CommandVal.MoveInput)
                {
                    var m = Move.ParseMove(b, cmd.Arguments[0]);
                    if (m == null || !b.MakeMove(m))
                    {
                        Console.WriteLine("Invalid Move");
                    }
                }

                else if(cmd.Value==CommandVal.Eval){
                    Console.WriteLine("Eval Score: {0}",Evaluator.Evaluate(b));
                }

                else if (cmd.Value == CommandVal.Undo)
                {
                    b.UnMakeMove();
                }            

                else if (cmd.Value == CommandVal.EpdTest)
                    EpdTest(cmd);

                else if (cmd.Value == CommandVal.SetBoard){
                    b = Board.ParseFenString(String.Join(" ",cmd.Arguments,1,cmd.Arguments.Length-1));
                }
        }

        static void EpdTest(Command cmd){
            using (var fs = new FileStream(cmd.Arguments[1],FileMode.Open)){
                var sr = new StreamReader(fs);
                String line;
                Board b;
                do{
                    line = sr.ReadLine();
                    if(String.IsNullOrEmpty(line))
                        break;
                    b = Board.ParseFenString(line);
                    DoCommand(new Command{Value=CommandVal.Go},ref b);
                }while(!String.IsNullOrEmpty(line));
            }
        }
    }
}