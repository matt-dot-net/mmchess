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
                CommandParser.DoCommand(cmd,ref b);               
            }
        }
      
    }
}