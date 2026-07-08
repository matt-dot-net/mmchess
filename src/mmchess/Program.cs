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
            cmd = ReadNextCommand(gameState);
            if (cmd == null)
                break; // stdin closed

            if (!Ponder.HandlePonderedCommand(cmd, gameState))
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

                if (!myMove.IsNull)
                {
                    Console.WriteLine("move {0}", myMove.ToAlegbraicNotation(gameState.GameBoard));
                    gameState.GameBoard.MakeMove(myMove);
                    Ponder.EnsureMoveFromTranspositionTable(gameState);
                }
            }
        }
    }

    static Command ReadNextCommand(GameState gameState)
    {
        if (Ponder.ShouldPonder(gameState))
            return Ponder.PonderUntilCommand(gameState);

        var input = ConsoleInputQueue.ReadLine();
        return input == null ? null : CommandParser.ParseCommand(input);
    }
}
