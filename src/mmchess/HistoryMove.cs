namespace mmchess{

    public class HistoryMove : Move{
        public ulong EnPassant {get;set;}

        public HistoryMove(Move m){
            this.From = m.From;
            this.To = m.To;
            this.Bits=m.Bits;
            this.Promotion = m.Promotion;
        }
    }
}