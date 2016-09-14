namespace mmchess{

    public class HistoryMove : Move{
        public bool IsNullMove{get;private set;}
        public ulong EnPassant {get;set;}

        public byte CastleStatus{get;set;}
        public ulong HashKey{get;set;}
        public MoveBits CapturedPiece {get;set;}
        public HistoryMove(ulong hashKey, Move m)
        {
            HashKey = hashKey;
            if(m == null){
                IsNullMove=true;
                return;
            }
            CopyBaseProperties(m);
        }

        private void CopyBaseProperties(Move m)
        {
            this.Value=m.Value;
        }
    }
}