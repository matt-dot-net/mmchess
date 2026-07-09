using System;

namespace mmchess;

public static class Ponder
{
    public static void EnsureMoveFromTranspositionTable(GameState gameState)
    {
        if (!gameState.PonderMove.IsNull)
            return;

        if (!TranspositionTable.Instance.TryProbe(gameState.GameBoard.HashKey, out var entry) ||
            entry.Type != (byte)TranspositionTableEntry.EntryType.PV ||
            entry.MoveValue == 0)
            return;

        var move = new Move(entry.MoveValue);
        Span<Move> moveBuffer = stackalloc Move[MoveList.StackCapacity];
        var generatedMoves = new MoveList(moveBuffer);
        MoveGenerator.GenerateMoves(gameState.GameBoard, ref generatedMoves);
        for (int i = 0; i < generatedMoves.Count; i++)
        {
            var legal = generatedMoves[i];
            if (legal.Value == move.Value && gameState.GameBoard.MakeMove(legal))
            {
                gameState.GameBoard.UnMakeMove();
                gameState.PonderMove = legal;
                return;
            }
        }
    }

    public static bool ShouldPonder(GameState gameState)
    {
        return gameState.PonderEnabled &&
            gameState.ComputerSide >= 0 &&
            !gameState.IsMyTurn;
    }

    public static Command PonderUntilCommand(GameState gameState)
    {
        Command pendingCommand = null;
        var predictedMove = gameState.PonderMove;
        if (predictedMove.IsNull)
        {
            predictedMove = FindPonderMoveUntilCommand(gameState, out pendingCommand);
            if (pendingCommand != null)
                return pendingCommand;
            if (predictedMove.IsNull)
            {
                var input = ConsoleInputQueue.ReadLine();
                return input == null ? null : CommandParser.ParseCommand(input);
            }
            gameState.PonderMove = predictedMove;
        }

        gameState.PonderStartHash = gameState.GameBoard.HashKey;
        gameState.PonderMoveMade = false;

        if (ConsoleInputQueue.TryReadLine(out var readyInput))
            return CommandParser.ParseCommand(readyInput);

        if (!gameState.GameBoard.MakeMove(predictedMove))
        {
            gameState.PonderMove = Move.Null;
            gameState.PonderStartHash = 0;
            var input = ConsoleInputQueue.ReadLine();
            return input == null ? null : CommandParser.ParseCommand(input);
        }

        gameState.PonderMoveMade = true;
        if (gameState.ShowThinking)
            Console.WriteLine("Hint: {0}", MoveToCoordinateString(predictedMove));

        pendingCommand = null;
        Iterate.DoPonder(gameState, () =>
        {
            if (pendingCommand != null)
                return;

            if (ConsoleInputQueue.TryReadLine(out var input))
            {
                pendingCommand = CommandParser.ParseCommand(input);
                gameState.TimeUp = true;
            }
        });

        if (pendingCommand != null)
            return pendingCommand;

        var nextInput = ConsoleInputQueue.ReadLine();
        return nextInput == null ? null : CommandParser.ParseCommand(nextInput);
    }

    public static bool HandlePonderedCommand(Command cmd, GameState gameState)
    {
        if (!gameState.PonderMoveMade)
            return false;

        if (cmd.Value == CommandVal.MoveInput && MoveInputMatches(cmd.Arguments[0], gameState.PonderMove))
        {
            gameState.PonderHits++;
            ClearState(gameState);
            return true;
        }

        gameState.GameBoard.UnMakeMove();
        if (cmd.Value == CommandVal.MoveInput)
            gameState.PonderMisses++;
        ClearState(gameState);
        return false;
    }

    static Move FindPonderMoveUntilCommand(GameState gameState, out Command pendingCommand)
    {
        pendingCommand = null;

        if (ConsoleInputQueue.TryReadLine(out var readyInput))
        {
            pendingCommand = CommandParser.ParseCommand(readyInput);
            return Move.Null;
        }

        Command command = null;
        var move = Iterate.FindPonderMove(gameState, () =>
        {
            if (command != null)
                return;

            if (ConsoleInputQueue.TryReadLine(out var input))
            {
                command = CommandParser.ParseCommand(input);
                gameState.TimeUp = true;
            }
        });

        pendingCommand = command;
        return command == null ? move : Move.Null;
    }

    static void ClearState(GameState gameState)
    {
        gameState.PonderMove = Move.Null;
        gameState.PonderMoveMade = false;
        gameState.PonderStartHash = 0;
    }

    static bool MoveInputMatches(string input, Move move)
    {
        if (input.Length < 4)
            return false;

        if (!TrySquareIndex(input, 0, out var from) ||
            !TrySquareIndex(input, 2, out var to))
            return false;

        return from == move.From &&
            to == move.To &&
            PromotionFromInput(input) == (Piece)move.Promotion;
    }

    static bool TrySquareIndex(string input, int offset, out int square)
    {
        square = -1;
        var file = char.ToLowerInvariant(input[offset]) - 'a';
        var rank = input[offset + 1] - '1';
        if (file < 0 || file > 7 || rank < 0 || rank > 7)
            return false;

        square = (7 - rank) * 8 + file;
        return true;
    }

    static Piece PromotionFromInput(string input)
    {
        if (input.Length < 5)
            return Piece.Empty;

        switch (char.ToLowerInvariant(input[4]))
        {
            case 'q': return Piece.Queen;
            case 'r': return Piece.Rook;
            case 'b': return Piece.Bishop;
            case 'n': return Piece.Knight;
            default: return Piece.Empty;
        }
    }

    static string MoveToCoordinateString(Move move)
    {
        var text = Board.SquareNames[move.From] + Board.SquareNames[move.To];
        switch ((Piece)move.Promotion)
        {
            case Piece.Queen: return text + "q";
            case Piece.Rook: return text + "r";
            case Piece.Bishop: return text + "b";
            case Piece.Knight: return text + "n";
            default: return text;
        }
    }
}
