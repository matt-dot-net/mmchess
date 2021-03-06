using System;
using System.IO;
using System.Text.RegularExpressions;

namespace mmchess
{

    public enum CommandVal
    {
        NoOp,
        Uci,
        xboard,
        Protover,
        Black,
        White,
        Force,
        New,
        Random,
        IsReady,
        Post,
        NoPost,
        Hard,
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
        ST
    }
    public class Command
    {
        public CommandVal Value { get; set; }
        public string[] Arguments;

    }

    public static class CommandParser
    {
        const String VersionNumber = "0.1";


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
            else if (cmd.Value == CommandVal.Force)
            {
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
                     cmd.Value == CommandVal.Hard)
            {
                //noop

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
                Console.WriteLine("id name mmchess {0}");
                Console.WriteLine("author Matt McKnight");
                Console.WriteLine("uciok");
            }
            else if (cmd.Value == CommandVal.Protover)
            {
                Console.WriteLine("feature setboard=1 reuse=1 myname=\"mmchess\" done=1");
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
                            bool waitForLine = false;
                            if (Console.IsInputRedirected)
                                waitForLine = (Console.In.Peek() != -1);
                            else if (Console.KeyAvailable)
                                waitForLine = true;

                            if (waitForLine)
                            {
                                var input = Console.ReadLine();
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
}