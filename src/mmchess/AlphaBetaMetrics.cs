namespace mmchess;

public class AlphaBetaMetrics{
    public ulong Nodes{get;set;}
    public ulong QNodes{get;set;}
    public ulong FailHigh{get;set;}
    public ulong FirstMoveFailHigh{get;set;}
    public ulong KillerFailHigh{get;set;}
    public ulong TTFailHigh{get;set;}
    public ulong NullMoveTries{get;set;}
    public ulong NullMoveFailHigh{get;set;}
    public ulong NullMoveResearch{get;set;}
    public ulong MateThreats{get;set;}
    public ulong LMRResearch{get;set;}
    public ulong FPrune{get;set;}
    public ulong EFPrune {get;set;}
    public ulong SplitPointsCreated { get; set; }
    public ulong WorkItemsScheduled { get; set; }
    public ulong WorkItemsStarted { get; set; }
    public ulong WorkItemsCompleted { get; set; }
    public ulong WorkItemsSkipped { get; set; }
    public ulong WorkItemsCancelled { get; set; }
    public ulong WorkerFailLows { get; set; }
    public ulong WorkerFailHighCandidates { get; set; }
    public ulong CandidatesInvalidatedByAlpha { get; set; }
    public ulong FullWindowResearches { get; set; }
    public ulong ParallelBetaCutoffs { get; set; }
    public ulong[] DepthNodes{get;set;}
    public int Depth{get;set;}
    public AlphaBetaMetrics (){
        DepthNodes = new ulong[AlphaBeta.MAX_DEPTH];
    }
}
