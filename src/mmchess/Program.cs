using System;

namespace mmchess
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var gameState = new GameState();
            gameState.GameBoard = new Board();

            Command cmd = null;

            while (cmd == null || cmd.Value != CommandVal.Quit)
            {
                var input = Console.ReadLine();
                cmd = CommandParser.ParseCommand(input);
                CommandParser.DoCommand(cmd,gameState);     

                if(gameState.IsMyTurn)
                {
                    SearchRoot.Iterate(gameState,()=>{
                        if(Console.In.Peek()>0){
                            input = Console.ReadLine();
                            cmd = CommandParser.ParseCommand(input);
                            CommandParser.DoCommand(cmd,gameState);
                        }
                    });
                }
            }
        }
      
    }
}