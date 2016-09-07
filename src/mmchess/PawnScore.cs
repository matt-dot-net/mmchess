namespace mmchess{

    public class PawnScore{
        public int Eval{get;set;}
        public ulong[,] Files = new ulong[2,8];
    }
}