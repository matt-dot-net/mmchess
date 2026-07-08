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

    static void PrintMetrics(AlphaBetaMetrics metrics, TimeSpan searchTime)
    {
        //prevent index out of bounds.
        //note, this will not effect the calculation
        if (metrics.Depth == 0)
        {
            metrics.Depth = 1;
        }
        float ebf = 0;
        for (int d = 1; d < metrics.Depth; d++)
        {
            var bf = (float)metrics.DepthNodes[d] / (float)(metrics.DepthNodes[d - 1] + 1);
            if (ebf > 0)
                ebf = (ebf + bf) / 2;
            else
                ebf = bf;
        }
        Console.Error.WriteLine("Nodes={0}, QNodes={1}, Qsearch%={2:0.0}, Knps={3:0}, EBF({4})={5:0.00}",
            metrics.Nodes,
            metrics.QNodes,
            100 * (double)metrics.QNodes / ((double)metrics.Nodes + 1),
            (metrics.Nodes / 1000 / searchTime.TotalSeconds),
            metrics.Depth,
            ebf);
        Console.Error.WriteLine("FirstMoveFH%={0:0.0}, Killers%={1:0.0} FutilePrune={2}, EFutilePrune={3}",
            100 * (double)metrics.FirstMoveFailHigh / ((double)metrics.FailHigh + 1),
            100 * (double)metrics.KillerFailHigh / ((double)metrics.FailHigh + 1),
            metrics.FPrune,
            metrics.EFPrune);
        Console.Error.WriteLine("NullMoveTries={0} NullMove%={1:0.0}, NMResearch={2}, MateThreats={3}, LMRResearch={4}",
            metrics.NullMoveTries,
            100 * (double)metrics.NullMoveFailHigh / ((double)metrics.NullMoveTries + 1),
            metrics.NullMoveResearch,
            metrics.MateThreats,
            metrics.LMRResearch);
        Console.Error.WriteLine("HashTable: FH%={0:0.0} Hit%={1:0.0}",
            100*(double)metrics.TTFailHigh/(double)metrics.FirstMoveFailHigh+1,
            100*(double)TranspositionTable.Instance.Hits/(double)TranspositionTable.Instance.Probes);
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
        return DoSearch(state, interrupt, out metrics, useClock: true, suppressThinking: false, honorFixedDepthLimit: true, updatePonderMove: true, uciOutput: false);
    }

    public static Move DoUciIterate(GameState state, Action interrupt)
    {
        return DoSearch(state, interrupt, out _, useClock: state.TimeControl.Type != TimeControlType.Infinite, suppressThinking: false, honorFixedDepthLimit: true, updatePonderMove: false, uciOutput: true);
    }

    public static Move DoPonder(GameState state, Action interrupt)
    {
        return DoSearch(state, interrupt, out _, useClock: false, suppressThinking: false, honorFixedDepthLimit: false, updatePonderMove: false, uciOutput: false);
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
            useClock: false,
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
        bool useClock,
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
        var rootMoves = MoveGenerator.GenerateLegalMoves(state.GameBoard);
        if (rootMoves.Count == 1)
        {
            metrics = new AlphaBetaMetrics();
            return rootMoves[0];
        }

        var startTime = DateTime.Now;
        var timeLimit = GetThinkTimeSpan(state);
        AlphaBeta ab = new AlphaBeta(state, () =>
        {
            if (useClock && (DateTime.Now - startTime) > timeLimit)
                state.TimeUp = true;
            interrupt();
        });

        //increment transposition table search Id
        TranspositionTable.Instance.NextSearchId();

        int alpha = -10000;
        int beta = 10000;
        Move bestMove = Move.Null;
        Move ponderMove = Move.Null;
        //Console.WriteLine("Ply\tScore\tMillis\tNodes\tPV");
        ab.Metrics.Depth = 0;
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
                score = ab.SearchRoot(alpha, beta, i);
                bestMove=ab.PrincipalVariation[0,0];
                
                if(!state.TimeUp && uciOutput && !suppressThinking && i > 0)
                    PrintUciSearchResult(state, startTime, ab, i, score);
                else if(!state.TimeUp && state.ShowThinking && !suppressThinking)
                    PrintSearchResult(state, startTime, ab, i, score);
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
                ab.Metrics.DepthNodes[i] = ab.Metrics.Nodes;
                ab.Metrics.Depth = i;
                if (updatePonderMove && ab.PvLength[0] > 1)
                    ponderMove = ab.PrincipalVariation[0, 1];
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
            PrintMetrics(ab.Metrics, DateTime.Now - startTime);
        metrics = ab.Metrics;
        if (updatePonderMove)
            state.PonderMove = ponderMove;
        return bestMove;
    }

    private static void PrintSearchResult(GameState state, DateTime startTime, AlphaBeta ab, int i, int score)
    {
        Console.Write("{0} {1} {2:0} {3} ", i, score,
            (DateTime.Now - startTime).TotalMilliseconds / 10, ab.Metrics.Nodes);
        PrintPV(state.GameBoard, ab);
        Console.WriteLine();
    }

    private static void PrintUciSearchResult(GameState state, DateTime startTime, AlphaBeta ab, int i, int score)
    {
        var elapsed = DateTime.Now - startTime;
        var milliseconds = Math.Max(1, (long)elapsed.TotalMilliseconds);
        var nps = (long)(ab.Metrics.Nodes * 1000.0 / milliseconds);
        Console.Write("info depth {0} score cp {1} time {2} nodes {3} nps {4} pv ",
            i,
            score,
            milliseconds,
            ab.Metrics.Nodes,
            nps);
        PrintUciPV(state.GameBoard, ab);
        Console.WriteLine();
    }

    private static void PrintUciPV(Board b, AlphaBeta ab)
    {
        for (int j = 0; j < ab.PvLength[0]; j++)
        {
            var m = ab.PrincipalVariation[0, j];
            Console.Write("{0} ", m.ToCoordinateString());
            b.MakeMove(m);
        }

        for (int j = ab.PvLength[0] - 1; j >= 0; j--)
            b.UnMakeMove();
    }

    private static void PrintPV(Board b, AlphaBeta ab)
    {
        for (int j = 0; j < ab.PvLength[0]; j++)
        {
            var m = ab.PrincipalVariation[0, j];
            Console.Write("{0} ", m.ToAlegbraicNotation(b));
            b.MakeMove(m);
        }

        //Walk through the TT table to augment the PV
        int hashTableMoves = 0;
        List<ulong> hashKeys = new List<ulong>();
        while (true)
        {
            //don't let the hashtable send us into a cycle
            if (hashKeys.Contains(b.HashKey))
                break;

            if (!TranspositionTable.Instance.TryProbe(b.HashKey, out var entry) ||
                entry.Type != (byte)TranspositionTableEntry.EntryType.PV)
                break;
            var m = new Move(entry.MoveValue);
            Console.Write("{0}(HT) ", m.ToAlegbraicNotation(b));
            if (!b.MakeMove(m))
                throw new Exception("invalid move from HT!");
            hashTableMoves++;
            hashKeys.Add(b.HashKey);
        }
        while (hashTableMoves-- > 0)
            b.UnMakeMove();

        for (int j = ab.PvLength[0] - 1; j >= 0; j--)
            b.UnMakeMove();

    }
}
