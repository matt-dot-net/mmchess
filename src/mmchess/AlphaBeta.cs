using System;
using System.Linq;
using static mmchess.TranspositionTableEntry;

namespace mmchess
{
    public class AlphaBeta
    {
        public const int MAX_DEPTH = 64;
        const int R = 3;
        int Ply { get; set; }
        public AlphaBetaMetrics Metrics { get; set; }
        public Move[,] PrincipalVariation { get; private set; }
        public int[] PvLength = new int[MAX_DEPTH];
        public TimeSpan TimeLimit { get; set; }
        Board MyBoard { get; set; }
        GameState MyGameState { get; set; }
        Action Interrupt { get; set; }

        Move[,] Killers = new Move[MAX_DEPTH, 2];

        public int CurrentDrawScore { get; set; }
        public AlphaBeta()
        {
            Metrics = new AlphaBetaMetrics();
        }
        public AlphaBeta(GameState state, Action interrupt)
        {

            PrincipalVariation = new Move[MAX_DEPTH, MAX_DEPTH];
            PvLength[0] = 0;
            Ply = 0;
            MyGameState = state;
            MyBoard = state.GameBoard;
            TimeLimit = TimeSpan.FromSeconds(5);
            Metrics = new AlphaBetaMetrics();
            Interrupt = interrupt;
        }

        int OrderQuiesceMove(Move m)
        {
            //sort by LVA/MVV
            return LvaMvv(m);
        }

        int OrderMove(Move m, TranspositionTableEntry entry)
        {
            if (entry != null)
            {
                if (m.Value == entry.MoveValue)
                    return int.MaxValue;// search this move first
            }

            if (PrincipalVariation[Ply, Ply] != null)
            {
                if (PrincipalVariation[Ply, Ply].Value == m.Value)
                    return int.MaxValue; // search this move first 
                                         // should always be found in hash table
            }

            if ((m.Bits & (byte)MoveBits.Capture) > 0)
            {
                return LvaMvv(m);
            }
            else
            {
                //these happen before losing captures
                if (Killers[Ply, 0] != null && Killers[Ply, 0].Value == m.Value)
                    return -1;
                else if (Killers[Ply, 1] != null && Killers[Ply, 1].Value == m.Value)
                    return -2;
            }

            return 0;
        }

        private int LvaMvv(Move m)
        {
            //sort by victim-attacker (LVV/MVA)
            //note these will occur after killer moves if they are deemed to be losing
            return (Evaluator.PieceValueOnSquare(MyBoard, m.To) -
                Evaluator.PieceValues[(int)Move.GetPiece((MoveBits)m.Bits)])
                +
                Evaluator.PieceValues[m.Promotion]; //add promotion in as well
        }

        public int Quiesce(int alpha, int beta)
        {
            Metrics.Nodes++;
            Metrics.QNodes++;
            if ((Metrics.Nodes & 65535) == 65535)
            {
                Interrupt();
                if (MyGameState.TimeUp)
                    return alpha;
            }

            //attemp to stand pat (don't search if eval tells us we are in a good position)
            if(!MyBoard.InCheck(MyBoard.SideToMove))
            {

                int standPat = Evaluator.Evaluate(MyBoard);
                if (standPat >= beta)
                    return beta;

                //Don't bother searching if we are evaluating at less than a Queen
                if (standPat < alpha - Evaluator.PieceValues[(int)Piece.Queen])
                    return alpha;

                if (alpha < standPat)
                    alpha = standPat;
            }

            var moves = MoveGenerator
                .GenerateCaptures(MyBoard)
                .OrderByDescending((m) => OrderQuiesceMove(m));

            foreach (var m in moves)
            {
                if (!MyBoard.MakeMove(m))
                    continue;
                Ply++;
                int score = -Quiesce(-beta, -alpha);
                MyBoard.UnMakeMove();
                Ply--;
                if (score >= beta)
                    return beta;
                if (score > alpha)
                    alpha = score;

            }
            return alpha;

        }

        public int SearchRoot(int alpha, int beta, int depth)
        {
            throw new NotImplementedException();
        }

        public int Search(int alpha, int beta, int depth)
        {
            if(Ply == MAX_DEPTH-1)
                return alpha;

            Metrics.Nodes++;
            PvLength[Ply] = Ply;
            var inCheck = MyBoard.InCheck(MyBoard.SideToMove);
            int ext = inCheck? 1:0;

            if ((Metrics.Nodes & 65535) == 65535)
            {
                Interrupt();
                if (MyGameState.TimeUp)
                    return alpha;
            }

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
                        case EntryType.ALL:
                        case EntryType.CUT:
                            return entry.Score;
                    }
                }
            }

            int mateThreat = 0;
            //next try a Null Move
            if (Ply > 0 && depth > R + 1 &&
                !inCheck &&
                !MyBoard.History[Ply - 1].IsNullMove)
            {
                Metrics.NullMoveTries++;
                MakeNullMove();
                var nmScore = Search(-beta, 1 - beta, depth - R - 1);
                UnmakeNullMove();

                if(MyGameState.TimeUp)
                    return alpha;


                if (nmScore >= beta)
                {
                    Metrics.NullMoveFailHigh++;
                    

                    TranspositionTable.Instance.Store(MyBoard.HashKey,null,depth,nmScore,EntryType.CUT);
                    return nmScore;
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

                int score;

                //LATE MOVE REDUCTIONS
                if (depth>3 && 
                    ext==0 && //no extension
                    !inCheck && //i am in check at this node
                    !MyBoard.InCheck(MyBoard.SideToMove) && //the move we just made checks the opponent
                    mateThreat == 0 && //don't reduce if we have a mate threat.
                    (m.Bits & (byte)MoveBits.Capture) == 0 && //wait until after captures have been searched 
                    ++nonCapMovesSearched > 2) //wait until after killers have been searched
                {
                    lmr = 1; // start reducing depth if we aren't finding anything useful
                }                

                //if we don't yet have a move, then search full window (PV Node)
                if (bestMove == null)
                    score = -Search(-beta, -alpha, depth - 1 -lmr + ext);
                else //otherwise, use a zero window
                {
                    //zero window search
                    score = -Search(-alpha - 1, -alpha, depth - 1 -lmr + ext);

                    if(score > alpha)
                    {   
                        //this move might be better than our current best move
                        //we have to research with full window
                        
                        score = -Search(-beta,-alpha, depth-1-lmr+ext);

                        if(score>alpha && lmr == 1){
                            //let's research again without the lmr
                            Metrics.LMRResearch++;
                            score = -Search(-beta,-alpha,depth-1);
                        }
                    }
                }


                MyBoard.UnMakeMove();
                Ply--;
                if (MyGameState.TimeUp)
                    return alpha;

                if (score >= beta)
                {
                    SearchFailHigh(m, score, depth, entry);
                    if (lastMove == null)
                        Metrics.FirstMoveFailHigh++;
                    return score;
                }


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
                
                lastMove = m;
            }

            //check for mate
            if (lastMove == null)
            {
                //we can't make a move. check for mate or stalemate.
                if (inCheck)
                    return -10000 + Ply;
                else
                    return CurrentDrawScore;
            }


            if (bestMove == null)
            {
                //ALL NODE
                TranspositionTable.Instance.Store(
                    MyBoard.HashKey, null, depth, alpha,
                    TranspositionTableEntry.EntryType.ALL);
            }
            return alpha;
        }

        private void UnmakeNullMove()
        {
            var nullMove = MyBoard.History[MyBoard.History.Count - 1];
            MyBoard.History.RemoveAt(MyBoard.History.Count - 1);//remove the null move

            if (nullMove.EnPassant > 0)
            {
                MyBoard.EnPassant = nullMove.EnPassant;
                var file = MyBoard.EnPassant.BitScanForward().File();
                MyBoard.HashKey ^= TranspositionTable.EnPassantFileKey[file];
            }
            Ply--;
            MyBoard.HashKey ^= TranspositionTable.SideToMoveKey;
            MyBoard.SideToMove ^= 1;
        }

        private void MakeNullMove()
        {
            var nullMove = new HistoryMove(MyBoard.HashKey, null);

            MyBoard.SideToMove ^= 1;
            MyBoard.HashKey ^= TranspositionTable.SideToMoveKey;
            Ply++;
            if (MyBoard.EnPassant > 0)
            {
                nullMove.EnPassant = MyBoard.EnPassant;
                var file = MyBoard.EnPassant.BitScanForward().File();
                MyBoard.HashKey ^= TranspositionTable.EnPassantFileKey[file];
                MyBoard.EnPassant = 0;
            }

            MyBoard.History.Add(nullMove);//store a null move in history
        }

        private void UpdatePv(Move bestMove)
        {
            PrincipalVariation[Ply, Ply] = bestMove;

            for (int i = Ply + 1; i < PvLength[Ply + 1]; i++)
                PrincipalVariation[Ply, i] = PrincipalVariation[Ply + 1, i];
            PvLength[Ply] = PvLength[Ply + 1];
        }

        void SearchFailHigh(Move m, int score, int depth, TranspositionTableEntry entry)
        {
            UpdateKillers(m);
            Metrics.FailHigh++;

            if (entry != null && entry.MoveValue == m.Value)
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