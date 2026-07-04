using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace mmchess;


public enum CommandVal
{
    NoOp,
    Uci,
    xboard,
    Protover,
    Black,
    White,
    Force,
    Result,
    New,
    Random,
    IsReady,
    Post,
    NoPost,
    Hard,
    Easy,
    Position,
    PERFT,
    Quit,
    Undo,
    Remove,
    Eval,
    Time,
    Otim,
    Level,
    Go,
    SetBoard,
    EpdTest,
    MoveInput,
    Accepted,
    Rejected,
    SD,
    ST,
    Bench
}
public class Command
{
    public CommandVal Value { get; set; }
    public string[] Arguments;

}

public static class CommandParser
{
    static readonly String VersionNumber =
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
        ?? "unknown";


    public static Command ParseCommand(string input)
    {
        CommandVal cmd;
        var buffer = input.Split(' ');
        input = buffer[0];
        if(String.IsNullOrEmpty(input))
            return new Command { Value=CommandVal.NoOp};
        if (Enum.TryParse(input, true, out cmd))
            return new Command { Value = cmd, Arguments = buffer };
        return new Command { Value = CommandVal.MoveInput, Arguments = new string[] { input } };
    }

    public static void DoCommand(Command cmd, GameState state)
    {
        if (cmd.Value == CommandVal.Quit)
        {
            state.TimeUp = true;
            state.ComputerSide = -1;
        }
        if (cmd.Value == CommandVal.xboard)
        {
            state.UsingGui = true;
            Console.WriteLine();
        }
        else if (cmd.Value == CommandVal.White || cmd.Value == CommandVal.Black)
        {

        }
        else if (cmd.Value == CommandVal.Force || cmd.Value == CommandVal.Result)
        {
            //the game is over (or the GUI wants us to stop moving on our own) -
            //without this, an unrecognized "result ..." falls through to
            //MoveInput, fails to parse, and leaves ComputerSide untouched, so
            //we can end up emitting an unsolicited move after the game already ended
            state.ComputerSide = -1;
        }
        else if (cmd.Value == CommandVal.Time)
        {
            int centisecs;
            if (int.TryParse(cmd.Arguments[1], out centisecs))
                state.WinBoardUpdateMyClock(centisecs);
            else Console.WriteLine("Error: invalid time {0}", cmd.Arguments[1]);
        }
        else if (cmd.Value == CommandVal.Otim)
        {
            int centisecs;
            if (int.TryParse(cmd.Arguments[1], out centisecs))
                state.WinBoardUpdateOpponentClock(centisecs);
            else Console.WriteLine("Error: invalid time {0}", cmd.Arguments[1]);
        }
        else if (cmd.Value == CommandVal.Accepted ||
                cmd.Value == CommandVal.Rejected ||

                cmd.Value == CommandVal.Random ||
                 cmd.Value == CommandVal.Hard ||
                 cmd.Value == CommandVal.Easy)
        {
            //noop
            //we don't ponder, so hard/easy are both no-ops

        }
        else if (cmd.Value == CommandVal.Post)
        {
            state.ShowThinking = true;
        }
        else if (cmd.Value == CommandVal.NoPost)
        {
            state.ShowThinking = false;
        }
        else if (cmd.Value == CommandVal.Level)
        {
            int moves;
            if (!int.TryParse(cmd.Arguments[1], out moves))
                Console.WriteLine("Error: Invalid input {0}", cmd.Arguments[1]);

            if (moves == 0)
            {
                state.TimeControl.Type = TimeControlType.TimePerGame;

            }
            else
            {
                state.TimeControl.Type = TimeControlType.NumberOfMoves;
                state.TimeControl.MovesInTimeControl = moves;
            }

            int minutes;
            if (int.TryParse(cmd.Arguments[2], out minutes))
                state.TimeControl.GameTimeSeconds = minutes * 60;
            else
                Console.Error.WriteLine("Error: invalid input {0}", cmd.Arguments[2]);
            int increment;
            if (int.TryParse(cmd.Arguments[3], out increment))
                state.TimeControl.IncrementSeconds = increment;
            else
                Console.Error.WriteLine("Error: invalid input {0}", cmd.Arguments[3]);

        }
        else if (cmd.Value == CommandVal.SD)
        {
            state.TimeControl.Type = TimeControlType.FixedDepth;

            int d;
            if (!int.TryParse(cmd.Arguments[1], out d))
                return;
            state.DepthLimit = d;
        }
        else if (cmd.Value == CommandVal.ST)
        {
            if (cmd.Arguments.Length < 2)
            {
                Console.Error.WriteLine("Error: expected time value");
                return;
            }
            int t;
            if (!int.TryParse(cmd.Arguments[1], out t))
            {
                Console.Error.WriteLine("Error: Invalid input {0}", cmd.Arguments[1]);
                return;
            }

            state.TimeControl.Type = TimeControlType.FixedTimePerMove;

            int seconds;
            if (int.TryParse(cmd.Arguments[1], out seconds))
                state.TimeControl.FixedTimePerSearchSeconds = seconds;
            else
                Console.Error.WriteLine("Error: Invalid input {0}", cmd.Arguments[1]);
        }
        else if (cmd.Value == CommandVal.New)
        {
            state.GameBoard = new Board();
        }
        else if (cmd.Value == CommandVal.Uci)
        {
            state.UsingGui = true;
            Console.WriteLine("id name mmchess {0}", VersionNumber);
            Console.WriteLine("id author Matt McKnight");
            Console.WriteLine("uciok");
        }
        else if (cmd.Value == CommandVal.Protover)
        {
            Console.WriteLine("feature setboard=1 reuse=1 myname=\"mmchess {0}\" done=1", VersionNumber);
        }
        else if (cmd.Value == CommandVal.IsReady)
        {
            Console.WriteLine("readyok");
        }
        else if (cmd.Value == CommandVal.Position)
        {
            if (cmd.Arguments.Length < 2)
                return;
            if (cmd.Arguments[1] == "startpos")
            {
                state.GameBoard = new Board(); return;
            }

            var fenString = string.Join(' ',cmd.Arguments,1,cmd.Arguments.Length-1);
            state.GameBoard = Board.ParseFenString(fenString);
        }
        else if (cmd.Value == CommandVal.PERFT)
        {
            int depth = 0;
            bool parallel = false;
            for (int i = 1; i < cmd.Arguments.Length; i++)
            {
                if (depth == 0)
                    int.TryParse(cmd.Arguments[i], out depth);
                if (cmd.Arguments[i].ToLower() == "parallel")
                    parallel = true;
            }

            if (depth < 1)
            {
                Console.Error.WriteLine("Usage: perft <depth> [parallel]");
                Console.Error.WriteLine("  <depth>    number of plies to search (required, integer >= 1)");
                Console.Error.WriteLine("  parallel   optional - split root moves across threads");
                return;
            }

            if (parallel)
                PerfT.PerftDivideParallel(state.GameBoard, depth);
            else
                PerfT.PerftDivide(state.GameBoard, depth);
        }
        if (cmd.Value == CommandVal.Go)
        {
            state.ComputerSide = state.GameBoard.SideToMove;

        }
        else if (cmd.Value == CommandVal.MoveInput)
        {
            var m = Move.ParseMove(state.GameBoard, cmd.Arguments[0]);
            if (m == null || !state.GameBoard.MakeMove(m))
            {
                Console.WriteLine("Illegal Move: {0}", cmd.Arguments[0]);
                return;
            }
        }

        else if (cmd.Value == CommandVal.Eval)
        {
            Console.WriteLine("Eval Score: {0}", Evaluator.Evaluate(state.GameBoard,-10000,10000));
        }

        else if (cmd.Value == CommandVal.Undo ||
                cmd.Value == CommandVal.Remove)
        {
            state.GameBoard.UnMakeMove();
        }

        else if (cmd.Value == CommandVal.EpdTest)
            EpdTest(cmd);

        else if (cmd.Value == CommandVal.Bench)
            Bench(cmd);

        else if (cmd.Value == CommandVal.SetBoard)
        {
            try
            {
                state.GameBoard = Board.ParseFenString(String.Join(" ", cmd.Arguments, 1, cmd.Arguments.Length - 1));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }
    }

    // Fixed-depth, no-wall-clock move-ordering benchmark: runs every position
    // in an EPD/FEN file at the same search depth and reports aggregate
    // fail-high metrics (FirstMoveFailHigh% above all - see AlphaBeta.cs
    // Search's move loop). Unlike EpdTest, this isn't measuring "can we find
    // the tactic in N seconds" (wall-clock time makes small deltas noise -
    // depth is what's reproducible across runs/machines). Use this to A/B
    // move-ordering changes: same file, same depth, compare FirstMoveFailHigh%
    // and total node count before/after.
    static void Bench(Command cmd)
    {
        if (cmd.Arguments.Length < 2)
        {
            Console.Error.WriteLine("Usage: bench <epdfile> [depth]");
            return;
        }

        int depth = 7;
        if (cmd.Arguments.Length >= 3 && !int.TryParse(cmd.Arguments[2], out depth))
        {
            Console.Error.WriteLine("Error: invalid depth {0}", cmd.Arguments[2]);
            return;
        }

        if (!File.Exists(cmd.Arguments[1]))
        {
            Console.Error.WriteLine("Error: file not found: {0}", cmd.Arguments[1]);
            return;
        }

        var aggregate = new AlphaBetaMetrics();
        int positions = 0;
        var sw = Stopwatch.StartNew();

        var originalError = Console.Error;
        Console.SetError(TextWriter.Null); // suppress each position's per-search PrintMetrics noise
        try
        {
            using var fs = new FileStream(cmd.Arguments[1], FileMode.Open);
            var sr = new StreamReader(fs);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (String.IsNullOrWhiteSpace(line))
                    continue;

                var gameState = new GameState
                {
                    GameBoard = Board.ParseFenString(line),
                    TimeControl = new TimeControl { Type = TimeControlType.FixedDepth },
                    DepthLimit = depth
                };

                Iterate.DoIterate(gameState, () => { }, out var metrics);

                aggregate.Nodes += metrics.Nodes;
                aggregate.QNodes += metrics.QNodes;
                aggregate.FailHigh += metrics.FailHigh;
                aggregate.FirstMoveFailHigh += metrics.FirstMoveFailHigh;
                aggregate.KillerFailHigh += metrics.KillerFailHigh;
                aggregate.TTFailHigh += metrics.TTFailHigh;
                aggregate.NullMoveTries += metrics.NullMoveTries;
                aggregate.NullMoveFailHigh += metrics.NullMoveFailHigh;
                aggregate.MateThreats += metrics.MateThreats;
                aggregate.LMRResearch += metrics.LMRResearch;
                aggregate.FPrune += metrics.FPrune;
                aggregate.EFPrune += metrics.EFPrune;
                positions++;

                Console.Write('.');
                Console.Out.Flush();
            }
        }
        finally
        {
            Console.SetError(originalError);
        }

        sw.Stop();

        Console.WriteLine();
        Console.WriteLine("Bench: {0} positions at depth {1}", positions, depth);
        Console.WriteLine("Nodes={0}, QNodes={1}, Knps={2:0}",
            aggregate.Nodes, aggregate.QNodes, aggregate.Nodes / 1000.0 / sw.Elapsed.TotalSeconds);
        Console.WriteLine("FirstMoveFH%={0:0.00}, KillerFH%={1:0.00}, TTFH%={2:0.00}, FailHigh={3}",
            100.0 * aggregate.FirstMoveFailHigh / (aggregate.FailHigh + 1),
            100.0 * aggregate.KillerFailHigh / (aggregate.FailHigh + 1),
            100.0 * aggregate.TTFailHigh / (aggregate.FailHigh + 1),
            aggregate.FailHigh);
        Console.WriteLine("NullMoveTries={0}, NullMoveFH%={1:0.00}, MateThreats={2}, LMRResearch={3}, FPrune={4}, EFPrune={5}",
            aggregate.NullMoveTries,
            100.0 * aggregate.NullMoveFailHigh / (aggregate.NullMoveTries + 1),
            aggregate.MateThreats,
            aggregate.LMRResearch,
            aggregate.FPrune,
            aggregate.EFPrune);
    }

    static void EpdTest(Command cmd)
    {
        int tests = 0, successes = 0;
        if(cmd.Arguments.Length < 2)
        {
            Console.Error.WriteLine("Missing parameter filename");
            return;
        }
        try
        {

            using (var fs = new FileStream(cmd.Arguments[1], FileMode.Open))
            {
                Console.WriteLine("beginning test in 5 seconds... type 'quit' to abort");
                System.Threading.Thread.Sleep(5000);
                var sr = new StreamReader(fs);
                String line;
                bool quit = false;

                var gameState = new GameState();
                gameState.ShowThinking = true;
                gameState.TimeControl = new TimeControl
                {
                    Type = TimeControlType.FixedTimePerMove,
                    FixedTimePerSearchSeconds = 5
                };
                do
                {
                    line = sr.ReadLine();

                    if(String.IsNullOrEmpty(line))
                        break;

                    var bmRegex = new Regex("bm (?<bms>[^;]*)");
                    var match = bmRegex.Match(line);
                    var bestMoves = match.Groups["bms"].Value.Split(' ');

                    Console.WriteLine(line);
                    if (String.IsNullOrEmpty(line))
                        break;
                    gameState.GameBoard = Board.ParseFenString(line);
                    var found = Iterate.DoIterate(gameState, () =>
                    {
                        if (ConsoleInputQueue.TryReadLine(out var input))
                        {
                            cmd = CommandParser.ParseCommand(input);
                            if (cmd.Value == CommandVal.Quit)
                            {
                                quit = true;
                                gameState.TimeUp = true;
                                Console.WriteLine("Aborting");
                            }
                            else
                            {
                                Console.WriteLine("Running test!");
                            }
                        }
                    });

                    var moveStr = found.ToAlegbraicNotation(gameState.GameBoard);
                    bool fail = true;
                    foreach (var bm in bestMoves)
                    {
                        if (bm.ToLower() == moveStr.ToLower())
                        {
                            fail = false;
                            break;
                        }
                    }
                    tests++;
                    if (fail)
                        Console.WriteLine("FAIL {0}/{1}",successes,tests);
                    else
                    {
                        successes++;
                        Console.WriteLine("SUCCESS! {0}/{1}",successes,tests);
                    }
                } while (!String.IsNullOrEmpty(line) && !quit);
            }
        }
        catch (FileNotFoundException fex)
        {
            Console.WriteLine(fex.Message);
        }

        Console.WriteLine("Test suite complete");
        Console.WriteLine("{0}/{1} Solved", successes, tests);
    }
}