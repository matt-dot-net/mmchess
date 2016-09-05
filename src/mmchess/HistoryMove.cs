namespace mmchess{

    public class HistoryMove : Move{
        public bool IsNullMove{get;private set;}
        public ulong EnPassant {get;set;}

        public byte CastleStatus{get;set;}
        public ulong HashKey{get;set;}
        public MoveBits CapturedPiece {get;set;}
        public HistoryMove(ulong hashKey, Move m)
        {
            if(m == null){
                IsNullMove=true;
                return;
            }
            HashKey = hashKey;
            CopyBaseProperties(m);
        }

        private void CopyBaseProperties(Move m)
        {
            this.From = m.From;
            this.To = m.To;
            this.Bits = m.Bits;
            this.Promotion = m.Promotion;
        }
    }
}