using System;

namespace mmchess{
    public enum CommandVal{
        NoOp,
        PERFT,
        Quit,
        Undo,
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
    }
}