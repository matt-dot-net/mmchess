namespace mmchess{

    public class HistoryMove : Move{
        public ulong EnPassant {get;set;}

        public byte CastleStatus{get;set;}

        public MoveBits CapturedPiece {get;set;}
        public HistoryMove(Move m)
        {
            CopyBaseProperties(m);
        }

        private void CopyBaseProperties(Move m)
        {
            this.From = m.From;
            this.To = m.To;
            this.Bits = m.Bits;
            this.Promotion = m.Promotion;
        }

        public HistoryMove(HistoryMove hm){
            this.EnPassant=hm.EnPassant;
            this.CastleStatus=hm.CastleStatus;
            this.CapturedPiece= hm.CapturedPiece;
            CopyBaseProperties(hm);
        }
    }
}