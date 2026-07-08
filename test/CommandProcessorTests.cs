using System;
using System.IO;
using Xunit;
namespace mmchess.Test;

public class CommandProcessorTests
{
    // cutechess-cli treats any engine output starting with "Illegal move"
    // as a formal claim that the opponent's last move was illegal, and
    // adjudicates the game AGAINST us when the claim is false. So only
    // input shaped like a coordinate move may ever reach the MoveInput /
    // "Illegal Move" path; everything else must be a known command or the
    // xboard-spec "Error (unknown command)" reply.

    [Fact]
    public void MoveNowCommandIsNotTreatedAsMove()
    {
        // xboard "?" = move now; cutechess sends it around adjudications
        var cmd = CommandParser.ParseCommand("?");
        Assert.Equal(CommandVal.MoveNow, cmd.Value);
    }

    [Fact]
    public void DrawOfferIsNotTreatedAsMove()
    {
        var cmd = CommandParser.ParseCommand("draw");
        Assert.Equal(CommandVal.Draw, cmd.Value);
    }

    [Fact]
    public void HardAndEasyTogglePondering()
    {
        var state = new GameState { GameBoard = new Board() };

        CommandParser.DoCommand(CommandParser.ParseCommand("hard"), state);
        Assert.True(state.PonderEnabled);

        CommandParser.DoCommand(CommandParser.ParseCommand("easy"), state);
        Assert.False(state.PonderEnabled);
    }

    [Fact]
    public void MoveNowRequestsSearchStop()
    {
        var state = new GameState { GameBoard = new Board() };

        CommandParser.DoCommand(CommandParser.ParseCommand("?"), state);

        Assert.True(state.TimeUp);
    }

    [Theory]
    [InlineData("ping 1")]
    [InlineData("xyzzy")]
    [InlineData("e2e4x")] // move-ish but not a legal coordinate move shape
    public void UnrecognizedInputParsesAsUnknownNotMove(string input)
    {
        var cmd = CommandParser.ParseCommand(input);
        Assert.Equal(CommandVal.Unknown, cmd.Value);
    }

    [Theory]
    [InlineData("e2e4")]
    [InlineData("g7g8q")]
    [InlineData("e1g1")]
    public void CoordinateMovesStillParseAsMoveInput(string input)
    {
        var cmd = CommandParser.ParseCommand(input);
        Assert.Equal(CommandVal.MoveInput, cmd.Value);
    }

    [Theory]
    [InlineData("?")]
    [InlineData("draw")]
    [InlineData("ping 1")]
    public void NonMoveInputNeverPrintsIllegalMove(string input)
    {
        var state = new GameState { GameBoard = new Board() };
        var stdout = new StringWriter();
        var original = Console.Out;
        Console.SetOut(stdout);
        try
        {
            CommandParser.DoCommand(CommandParser.ParseCommand(input), state);
        }
        finally
        {
            Console.SetOut(original);
        }

        Assert.DoesNotContain("Illegal Move", stdout.ToString());
    }

    [Fact]
    public void IllegalCoordinateMoveIsRejectedNotPlayed()
    {
        // e2e5 is move-shaped but not a legal move; ParseMove used to build
        // it anyway and MakeMove trusted it, silently desyncing the board.
        var state = new GameState { GameBoard = new Board() };
        var hashBefore = state.GameBoard.HashKey;
        var stdout = new StringWriter();
        var original = Console.Out;
        Console.SetOut(stdout);
        try
        {
            CommandParser.DoCommand(CommandParser.ParseCommand("e2e5"), state);
        }
        finally
        {
            Console.SetOut(original);
        }

        Assert.Contains("Illegal Move", stdout.ToString());
        Assert.Equal(hashBefore, state.GameBoard.HashKey);
    }

    [Fact]
    public void LegalCoordinateMoveIsPlayed()
    {
        var state = new GameState { GameBoard = new Board() };
        CommandParser.DoCommand(CommandParser.ParseCommand("e2e4"), state);

        Assert.Equal(1, state.GameBoard.SideToMove);
    }

    [Fact]
    public void UnknownCommandGetsXboardErrorReply()
    {
        var state = new GameState { GameBoard = new Board() };
        var stdout = new StringWriter();
        var original = Console.Out;
        Console.SetOut(stdout);
        try
        {
            CommandParser.DoCommand(CommandParser.ParseCommand("xyzzy"), state);
        }
        finally
        {
            Console.SetOut(original);
        }

        Assert.Contains("Error (unknown command): xyzzy", stdout.ToString());
    }

    [Fact]
    public void ResultCommandParsesAsResultNotMoveInput()
    {
        var cmd = CommandParser.ParseCommand("result 1-0 {White mates}");

        Assert.Equal(CommandVal.Result, cmd.Value);
    }

    [Fact]
    public void ResultCommandStopsEngineFromMovingFurther()
    {
        // Without recognizing "result", ComputerSide is left untouched, so the
        // engine can still think it's its turn and try to emit another move
        // after the GUI already considers the game over.
        var state = new GameState { ComputerSide = 0 };
        var cmd = CommandParser.ParseCommand("result 1-0 {White mates}");

        CommandParser.DoCommand(cmd, state);

        Assert.Equal(-1, state.ComputerSide);
    }

    [Fact]
    public void MemoryCommandResizesHashTable()
    {
        try
        {
            CommandParser.DoCommand(
                CommandParser.ParseCommand("memory 32"),
                new GameState { GameBoard = new Board() });

            Assert.Equal(32, TranspositionTable.Instance.SizeInMb);
        }
        finally
        {
            TranspositionTable.SetSize(TranspositionTable.DefaultSizeMb);
        }
    }

    [Fact]
    public void SetOptionHashResizesHashTable()
    {
        try
        {
            CommandParser.DoCommand(
                CommandParser.ParseCommand("setoption name Hash value 48"),
                new GameState { GameBoard = new Board() });

            Assert.Equal(48, TranspositionTable.Instance.SizeInMb);
        }
        finally
        {
            TranspositionTable.SetSize(TranspositionTable.DefaultSizeMb);
        }
    }
}
