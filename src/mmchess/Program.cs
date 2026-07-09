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

            if (gameState.UciGoRequested)
            {
                gameState.UciGoRequested = false;
                var bestMove = Iterate.DoUciIterate(gameState, () =>
                {
                    if (ConsoleInputQueue.TryReadLine(out var line))
                    {
                        cmd = CommandParser.ParseCommand(line);
                        if (cmd.Value == CommandVal.Stop || cmd.Value == CommandVal.Quit)
                        {
                            gameState.TimeUp = true;
                            if (cmd.Value == CommandVal.Quit)
                                CommandParser.DoCommand(cmd, gameState);
                        }
                        else if (cmd.Value == CommandVal.IsReady)
                        {
                            CommandParser.DoCommand(cmd, gameState);
                        }
                        else if (cmd.Value == CommandVal.PonderHit)
                        {
                            CommandParser.DoCommand(cmd, gameState);
                        }
                    }
                });

                var bestMoveText = bestMove.IsNull ? "0000" : bestMove.ToCoordinateString();
                if (gameState.PonderEnabled && !gameState.PonderMove.IsNull)
                    Console.WriteLine("bestmove {0} ponder {1}", bestMoveText, gameState.PonderMove.ToCoordinateString());
                else
                    Console.WriteLine("bestmove {0}", bestMoveText);
                gameState.UciPonderSearch = false;
                gameState.UciPonderHit = false;
            }
            else if (!gameState.UciMode && gameState.IsMyTurn)
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
