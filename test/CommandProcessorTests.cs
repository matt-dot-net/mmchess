using System;
using System.IO;
using System.Linq;
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
    public void SetOptionPonderTogglesPondering()
    {
        var state = new GameState { GameBoard = new Board() };

        CommandParser.DoCommand(CommandParser.ParseCommand("setoption name Ponder value true"), state);
        Assert.True(state.PonderEnabled);

        CommandParser.DoCommand(CommandParser.ParseCommand("setoption name Ponder value false"), state);
        Assert.False(state.PonderEnabled);
    }

    [Fact]
    public void SetOptionThreadsReconfiguresScheduler()
    {
        var state = new GameState { GameBoard = new Board() };
        try
        {
            CommandParser.DoCommand(CommandParser.ParseCommand("setoption name Threads value 3"), state);

            Assert.Equal(3, state.ThreadCount);
            Assert.Equal(2, state.SearchScheduler.WorkerCount);

            CommandParser.DoCommand(CommandParser.ParseCommand("setoption name Threads value 1"), state);
            Assert.Equal(1, state.ThreadCount);
            Assert.False(state.SearchScheduler.IsEnabled);
        }
        finally
        {
            state.SearchScheduler.Dispose();
        }
    }

    [Fact]
    public void XBoardCoresReconfiguresScheduler()
    {
        var state = new GameState { GameBoard = new Board() };
        try
        {
            CommandParser.DoCommand(CommandParser.ParseCommand("cores 4"), state);

            Assert.Equal(4, state.ThreadCount);
            Assert.Equal(3, state.SearchScheduler.WorkerCount);
        }
        finally
        {
            state.SearchScheduler.Dispose();
        }
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

    [Fact]
    public void UciCommandEntersUciMode()
    {
        var state = new GameState { GameBoard = new Board() };
        var stdout = new StringWriter();
        var original = Console.Out;
        Console.SetOut(stdout);
        try
        {
            CommandParser.DoCommand(CommandParser.ParseCommand("uci"), state);
        }
        finally
        {
            Console.SetOut(original);
        }

        Assert.True(state.UciMode);
        Assert.Contains("uciok", stdout.ToString());
    }

    [Fact]
    public void UciCommandAdvertisesEngineIdentity()
    {
        var state = new GameState { GameBoard = new Board() };
        var stdout = new StringWriter();
        var original = Console.Out;
        Console.SetOut(stdout);
        try
        {
            CommandParser.DoCommand(CommandParser.ParseCommand("uci"), state);
        }
        finally
        {
            Console.SetOut(original);
        }

        var output = stdout.ToString();

        Assert.Contains("id name mmchess", output);
        Assert.Contains("id author Matt McKnight", output);
    }

    [Fact]
    public void UciCommandAdvertisesHashOptionBeforeUciOk()
    {
        var state = new GameState { GameBoard = new Board() };
        var stdout = new StringWriter();
        var original = Console.Out;
        Console.SetOut(stdout);
        try
        {
            CommandParser.DoCommand(CommandParser.ParseCommand("uci"), state);
        }
        finally
        {
            Console.SetOut(original);
        }

        var output = stdout.ToString();
        var hashOption = "option name Hash type spin default 512 min 1 max 4096";

        Assert.Contains(hashOption, output);
        Assert.True(
            output.IndexOf(hashOption, StringComparison.Ordinal) <
            output.IndexOf("uciok", StringComparison.Ordinal));
    }

    [Fact]
    public void UciCommandAdvertisesClearHashOptionBeforeUciOk()
    {
        var state = new GameState { GameBoard = new Board() };
        var stdout = new StringWriter();
        var original = Console.Out;
        Console.SetOut(stdout);
        try
        {
            CommandParser.DoCommand(CommandParser.ParseCommand("uci"), state);
        }
        finally
        {
            Console.SetOut(original);
        }

        var output = stdout.ToString();
        var clearHashOption = "option name Clear Hash type button";

        Assert.Contains(clearHashOption, output);
        Assert.True(
            output.IndexOf(clearHashOption, StringComparison.Ordinal) <
            output.IndexOf("uciok", StringComparison.Ordinal));
    }

    [Fact]
    public void UciCommandAdvertisesPonderOptionBeforeUciOk()
    {
        var state = new GameState { GameBoard = new Board() };
        var stdout = new StringWriter();
        var original = Console.Out;
        Console.SetOut(stdout);
        try
        {
            CommandParser.DoCommand(CommandParser.ParseCommand("uci"), state);
        }
        finally
        {
            Console.SetOut(original);
        }

        var output = stdout.ToString();
        var ponderOption = "option name Ponder type check default false";

        Assert.Contains(ponderOption, output);
        Assert.True(
            output.IndexOf(ponderOption, StringComparison.Ordinal) <
            output.IndexOf("uciok", StringComparison.Ordinal));
    }

    [Fact]
    public void UciCommandAdvertisesOnlySupportedOptions()
    {
        var state = new GameState { GameBoard = new Board() };
        var stdout = new StringWriter();
        var original = Console.Out;
        Console.SetOut(stdout);
        try
        {
            CommandParser.DoCommand(CommandParser.ParseCommand("uci"), state);
        }
        finally
        {
            Console.SetOut(original);
        }

        var optionLines = stdout.ToString()
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.StartsWith("option name ", StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(new[]
        {
            "option name Hash type spin default 512 min 1 max 4096",
            "option name Threads type spin default 1 min 1 max 256",
            "option name Clear Hash type button",
            "option name Ponder type check default false"
        }, optionLines);
    }

    [Fact]
    public void UciCommandAdvertisesThreadsOptionBeforeUciOk()
    {
        var state = new GameState { GameBoard = new Board() };
        var stdout = new StringWriter();
        var original = Console.Out;
        Console.SetOut(stdout);
        try
        {
            CommandParser.DoCommand(CommandParser.ParseCommand("uci"), state);
        }
        finally
        {
            Console.SetOut(original);
        }

        var output = stdout.ToString();
        var threadsOption = "option name Threads type spin default 1 min 1 max 256";
        Assert.Contains(threadsOption, output);
        Assert.True(output.IndexOf(threadsOption, StringComparison.Ordinal) < output.IndexOf("uciok", StringComparison.Ordinal));
    }

    [Fact]
    public void XBoardAdvertisesSmpSupport()
    {
        var state = new GameState { GameBoard = new Board() };
        var stdout = new StringWriter();
        var original = Console.Out;
        Console.SetOut(stdout);
        try
        {
            CommandParser.DoCommand(CommandParser.ParseCommand("protover 2"), state);
        }
        finally
        {
            Console.SetOut(original);
        }

        Assert.Contains("smp=1", stdout.ToString());
    }

    [Fact]
    public void SetOptionClearHashClearsMainAndPawnHashTables()
    {
        var tt = TranspositionTable.Instance;
        var pawnTable = PawnHashTable.Instance;
        var ttKey = 0x1234_5678_9abc_def0UL;
        var pawnKey = 0x0f0e_0d0c_0b0a_0908UL;

        tt.Store(ttKey, new Move(0x00001234), 2, 35, TranspositionTableEntry.EntryType.PV, 0);
        pawnTable.Store(pawnKey, new PawnScore { Eval = 17 });
        Assert.True(tt.TryProbe(ttKey, out _));
        Assert.True(pawnTable.TryProbe(pawnKey, out _));

        CommandParser.DoCommand(
            CommandParser.ParseCommand("setoption name Clear Hash"),
            new GameState { GameBoard = new Board() });

        Assert.False(tt.TryProbe(ttKey, out _));
        Assert.False(pawnTable.TryProbe(pawnKey, out _));
    }

    [Fact]
    public void PositionStartposAppliesTrailingMoves()
    {
        var state = new GameState { GameBoard = new Board() };

        CommandParser.DoCommand(
            CommandParser.ParseCommand("position startpos moves e2e4 e7e5"),
            state);

        Assert.Equal(0, state.GameBoard.SideToMove);
        Assert.Equal(2, state.GameBoard.History.Count);
    }

    [Fact]
    public void UciGoDepthRequestsSearch()
    {
        var state = new GameState { GameBoard = new Board(), UciMode = true };

        CommandParser.DoCommand(CommandParser.ParseCommand("go depth 1"), state);

        Assert.True(state.UciGoRequested);
        Assert.Equal(TimeControlType.FixedDepth, state.TimeControl.Type);
        Assert.Equal(1, state.DepthLimit);
    }

    [Fact]
    public void UciGoPonderMarksPonderSearch()
    {
        var state = new GameState { GameBoard = new Board(), UciMode = true };

        CommandParser.DoCommand(CommandParser.ParseCommand("go ponder wtime 60000 btime 60000"), state);

        Assert.True(state.UciGoRequested);
        Assert.True(state.UciPonderSearch);
        Assert.False(state.UciPonderHit);
    }

    [Fact]
    public void PonderHitMarksUciPonderHitWithoutStoppingSearch()
    {
        var state = new GameState { GameBoard = new Board(), UciPonderSearch = true };

        CommandParser.DoCommand(CommandParser.ParseCommand("ponderhit"), state);

        Assert.True(state.UciPonderHit);
        Assert.False(state.TimeUp);
    }
}
