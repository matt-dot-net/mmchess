using System;
using System.Collections.Generic;
using System.Linq;
using static mmchess.TranspositionTableEntry;

namespace mmchess
{
    public class AlphaBeta
    {
        const int MAX_DEPTH = 64;
        const int R = 3;
        int Ply { get; set; }
        public AlphaBetaMetrics Metrics { get; set; }
        public Move[,] PrincipalVariation { get; private set;}
        public int [] PvLength = new int[MAX_DEPTH];
        public bool TimeUp { get; set; }
        DateTime StartTime { get; set; }
        Board MyBoard { get; set; }
        TimeSpan TimeLimit { get; set; }

        Move[,] Killers = new Move[MAX_DEPTH, 2];

        public int CurrentDrawScore { get; set; }
        public AlphaBeta()
        {
            Metrics = new AlphaBetaMetrics();
        }
        public AlphaBeta(Board b, TimeSpan timeLimit)
        {
            PrincipalVariation = new Move[MAX_DEPTH,MAX_DEPTH];
            PvLength[0]=0;
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

        int OrderQuiesceMove(Move m)
        {
            if ((m.Bits & (byte)MoveBits.Capture) > 0)
            {
                //sort by LVA/MVV
                return Evaluator.PieceValueOnSquare(MyBoard, m.To) -
                    Evaluator.MovingPieceValue((MoveBits)m.Bits);
            }

            return int.MaxValue; // checks first
        }

        int OrderMove(Move m, TranspositionTableEntry entry)
        {

            if (entry != null)
            {
                if (m.Value == entry.MoveValue)
                    return int.MaxValue;// search this move first
            }

            if (PrincipalVariation[Ply,Ply] != null)
            {
                if (PrincipalVariation[Ply,Ply].Value == m.Value)
                    return int.MaxValue; // search this move first 
                                         // should always be found in hash table
            }

            if ((m.Bits & (byte)MoveBits.Capture) > 0)
            {
                //sort by LVA/MVV
                return Evaluator.PieceValueOnSquare(MyBoard, m.To) -
                    Evaluator.MovingPieceValue((MoveBits)m.Bits);

            }
            else
            {
                if (Killers[Ply, 0] != null && Killers[Ply, 0].Value == m.Value)
                    return 2;
                else if (Killers[Ply, 1] != null && Killers[Ply, 1].Value == m.Value)
                    return 1;
            }

            return 0;
        }

        public int Quiesce(int alpha, int beta)
        {
            Metrics.Nodes++;
            Metrics.QNodes++;
            if ((Metrics.Nodes & 65536) > 0)
            {
                CheckTime();
                if (TimeUp)
                    return alpha;
            }

            int standPat = Evaluator.Evaluate(MyBoard);
            if (standPat >= beta)
                return beta;
            if (alpha < standPat)
                alpha = standPat;

            var moves = MoveGenerator
                .GenerateCaptures(MyBoard)
                .OrderByDescending((m) => OrderQuiesceMove(m));

            foreach (var m in moves)
            {
                if (!MyBoard.MakeMove(m))
                    continue;
                int score = -Quiesce(-beta, -alpha);
                MyBoard.UnMakeMove();
                if (score >= beta)
                    return beta;
                if (score > alpha)
                    alpha = score;

            }
            return alpha;

        }

        public int Search(int alpha, int beta, int depth)
        {
            Metrics.Nodes++;
            PvLength[Ply]=Ply;
            if ((Metrics.Nodes & 65536) > 0)
            {
                CheckTime();
                if (TimeUp)
                    return alpha;
            }

            int best = -10000;
            Move bestMove = null;
            if (depth <= 0)
                return Quiesce(alpha, beta);

            //first let's look for a transposition
            var entry = TranspositionTable.Instance.Read(MyBoard.HashKey);
            if (entry != null)
            {
                //we have a hit from the TTable
                if (entry.Depth > depth)
                {

                    switch ((EntryType)entry.Type)
                    {
                        case EntryType.PV:
                            if (entry.Value < beta)
                                UpdatePv(new Move(entry.Value));
                            return entry.Score;
                        case EntryType.ALL:
                        case EntryType.CUT:
                            {
                                return entry.Score;
                            }
                    }
                }
            }

            //next try a Null Move
            if(Ply>0 && depth > R+1 &&
                !MyBoard.InCheck(MyBoard.SideToMove) &&
                !MyBoard.History[Ply-1].IsNullMove)
            {
                MyBoard.SideToMove^=1;
                MyBoard.HashKey^=TranspositionTable.SideToMoveKey[MyBoard.SideToMove];
                Ply++;
                var oldEnPassant = MyBoard.EnPassant;
                MyBoard.EnPassant=0;
                MyBoard.History.Add(new HistoryMove(null));//store a null move in history
                var nmScore = Search(-beta,1-beta,depth-R-1);
                MyBoard.History.RemoveAt(MyBoard.History.Count-1);//remove the null move
                MyBoard.EnPassant= oldEnPassant;
                Ply--;
                MyBoard.HashKey^=TranspositionTable.SideToMoveKey[MyBoard.SideToMove];
                MyBoard.SideToMove^=1;
                if(nmScore>=beta){
                    Metrics.NullMoveFailHigh++;
                    Metrics.FailHigh++;
                    Metrics.FirstMoveFailHigh++;
                    return beta;
                }
            }


            var moves = MoveGenerator
                .GenerateMoves(MyBoard)
                .OrderByDescending((m) => OrderMove(m, entry));
            Move lastMove = null;
            int nonCapMovesSearched = 0, lmr = 0;
            foreach (var m in moves)
            {
                if (!MyBoard.MakeMove(m))
                    continue;

                Ply++;
                int score = -Search(-beta, -alpha, depth - 1 - lmr);
                MyBoard.UnMakeMove();
                Ply--;
                if (TimeUp)
                {
                    return alpha;
                }
                if ((m.Bits & (byte)MoveBits.Capture)==0 && ++nonCapMovesSearched > 2) // start reducing depth if we aren't finding anything useful
                    lmr=1;

                if (score >= beta)
                {
                    SearchFailHigh(m, score, depth, entry);
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
                        // PV Node
                        //update the PV
                        UpdatePv(bestMove);
                        //Add to hashtable
                        TranspositionTable.Instance.Store(
                            MyBoard.HashKey, bestMove, depth, alpha, TranspositionTableEntry.EntryType.PV);
                    }
                }
                lastMove = m;
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


            if (bestMove ==null)
            {
                //ALL NODE
                TranspositionTable.Instance.Store(
                    MyBoard.HashKey, null, depth, alpha,
                    TranspositionTableEntry.EntryType.ALL);
            }
            return best;
        }

        private void UpdatePv(Move bestMove)
        {
            PrincipalVariation[Ply,Ply]=bestMove;

            for(int i=Ply+1;i<PvLength[Ply+1];i++)
                PrincipalVariation[Ply,i] = PrincipalVariation[Ply+1,i];
            PvLength[Ply] = PvLength[Ply+1];
        }

        void SearchFailHigh(Move m, int score, int depth, TranspositionTableEntry entry)
        {
            UpdateKillers(m);
            Metrics.FailHigh++;

            if(entry != null && entry.MoveValue == m.Value)
                Metrics.TTFailHigh++;

            else if ((Killers[Ply, 0] != null && m.Value == Killers[Ply, 0].Value) ||
                (Killers[Ply, 1] != null && m.Value == Killers[Ply, 1].Value))
                Metrics.KillerFailHigh++;

            //update the transposition table
            TranspositionTable.Instance.Store(
                MyBoard.HashKey, m, depth, score, TranspositionTableEntry.EntryType.CUT);
        }

        private void UpdateKillers(Move m)
        {
            if ((m.Bits & (byte)MoveBits.Capture) == 0)
            {
                if (Killers[Ply, 1] != null && Killers[Ply, 1].Value != m.Value)
                    Killers[Ply, 0] = Killers[Ply, 1];

                Killers[Ply, 1] = m;
            }
        }
    }
}