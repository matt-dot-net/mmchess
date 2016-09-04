using System;
using System.Collections.Generic;
using System.Linq;

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

        int OrderMove(Move m){
            if(PrincipalVariation[Ply] !=null){
                if(PrincipalVariation[Ply][0].Value == m.Value)
                    return int.MaxValue; // search this move first
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
                .OrderBy((m)=>OrderQuiesceMove(m));

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
            int best = -10000;
            Move bestMove=null;
            if (depth == 0)
                return Quiesce(alpha,beta);

            var moves = MoveGenerator
                .GenerateMoves(MyBoard)
                .OrderBy((m)=>OrderMove(m));
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
                //Add to PV
                AddExactToPv(bestMove,alpha,depth);
            }

            //ALL NODE
            AddUpperNodeToTTable(bestMove,best);
            return best;
        }

        void AddExactToPv(Move m, int alpha, int depth){
            TranspositionTable.Instance.Store(
                MyBoard.HashKey,
                new TranspositionTableEntry{
                    Type = (byte) TranspositionTableEntry.EntryType.PV,
                    Score = (UInt16)alpha,
                    Age = (byte)MyBoard.History.Count,
                    MoveValue = m.Value
                }
            );
        }

        void AddUpperNodeToTTable(Move m, int score){
            TranspositionTable.Instance.Store(
                MyBoard.HashKey,
                new TranspositionTableEntry{
                    Type = (byte) TranspositionTableEntry.EntryType.ALL,
                    Score = (UInt16) score,
                    Age = (byte)MyBoard.History.Count,
                    MoveValue = m.Value
                }
            );
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
                MyBoard.HashKey,
                new TranspositionTableEntry{
                    Type = (byte) TranspositionTableEntry.EntryType.CUT,
                    Score = (byte)score,
                    Age = (byte) MyBoard.History.Count,
                    MoveValue = m.Value
                });
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