namespace mmchess{

    public class AlphaBetaMetrics{
        public ulong Nodes{get;set;}
        public ulong QNodes{get;set;}
        public ulong FailHigh{get;set;}
        public ulong FirstMoveFailHigh{get;set;}
        public ulong KillerFailHigh{get;set;}
        public ulong TTFailHigh{get;set;}
    }
}