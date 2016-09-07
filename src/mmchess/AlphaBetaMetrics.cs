namespace mmchess{

    public class AlphaBetaMetrics{
        public ulong Nodes{get;set;}
        public ulong QNodes{get;set;}
        public ulong FailHigh{get;set;}
        public ulong FirstMoveFailHigh{get;set;}
        public ulong KillerFailHigh{get;set;}
        public ulong TTFailHigh{get;set;}
        public ulong NullMoveFailHigh{get;set;}
        public ulong NullMoveResearch{get;set;}
        public ulong MateThreats{get;set;}
        public ulong LMRResearch{get;set;}
    }
}