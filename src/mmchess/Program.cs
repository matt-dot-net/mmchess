using System;

namespace mmchess;

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleInputQueue.Start();

        var gameState = new GameState();
        gameState.GameBoard = new Board();

        Command cmd = null;

        while (cmd == null || cmd.Value != CommandVal.Quit)
        {
            var input = ConsoleInputQueue.ReadLine();
            if (input == null)
                break; // stdin closed

            cmd = CommandParser.ParseCommand(input);
            CommandParser.DoCommand(cmd, gameState);

            if (gameState.IsMyTurn)
            {
                var myMove = Iterate.DoIterate(gameState, () =>
                {
                    if (ConsoleInputQueue.TryReadLine(out var line))
                    {
                        cmd = CommandParser.ParseCommand(line);
                        CommandParser.DoCommand(cmd, gameState);
                    }
                });

                if (myMove != null)
                {
                    Console.WriteLine("move {0}", myMove.ToAlegbraicNotation(gameState.GameBoard));
                    gameState.GameBoard.MakeMove(myMove);
                }
            }
        }
    }



}