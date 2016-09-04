using System;
using System.Collections.Generic;
using System.Linq;
using static mmchess.TranspositionTableEntry;

namespace mmchess
{
    public class AlphaBeta
    {
        const int MAX_DEPTH = 64;

        int Ply { get; set; }
        public AlphaBetaMetrics Metrics {get;set;}
        public List<Move> [] PrincipalVariation {get;set;}
        public bool TimeUp { get; set; }
        DateTime StartTime { get; set; }
        Board MyBoard { get; set; }
        TimeSpan TimeLimit { get; set; }

        Move [,] Killers = new Move[MAX_DEPTH,2];

        public int CurrentDrawScore { get; set; }
        public AlphaBeta()
        {
            Metrics = new AlphaBetaMetrics();
        }
        public AlphaBeta(Board b, TimeSpan timeLimit)
        {
            PrincipalVariation = new List<Move>[MAX_DEPTH];
            Ply = 0;
            MyBoard = b;
            StartTime = DateTime.Now;
            TimeLimit = timeLimit;
            Metrics = new AlphaBetaMetrics();
        }

        void CheckTime()
        {
            if (DateTime.Now - StartTime > TimeLimit)
                TimeUp = true;
        }

        int OrderQuiesceMove(Move m){
            if((m.Bits & (byte)MoveBits.Capture)>0){
                //sort by LVA/MVV
                return Evaluator.PieceValueOnSquare(MyBoard, m.To)-
                    Evaluator.MovingPieceValue((MoveBits)m.Bits);
            }

            return int.MaxValue; // checks first
        }

        int OrderMove(Move m, TranspositionTableEntry entry){           

            if(entry != null ) {
                if(m.Value == entry.MoveValue )
                    return int.MaxValue;// search this move first
            }

            if(PrincipalVariation[Ply] !=null){
                if(PrincipalVariation[Ply][0].Value == m.Value)
                    return int.MaxValue; // search this move first 
                                         // should always be found in hash table
            }

            if((m.Bits & (byte)MoveBits.Capture) >0 )
            {
                //sort by LVA/MVV
                return Evaluator.PieceValueOnSquare(MyBoard, m.To)-
                    Evaluator.MovingPieceValue((MoveBits)m.Bits);
                    
            }
            else{
                if(Killers[Ply,0]!=null && Killers[Ply,0].Value==m.Value)
                    return 2;
                else if(Killers[Ply,1] != null && Killers[Ply,1].Value==m.Value)
                    return 1;
            }

            return 0;
        }

        public int Quiesce(int alpha,int beta){
            Metrics.Nodes++;
            Metrics.QNodes++;
            if ((Metrics.Nodes & 65536) > 0)
            {
                CheckTime();
                if (TimeUp)
                    return alpha;
            }

            int standPat = Evaluator.Evaluate(MyBoard);
            if(standPat >= beta)
                return beta;
            if (alpha < standPat)
                alpha = standPat;

            var moves = MoveGenerator
                .GenerateCaptures(MyBoard)
                .OrderByDescending((m)=>OrderQuiesceMove(m));

            foreach(var m in moves){
                if(!MyBoard.MakeMove(m))
                    continue;
                int score = -Quiesce(-beta,-alpha);
                MyBoard.UnMakeMove();
                if(score >= beta)
                    return beta;
                if(score > alpha)
                    alpha=score;

            }
            return alpha;

        }

        public int Search(int alpha, int beta, int depth)
        {
            Metrics.Nodes++;
            if ((Metrics.Nodes & 65536) > 0)
            {
                CheckTime();
                if (TimeUp)
                    return alpha;
            }

            //first let's look for a transposition
            var entry = TranspositionTable.Instance.Read(MyBoard.HashKey);
            if(entry != null){
                //we have a hit from the TTable
                if(entry.Depth > depth)
                {   

                    switch((EntryType) entry.Type)
                    {
                        case EntryType.PV:
                            if(entry.Value < beta)
                                UpdatePv(new Move(entry.Value));
                            return entry.Score;
                        case EntryType.ALL:
                        case EntryType.CUT:{
                            return entry.Score;
                        }
                    }
                }
            }

            int best = -10000;
            Move bestMove=null;
            if (depth == 0)
                return Quiesce(alpha,beta);

            var moves = MoveGenerator
                .GenerateMoves(MyBoard)
                .OrderByDescending((m)=>OrderMove(m,entry));
            Move lastMove = null;
            foreach (var m in moves)
            {
                if (!MyBoard.MakeMove(m))
                    continue;
                
                Ply++;
                int score = -Search(-beta, -alpha, depth - 1);
                MyBoard.UnMakeMove();
                Ply--;
                if (TimeUp)
                {
                    return alpha;
                }
                if (score >= beta)
                {
                    SearchFailHigh(m, score,depth);
                    if (lastMove == null)
                        Metrics.FirstMoveFailHigh++;
                    return score;
                }
                if (score > best)
                {
                    best = score;

                    if (score > alpha)
                    {
                        alpha = score;
                        bestMove = m;
                        //potential PV Node
                    }
                }
                lastMove = m;
            }

            //check for mate
            if (lastMove == null)
            {
                //we can't make a move. check for mate or stalemate.
                //first truncate the PV
                PrincipalVariation[Ply]=null;
                if (MyBoard.InCheck(MyBoard.SideToMove))
                {
                    return -10000 + Ply;
                }
                else return CurrentDrawScore;
            }

            
            if (bestMove != null)
            {
                //update the PV
                UpdatePv(bestMove);
                //Add to hashtable
                TranspositionTable.Instance.Store(
                    MyBoard.HashKey,bestMove,depth,alpha,TranspositionTableEntry.EntryType.PV);

            }
            else{
            //ALL NODE
                TranspositionTable.Instance.Store(
                    MyBoard.HashKey,null,depth,alpha, 
                    TranspositionTableEntry.EntryType.ALL);
            }
            return best;
        }

        private void UpdatePv(Move bestMove)
        {
            if (PrincipalVariation[Ply] == null)
                PrincipalVariation[Ply] = new List<Move>();
            else
                PrincipalVariation[Ply].Clear();

            if (PrincipalVariation[Ply + 1] != null)
            {
                foreach (var m in PrincipalVariation[Ply + 1])
                    PrincipalVariation[Ply].Add(m);
            }
            PrincipalVariation[Ply].Insert(0, bestMove);
        }




        void SearchFailHigh( Move m, int score, int depth)
        {
            UpdateKillers(m);
            Metrics.FailHigh++;

            if ((Killers[Ply, 0] != null && m.Value == Killers[Ply, 0].Value) ||
                (Killers[Ply, 1] != null && m.Value == Killers[Ply, 1].Value))
                Metrics.KillerFailHigh++;

            //update the transposition table
            TranspositionTable.Instance.Store(
                MyBoard.HashKey,m,depth,score,TranspositionTableEntry.EntryType.CUT);
        }

        private void UpdateKillers(Move m)
        {
            if ((m.Bits & (byte)MoveBits.Capture) == 0)
            {
                if (Killers[Ply,1] != null && Killers[Ply,1].Value != m.Value)
                    Killers[Ply,0] = Killers[Ply,1];

                Killers[Ply,1] = m;
            }
        }
    }
}