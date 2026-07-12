using System;
using System.Diagnostics;

namespace mmchess;
public struct AlphaBetaContext
{
    
    const ulong InitialInterruptCheckNodes = 4096;
    ulong NextInterruptCheckNode { get; set; }
    ulong InterruptSampleNodes { get; set; }
    long InterruptSampleTimestamp { get; set; }
    Action Interrupt { get; set; }
    public AlphaBetaMetrics Metrics { get; set; }
    public TTMetrics TTMetrics { get; set; }
    public Move[,] PrincipalVariation { get; private set; }
    public int[] PvLength = new int[AlphaBeta.MAX_DEPTH];
    public int Ply { get; set; }
    public Move[,] Killers = new Move[AlphaBeta.MAX_DEPTH, 2];
    // [side to move, piece type - 1, to-square]: unlike Killers (indexed by
    // ply, where side-to-move already alternates implicitly along one line
    // of play), history accumulates across the whole tree, where the same
    // ply can be reached by either side across different branches - so side
    // has to be its own index here to keep White's and Black's stats apart.
    public int[,,] HistoryHeuristic = new int[2, 6, 64];

    public GameState GameState{get;set;}

    public AlphaBetaContext(GameState gameState, Board gameBoard, Action interrupt = null)
    {
        GameState = gameState;
        PrincipalVariation = new Move[AlphaBeta.MAX_DEPTH, AlphaBeta.MAX_DEPTH];
        PvLength[0] = 0;
        Ply = 0;
        Board = gameBoard;
        Metrics = new AlphaBetaMetrics();
        TTMetrics = new TTMetrics();
        Interrupt = interrupt;
        ResetInterruptCheckSchedule();
    }

    public Board Board { get; set; }

    public AlphaBetaContext Split()
    {
        return Split(Board.CloneForSearch());
    }

    public AlphaBetaContext Split(Board clonedBoard)
    {
        if (clonedBoard == null)
            throw new ArgumentNullException(nameof(clonedBoard));

        var split = new AlphaBetaContext(GameState, clonedBoard, Interrupt)
        {
            Ply = Ply
        };

        Array.Copy(PrincipalVariation, split.PrincipalVariation, PrincipalVariation.Length);
        Array.Copy(PvLength, split.PvLength, PvLength.Length);
        Array.Copy(Killers, split.Killers, Killers.Length);
        Array.Copy(HistoryHeuristic, split.HistoryHeuristic, HistoryHeuristic.Length);
        return split;
    }

    public void CountNode()
    {
        Metrics.Nodes++;
        if (Metrics.Nodes >= NextInterruptCheckNode)
            CheckScheduledInterrupt();
    }

    void CheckScheduledInterrupt()
    {
        Interrupt?.Invoke();

        var now = Stopwatch.GetTimestamp();
        var elapsedTicks = now - InterruptSampleTimestamp;
        var elapsedNodes = Metrics.Nodes - InterruptSampleNodes;

        ulong nextInterval;
        if (elapsedTicks > 0 && elapsedNodes > 0)
        {
            var nodesPerTargetWindow = elapsedNodes * (double)AlphaBeta.InterruptCheckTargetTicks / elapsedTicks;
            nextInterval = Math.Max(1, (ulong)Math.Round(nodesPerTargetWindow));
        }
        else
        {
            nextInterval = InitialInterruptCheckNodes;
        }

        InterruptSampleTimestamp = now;
        InterruptSampleNodes = Metrics.Nodes;
        NextInterruptCheckNode = Metrics.Nodes + nextInterval;
    }

    void ResetInterruptCheckSchedule()
    {
        InterruptSampleTimestamp = Stopwatch.GetTimestamp();
        InterruptSampleNodes = Metrics?.Nodes ?? 0;
        NextInterruptCheckNode = InterruptSampleNodes + InitialInterruptCheckNodes;
    }

    public bool Make(Move m)
    {
        if (!Board.MakeMove(m))
            return false;
        Ply++;
        return true;
    }
    public void TakeBack()
    {
        Board.UnMakeMove();
        Ply--;
    }
}
