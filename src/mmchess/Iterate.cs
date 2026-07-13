using System;
using System.Collections.Generic;

namespace mmchess;

public static class Iterate
{
    public const int ASPIRATION_WINDOW = 33;

    //widen the aspiration bounds after a root fail high/low. SearchRoot is
    //fail-soft on a beta cutoff, so `score` can sit far above beta - jump
    //straight past it instead of walking the fixed 33*relax steps out one
    //re-search at a time. (Fail low is fail-hard - SearchRoot returns alpha
    //unchanged - so the score term is a no-op there today; kept symmetric in
    //case the root ever becomes fail-soft on the low side.)
    public static int WidenBeta(int beta, int relax, int score)
    {
        return Math.Min(10000, Math.Max(beta + (ASPIRATION_WINDOW * relax), score + ASPIRATION_WINDOW));
    }

    public static int WidenAlpha(int alpha, int relax, int score)
    {
        return Math.Max(-10000, Math.Min(alpha - (ASPIRATION_WINDOW * relax), score - ASPIRATION_WINDOW));
    }

    static void PrintMetrics(AlphaBetaContext context, TimeSpan searchTime)
    {
        //prevent index out of bounds.
        //note, this will not effect the calculation
        if (context.Metrics.Depth == 0)
        {
            context.Metrics.Depth = 1;
        }
        float ebf = 0;
        for (int d = 1; d < context.Metrics.Depth; d++)
        {
            var bf = (float)context.Metrics.DepthNodes[d] / (float)(context.Metrics.DepthNodes[d - 1] + 1);
            if (ebf > 0)
                ebf = (ebf + bf) / 2;
            else
                ebf = bf;
        }
        Console.Error.WriteLine("Nodes={0}, QNodes={1}, Qsearch%={2:0.0}, Knps={3:0}, EBF({4})={5:0.00}",
            context.Metrics.Nodes,
            context.Metrics.QNodes,
            100 * (double)context.Metrics.QNodes / ((double)context.Metrics.Nodes + 1),
            (context.Metrics.Nodes / 1000 / searchTime.TotalSeconds),
            context.Metrics.Depth,
            ebf);
        Console.Error.WriteLine("FirstMoveFH%={0:0.0}, Killers%={1:0.0} FutilePrune={2}, EFutilePrune={3}",
            100 * (double)context.Metrics.FirstMoveFailHigh / ((double)context.Metrics.FailHigh + 1),
            100 * (double)context.Metrics.KillerFailHigh / ((double)context.Metrics.FailHigh + 1),
            context.Metrics.FPrune,
            context.Metrics.EFPrune);
        Console.Error.WriteLine("NullMoveTries={0} NullMove%={1:0.0}, NMResearch={2}, MateThreats={3}, LMRResearch={4}",
            context.Metrics.NullMoveTries,
            100 * (double)context.Metrics.NullMoveFailHigh / ((double)context.Metrics.NullMoveTries + 1),
            context.Metrics.NullMoveResearch,
            context.Metrics.MateThreats,
            context.Metrics.LMRResearch);
        Console.Error.WriteLine("HashTable: FH%={0:0.0} Hit%={1:0.0}",
            100*(double)context.Metrics.TTFailHigh/(double)context.Metrics.FirstMoveFailHigh+1,
            100*(double)context.TTMetrics.Hits/(double)context.TTMetrics.Probes);
        Console.Error.WriteLine("PawnHashTable: Hit%={0:0.0}",
            100*(double)PawnHashTable.Instance.Hits/(double)PawnHashTable.Instance.Probes);
    }

    static TimeSpan GetThinkTimeSpan(GameState state)
    {
        if (state == null)
            throw new ArgumentNullException("state");
        if (state.TimeControl == null)
            throw new ArgumentException("No Time Control set!");

        switch (state.TimeControl.Type)
        {
            case TimeControlType.FixedTimePerMove:
                if (state.TimeControl.FixedTimePerSearchMilliseconds > 0)
                    return TimeSpan.FromMilliseconds(state.TimeControl.FixedTimePerSearchMilliseconds);
                return TimeSpan.FromSeconds(state.TimeControl.FixedTimePerSearchSeconds);
            case TimeControlType.TimePerGame:
                return TimeSpan.FromSeconds(
                        (state.MyClock.TotalSeconds / 40) +
                        (state.TimeControl.IncrementSeconds / 2)
                    );
            case TimeControlType.NumberOfMoves:
                int moves = (state.GameBoard.History.Count / 2);
                if (moves >= state.TimeControl.MovesInTimeControl)
                {
                    //find the remainder
                    int timeControlsReached = moves / state.TimeControl.MovesInTimeControl;
                    moves -= state.TimeControl.MovesInTimeControl * timeControlsReached;
                }
                var movesRemaining = state.TimeControl.MovesInTimeControl - moves;
                return TimeSpan.FromSeconds(state.MyClock.TotalSeconds / (movesRemaining+1));
            case TimeControlType.Infinite:
                return TimeSpan.MaxValue;
            default:
                return TimeSpan.MaxValue;
        }
    }

    public static Move DoIterate(GameState state, Action interrupt)
    {
        return DoIterate(state, interrupt, out _);
    }

    public static Move DoIterate(GameState state, Action interrupt, out AlphaBetaMetrics metrics)
    {
        return DoSearch(state, interrupt, out metrics, useClock: () => true, suppressThinking: false, honorFixedDepthLimit: true, updatePonderMove: true, uciOutput: false);
    }

    public static Move DoUciIterate(GameState state, Action interrupt)
    {
        return DoSearch(
            state,
            interrupt,
            out _,
            useClock: () => state.TimeControl.Type != TimeControlType.Infinite &&
                (!state.UciPonderSearch || state.UciPonderHit),
            suppressThinking: false,
            honorFixedDepthLimit: true,
            updatePonderMove: state.PonderEnabled,
            uciOutput: true);
    }

    public static Move DoPonder(GameState state, Action interrupt)
    {
        return DoSearch(state, interrupt, out _, useClock: () => false, suppressThinking: false, honorFixedDepthLimit: false, updatePonderMove: false, uciOutput: false);
    }

    public static Move FindPonderMove(GameState state, Action interrupt)
    {
        var originalPonderMove = state.PonderMove;
        var originalPonderMoveMade = state.PonderMoveMade;
        var originalPonderStartHash = state.PonderStartHash;
        var originalTimeControl = state.TimeControl;
        var originalDepthLimit = state.DepthLimit;

        state.TimeControl = new TimeControl { Type = TimeControlType.FixedDepth };
        state.DepthLimit = 1;
        var move = DoSearch(
            state,
            interrupt,
            out _,
            useClock: () => false,
            suppressThinking: true,
            honorFixedDepthLimit: true,
            updatePonderMove: false,
            uciOutput: false);

        state.PonderMove = originalPonderMove;
        state.PonderMoveMade = originalPonderMoveMade;
        state.PonderStartHash = originalPonderStartHash;
        state.TimeControl = originalTimeControl;
        state.DepthLimit = originalDepthLimit;
        return move;
    }

    static Move DoSearch(
        GameState state,
        Action interrupt,
        out AlphaBetaMetrics metrics,
        Func<bool> useClock,
        bool suppressThinking,
        bool honorFixedDepthLimit,
        bool updatePonderMove,
        bool uciOutput)
    {
        state.TimeUp = false;
        if (updatePonderMove)
        {
            state.PonderMove = Move.Null;
            state.PonderMoveMade = false;
            state.PonderStartHash = 0;
        }

        //if the position is forced (exactly one legal move) there is nothing to
        //decide - play it immediately without searching and without burning the
        //clock. (Zero legal moves is mate/stalemate; fall through to the normal
        //path, which returns no move.)
        Span<Move> rootMoveBuffer = stackalloc Move[MoveList.StackCapacity];
        var rootMoves = new MoveList(rootMoveBuffer);
        MoveGenerator.GenerateLegalMoves(state.GameBoard, ref rootMoves);
        if (rootMoves.Count == 1)
        {
            metrics = new AlphaBetaMetrics();
            return rootMoves[0];
        }

        var startTime = DateTime.Now;
        var clockStartTime = startTime;
        var clockWasRunning = false;
        var timeLimit = GetThinkTimeSpan(state);
        var searchInterruptLock = new object();
        Action searchInterrupt = () =>
        {
            lock (searchInterruptLock)
            {
                var clockIsRunning = useClock();
                if (clockIsRunning && !clockWasRunning)
                {
                    clockStartTime = DateTime.Now;
                    clockWasRunning = true;
                }

                if (clockIsRunning && (DateTime.Now - clockStartTime) > timeLimit)
                    state.TimeUp = true;
                interrupt();
            }
        };

        AlphaBeta ab = new AlphaBeta(state, searchInterrupt);
        AlphaBetaContext context = new AlphaBetaContext(state, state.GameBoard, searchInterrupt);

        //increment transposition table search Id
        TranspositionTable.Instance.NextSearchId();

        int alpha = -10000;
        int beta = 10000;
        Move bestMove = Move.Null;
        Move ponderMove = Move.Null;
        //Console.WriteLine("Ply\tScore\tMillis\tNodes\tPV");
        context.Metrics.Depth = 0;
        for (int i = 0; i < AlphaBeta.MAX_DEPTH && !state.TimeUp; i++)
        {
            int score;
            int alphaRelax = 1, betaRelax = 1;
            if (i > 0)
            {
                beta = alpha + ASPIRATION_WINDOW;
                alpha = alpha - ASPIRATION_WINDOW;
            }

            do
            {
                score = ab.SearchRoot(context,alpha, beta, i);
                bestMove=context.PrincipalVariation[0,0];
                
                if(!state.TimeUp && uciOutput && !suppressThinking && i > 0)
                    PrintUciSearchResult(context, startTime, ab, i, score);
                else if(!state.TimeUp && state.ShowThinking && !suppressThinking)
                    PrintSearchResult(context, startTime, ab, i, score);
                if (score > alpha && score < beta)
                {
                    alpha = score;
                    break;
                }
                else if (score >= beta)
                {
                    if(score == 10000)
                        break;

                    beta = WidenBeta(beta, betaRelax, score);
                    betaRelax *= 4;
                }
                else if (score <= alpha)
                {
                    if(score == -10000)
                        break;
                    alpha = WidenAlpha(alpha, alphaRelax, score);
                    alphaRelax *= 4;
                }
            } while (!state.TimeUp); //keep searching for a PV move until time is up

            if (!state.TimeUp)
            {
                context.Metrics.DepthNodes[i] = context.Metrics.Nodes;
                context.Metrics.Depth = i;
                if (updatePonderMove && context.PvLength[0] > 1)
                    ponderMove = context.PrincipalVariation[0, 1];
            }

            if (Math.Abs(score) > 9900) // stop if we have found mate
                break;

            //fixed-depth mode ("sd" command, benchmarking): stop once we've
            //completed the requested depth instead of running to MAX_DEPTH -
            //GetThinkTimeSpan returns TimeSpan.MaxValue for this mode, so
            //nothing else would ever set state.TimeUp
            if (honorFixedDepthLimit && state.TimeControl.Type == TimeControlType.FixedDepth && i >= state.DepthLimit)
                break;
        }
        if (state.ShowThinking && !suppressThinking)
            PrintMetrics(context, DateTime.Now - startTime);
        metrics = context.Metrics;
        if (updatePonderMove)
            state.PonderMove = ponderMove;
        return bestMove;
    }

    private static void PrintSearchResult(AlphaBetaContext context, DateTime startTime, AlphaBeta ab, int i, int score)
    {
        Console.Write("{0} {1} {2:0} {3} ", i, score,
            (DateTime.Now - startTime).TotalMilliseconds / 10, context.Metrics.Nodes);
        PrintPV(context);
        Console.WriteLine();
    }

    private static void PrintUciSearchResult(AlphaBetaContext context, DateTime startTime, AlphaBeta ab, int i, int score)
    {
        var elapsed = DateTime.Now - startTime;
        var milliseconds = Math.Max(1, (long)elapsed.TotalMilliseconds);
        var nps = (long)(context.Metrics.Nodes * 1000.0 / milliseconds);
        Console.Write("info depth {0} score cp {1} time {2} nodes {3} nps {4} pv ",
            i,
            score,
            milliseconds,
            context.Metrics.Nodes,
            nps);
        PrintUciPV(context);
        Console.WriteLine();
    }

    private static void PrintUciPV(AlphaBetaContext context)
    {
        for (int j = 0; j < context.PvLength[0]; j++)
        {
            var m = context.PrincipalVariation[0, j];
            Console.Write("{0} ", m.ToCoordinateString());
            context.Board.MakeMove(m);
        }

        for (int j = context.PvLength[0] - 1; j >= 0; j--)
            context.Board.UnMakeMove();
    }

    private static void PrintPV(AlphaBetaContext context)
    {
        for (int j = 0; j < context.PvLength[0]; j++)
        {
            var m = context.PrincipalVariation[0, j];
            Console.Write("{0} ", m.ToAlegbraicNotation(context.Board));
            context.Board.MakeMove(m);
        }

        //Walk through the TT table to augment the PV
        int hashTableMoves = 0;
        List<ulong> hashKeys = new List<ulong>();
        while (true)
        {
            //don't let the hashtable send us into a cycle
            if (hashKeys.Contains(context.Board.HashKey))
                break;

            if (!TranspositionTable.Instance.TryProbe(context, out var entry) ||
                entry.Type != (byte)TranspositionTableEntry.EntryType.PV)
                break;
            var m = new Move(entry.MoveValue);
            Console.Write("{0}(HT) ", m.ToAlegbraicNotation(context.Board));
            if (!context.Board.MakeMove(m))
                throw new Exception("invalid move from HT!");
            hashTableMoves++;
            hashKeys.Add(context.Board.HashKey);
        }
        while (hashTableMoves-- > 0)
            context.Board.UnMakeMove();

        for (int j = context.PvLength[0] - 1; j >= 0; j--)
            context.Board.UnMakeMove();

    }
}
