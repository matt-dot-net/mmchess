using System;
using System.Collections.Generic;

namespace mmchess
{
    public class AlphaBeta
    {
        const int MAX_DEPTH = 64;

        int Ply { get; set; }
        public int Nodes { get; set; }
        public List<Move> [] PrincipalVariation {get;set;}
        public bool TimeUp { get; set; }
        DateTime StartTime { get; set; }
        Board MyBoard { get; set; }
        TimeSpan TimeLimit { get; set; }

        public int CurrentDrawScore { get; set; }
        public AlphaBeta()
        {

        }
        public AlphaBeta(Board b, TimeSpan timeLimit)
        {
            PrincipalVariation = new List<Move>[MAX_DEPTH];
            Ply = 0;
            MyBoard = b;
            StartTime = DateTime.Now;
            TimeLimit = timeLimit;
        }

        void CheckTime()
        {
            if (DateTime.Now - StartTime > TimeLimit)
                TimeUp = true;
        }

        public int Search(int alpha, int beta, int depth)
        {
            if ((Nodes & 16384) > 0)
            {
                CheckTime();
                if (TimeUp)
                    return alpha;
            }
            int best = -10000;
            Move bestMove=null;
            if (depth == 0)
                return Evaluator.Evaluate(MyBoard);

            var moves = MoveGenerator.GenerateMoves(MyBoard);
            Move lastMove = null;
            foreach (var m in moves)
            {
                if (!MyBoard.MakeMove(m))
                    continue;
                lastMove = m;
                Nodes++;
                Ply++;
                int score = -Search(-beta, -alpha, depth - 1);
                MyBoard.UnMakeMove();
                Ply--;
                if (TimeUp)
                {
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
                        bestMove = m;
                    }
                }
            }

            //check for mate
            if (lastMove == null)
            {
                //we can't make a move. check for mate or stalemate.
                if (MyBoard.InCheck(MyBoard.SideToMove))
                {
                    return -10000 + Ply;
                }
                else return CurrentDrawScore;
            }

            //update the PV
            if (bestMove != null)
            {

                if(PrincipalVariation[Ply]== null)
                    PrincipalVariation[Ply]= new List<Move>();
                else
                    PrincipalVariation[Ply].Clear();

                if(PrincipalVariation[Ply+1] != null)
                {
                    foreach(var m in PrincipalVariation[Ply+1])
                        PrincipalVariation[Ply].Add(m);
                }    
                PrincipalVariation[Ply].Insert(0,bestMove);
            }

            return best;
        }
    }
}