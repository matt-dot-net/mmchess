using System;
using System.Collections.Generic;

namespace mmchess
{
    public class AlphaBeta
    {
        int Ply { get; set; }
        public int Nodes { get; set; }
        public List<Move> PrincipalVariation { get; set; }
        public bool TimeUp{get;set;}
        DateTime StartTime{get;set;}
        Board MyBoard { get; set; }
        TimeSpan TimeLimit{get;set;}

        public int CurrentDrawScore{get;set;}
        public AlphaBeta()
        {

        }
        public AlphaBeta(Board b, TimeSpan timeLimit)
        {
            PrincipalVariation = new List<Move>();
            Ply = 0;
            MyBoard = b;
            StartTime = DateTime.Now;
            TimeLimit = timeLimit;
        }

        void CheckTime(){
            if(DateTime.Now - StartTime > TimeLimit)
                TimeUp=true;
        }

        public int Search(int alpha, int beta, int depth)
        {
            if((Nodes & 16384)>0){
                CheckTime();
                if(TimeUp)
                    return alpha;
            }
            int best = -10000;
            if (depth == 0)
                return Evaluator.Evaluate(MyBoard);
            var moves = MoveGenerator.GenerateMoves(MyBoard);
            Move lastMove = null;
            foreach (var m in moves)
            {
                if (!MyBoard.MakeMove(m))
                    continue;
                lastMove=m;
                Nodes++;
                Ply++;
                int score = -Search(-beta, -alpha, depth - 1);
                MyBoard.UnMakeMove();
                Ply--;
                if(TimeUp){
                    //truncate PV
                    if(PrincipalVariation.Count>=Ply+1)
                        PrincipalVariation.RemoveRange(Ply,PrincipalVariation.Count-(Ply+1));
                    return alpha;
                }
                if (score >= beta)
                    return score;
                if (score > best)
                {
                    best = score;
                    if (score > alpha)
                    {
                        alpha = score;

                        if(PrincipalVariation.Count<=Ply)
                            PrincipalVariation.Add(m);
                        else {
                            PrincipalVariation[Ply]=m;
                            if(PrincipalVariation.Count > Ply+1)
                                PrincipalVariation.RemoveRange(Ply+1,PrincipalVariation.Count-(Ply+1));
                        }
                    }
                }
                 
            }

            //check for mate
            if(lastMove ==null){
                //we can't make a move. check for mate or stalemate.
                if(MyBoard.InCheck(MyBoard.SideToMove))
                {
                    return -10000 + Ply;
                }
                else return CurrentDrawScore;
            }
            return best;
        }
    }
}