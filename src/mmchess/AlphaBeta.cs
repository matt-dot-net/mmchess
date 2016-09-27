using System;
using System.Collections.Generic;
using System.Linq;
using static mmchess.TranspositionTableEntry;

namespace mmchess
{
    public class AlphaBeta
    {
        public const int MAX_DEPTH = 64;
        int Ply { get; set; }
        public AlphaBetaMetrics Metrics { get; set; }
        public Move[,] PrincipalVariation { get; private set; }
        public int[] PvLength = new int[MAX_DEPTH];
        public TimeSpan TimeLimit { get; set; }
        Board MyBoard { get; set; }
        GameState MyGameState { get; set; }
        Action Interrupt { get; set; }
        Move[,] Killers = new Move[MAX_DEPTH, 2];
        //int [,] HistoryHeuristic = new int[6,64];
        public int CurrentDrawScore { get; set; }
        

        List<Move> RootMoves{get; set;}
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
            return LvaMvv(m);
        }

        int OrderRootMove(Move m)
        {

            //use the principal variation move first
            if (PrincipalVariation[0, 0] != null && PrincipalVariation[0, 0].Value == m.Value)
                return int.MaxValue;

            // otherwise we will use Qsearch to order the moves
            if (!MyBoard.MakeMove(m))
                return int.MinValue;

            var score = Quiesce(-10000, 10000);

            MyBoard.UnMakeMove();
            return score;

        }

        int OrderMove(Move m, TranspositionTableEntry entry)
        {
            if (entry != null)
            {
                if (m.Value == entry.MoveValue)
                    return int.MaxValue;// search this move first
            }

            if ((m.Bits & (byte)MoveBits.Capture) > 0)
            {

                //winning and even captures
                if(Evaluator.PieceValueOnSquare(MyBoard, m.To) >= Evaluator.PieceValues[(int)Move.GetPiece((MoveBits)m.Bits)])
                    return LvaMvv(m);
                else
                {
                    //verify that it is actually losing with SEE
                    if(StaticExchange.Eval(MyBoard,m, MyBoard.SideToMove) >= 0)
                        return LvaMvv(m); 
                    return int.MinValue + LvaMvv(m);//losing captures at the bottom
                }

            }
            else
            {
                //these happen before losing captures
                if (Killers[Ply, 0] != null && Killers[Ply, 0].Value == m.Value)
                    return 2; 
                else if (Killers[Ply, 1] != null && Killers[Ply, 1].Value == m.Value)
                    return 1;
            }

            return 0;
        }

        private int LvaMvv(Move m)
        {
            //sort by victim-attacker (LVV/MVA)
            //note these will occur after killer moves if they are deemed to be losing
            return (
                //we multiple the victim value so that moves like rook x rook are higher than pawn x pawn
                128*Evaluator.PieceValueOnSquare(MyBoard, m.To) -
                Evaluator.PieceValues[(int)Move.GetPiece((MoveBits)m.Bits)])
                +
                Evaluator.PieceValues[m.Promotion]; //add promotion in as well
        }

        public int Quiesce(int alpha, int beta)
        {
            Metrics.Nodes++;
            Metrics.QNodes++;

            if (Ply >= MAX_DEPTH)
            {
                TakeBack();
                return Evaluator.Evaluate(MyBoard,-10000,10000);
            }

            if ((Metrics.Nodes & 65535) == 65535)
            {
                Interrupt();
                if (MyGameState.TimeUp)
                    return alpha;
            }
            int score = Evaluator.Evaluate(MyBoard,alpha,beta);
            if(score > alpha)
                alpha = score;
            //attemp to stand pat (don't search if eval tells us we are in a good position)
            var inCheck = MyBoard.InCheck(MyBoard.SideToMove);
            if (!inCheck)
            {
                if (score >= beta)
                    return beta;

                //Don't bother searching if we are evaluating at less than a Queen
                if (score < alpha - Evaluator.PieceValues[(int)Piece.Queen])
                    return alpha;

            }

            var moves = MoveGenerator
                .GenerateCapturesAndPromotions(MyBoard,false)
                .OrderByDescending((m) => OrderQuiesceMove(m));
            
            foreach (var m in moves)
            {
                if((BitMask.Mask[m.To] & (MyBoard.King[0]|MyBoard.King[1])) > 0)
                    return beta;

                //if SEE says this is a losing capture, we prune it
                if(StaticExchange.Eval(MyBoard,m,MyBoard.SideToMove) < 0)
                    continue;
                MyBoard.MakeMove(m,false);
                Ply++;

                score = -Quiesce(-beta, -alpha);
                TakeBack();
                if (score >= beta)
                    return beta;
                if (score > alpha)
                    alpha = score;

            }
            return alpha;

        }

        private Boolean Make(Move m)
        {
            if (!MyBoard.MakeMove(m))
                return false;
            Ply++;
            return true;
        }
        private void TakeBack()
        {
            MyBoard.UnMakeMove();
            Ply--;
        }

        public int SearchRoot(int alpha, int beta, int depth)
        {
            PvLength[0]=0;

            if (MyGameState.GameBoard.History.IsGameDrawn(MyBoard.HashKey))
                return CurrentDrawScore;

            if(RootMoves == null){
                RootMoves= MoveGenerator
                .GenerateMoves(MyBoard)
                .OrderByDescending(m => OrderRootMove(m))
                .ToList();
            }

            int score;
            Move bestMove = null, lastMove = null;
            bool inCheck = MyBoard.InCheck(MyBoard.SideToMove);
            foreach (var m in RootMoves)
            {
                if (!Make(m))
                    continue;

                if (depth > 0){
                    if(bestMove == null)
                        score = -Search(-beta, -alpha, depth-1);
                    else   {
                        score = -Search(-alpha-1,-alpha,depth-1);
                        if(score > alpha)
                            score = -Search(-beta,-alpha, depth-1);
                    }
                    
                }
                else
                    score = -Quiesce(-beta, -alpha);

                TakeBack();

                if (MyGameState.TimeUp)
                    return alpha;

                if (score >= beta)
                {
                    //we want to try this move first next time
                    NewRootMove(m);
                    PvLength[0] = 1;
                    PrincipalVariation[0,0]=m;
                    return score;
                }

                if (score > alpha)
                {
                    alpha = score;
                    bestMove = m;
                    // PV Node
                    //update the PV
                    UpdatePv(bestMove);
                    TranspositionTable.Instance.Store(MyBoard.HashKey,m,depth,alpha,EntryType.PV); 
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


            if (bestMove != null)
            {
                if(bestMove != RootMoves[0]){
                    NewRootMove(bestMove);
                }
            }            
            return alpha;
        }

        public int Search(int alpha, int beta, int depth)
        {

            Metrics.Nodes++;
            PvLength[Ply] = Ply;
            var inCheck = MyBoard.InCheck(MyBoard.SideToMove);
            int ext = inCheck ? 1 : 0;

            if (MyGameState.GameBoard.History.IsPositionDrawn(MyBoard.HashKey))
                return CurrentDrawScore;

            if (Ply >= MAX_DEPTH)
            {
                TakeBack();
                return Evaluator.Evaluate(MyBoard,-10000,10000);
            }

            if ((Metrics.Nodes & 65535) == 65535)
            {
                Interrupt();
                if (MyGameState.TimeUp)
                    return alpha;
            }

            Move bestMove = null;
            if (depth+ext <= 0)
                return Quiesce(alpha, beta);

            //first let's look for a transposition
            var entry = TranspositionTable.Instance.Read(MyBoard.HashKey);
            if (entry != null)
            {
                //we have a hit from the TTable
                if (entry.Depth >= depth){
                    if(entry.Type == (byte)EntryType.CUT && entry.Score >= beta)
                        return beta;
                    else if(entry.Type==(byte)EntryType.ALL && entry.Score <= alpha)
                        return alpha;
                    else if(entry.Type==(byte)EntryType.PV)
                        UpdatePv(new Move(entry.MoveValue));
                        return entry.Score;
                }
            }

            int mateThreat = 0;
            var myPieceCount = MyBoard.PieceCount(MyBoard.SideToMove);
            //next try a Null Move
            if (Ply > 0 &&
                depth > 1 &&
                alpha==beta-1 &&
                !inCheck &&
                !MyBoard.History[Ply - 1].IsNullMove &&
                myPieceCount > 0 &&
                (myPieceCount > 2 || depth < 7))
            {
                Metrics.NullMoveTries++;
                MakeNullMove();
                var nullReductionDepth = depth > 6 ? 4 : 3;
                int nmScore;
                if (depth - nullReductionDepth - 1 > 0)
                    nmScore = -Search(-beta, 1 - beta, depth - nullReductionDepth - 1);
                else
                    nmScore = -Quiesce(-beta, 1 - beta);
                UnmakeNullMove();

                if (MyGameState.TimeUp)
                    return alpha;

                if (nmScore >= beta)
                {
                    Metrics.NullMoveFailHigh++;
                    TranspositionTable.Instance.Store(MyBoard.HashKey, null, depth, nmScore, EntryType.CUT);
                    return nmScore;
                }
            }

            var moves = MoveGenerator
                .GenerateMoves(MyBoard)
                .OrderByDescending((m) => OrderMove(m, entry));
            Move lastMove = null;
            int lmr = 0, nonCaptureMoves = 0, movesSearched = 0;

            foreach (var m in moves)
            {
                bool fprune = false;
                int score;
                if (!Make(m))
                    continue;

                var justGaveCheck = MyBoard.InCheck(MyBoard.SideToMove);
                var capture = ((m.Bits & (byte)MoveBits.Capture) != 0);
                if (!capture && (entry==null || entry.MoveValue!=m.Value)) // don't count the hash move as a non-capture
                    ++nonCaptureMoves;                                     // while it might not be a capture, the point 
                                                                           // here is to start counting after generated captures
                var passedpawnpush = (m.Bits & (byte)MoveBits.Pawn) > 0 && (Evaluator.PassedPawnMask[MyBoard.SideToMove^1,m.To] & MyBoard.Pawns[MyBoard.SideToMove]) == 0;                                               
                //LATE MOVE REDUCTIONS
                if (ext == 0 && //no extension
                    !inCheck && //i am not in check at this node
                    !justGaveCheck && //the move we just made does not check the opponent
                    mateThreat == 0 && //no mate threat detected
                    !passedpawnpush && //do not reduce/prune passed pawn pushes
                    nonCaptureMoves > 0) //start reducing after the winning captures
                {
                    if (depth > 2)
                        lmr = movesSearched > 2 ? 2 : 1; // start reducing depth if we aren't finding anything useful
                    //FUTILITY PRUNING
                    else if (depth < 3 && alpha > -9900 && beta < 9900)
                    {
                        if (depth == 2 && -Evaluator.EvaluateMaterial(MyBoard) + Evaluator.PieceValues[(int)Piece.Rook] <= alpha)
                        {
                            Metrics.EFPrune++;
                            fprune = true;
                        }

                        else if (depth == 1 && -Evaluator.EvaluateMaterial(MyBoard) + Evaluator.PieceValues[(int)Piece.Knight] <= alpha)
                        {
                            Metrics.FPrune++;
                            fprune = true;
                        }

                    }
                }
                if (!fprune)
                {
                    //if we don't yet have a move, then search full window (PV Node)
                    if (bestMove == null)
                        score = -Search(-beta, -alpha, depth - 1 - lmr + ext);
                    else //otherwise, use a zero window
                    {
                        //zero window search
                        score = -Search(-alpha - 1, -alpha, depth - 1 - lmr + ext);

                        if (score > alpha)
                        {
                            //this move might be better than our current best move
                            //we have to research with full window

                            score = -Search(-beta, -alpha, depth - 1 - lmr + ext);

                            if (score > alpha && lmr > 0)
                            {
                                //let's research again without the lmr
                                Metrics.LMRResearch++;
                                score = -Search(-beta, -alpha, depth - 1);
                            }
                        }
                    }
                }
                else
                {
                    score = -Quiesce(-beta, -alpha);
                }

                TakeBack();
                ++movesSearched;
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
            MyBoard.History.RemoveLast();

            if (nullMove.EnPassant > 0)
            {
                MyBoard.EnPassant = nullMove.EnPassant;
                var file = MyBoard.EnPassant.BitScanForward().File();
                MyBoard.HashKey ^= TranspositionTable.EnPassantFileKey[file];
            }
            Ply--;

            MyBoard.HashKey^=TranspositionTable.SideToMoveKey;
            MyBoard.SideToMove ^= 1;
        }

        private void MakeNullMove()
        {
            var nullMove = new HistoryMove(MyBoard.HashKey, null);

            MyBoard.SideToMove ^= 1;
            MyBoard.HashKey^=TranspositionTable.SideToMoveKey;

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

        void NewRootMove(Move m){
            //we need to move this one to the top of root moves
            RootMoves.Remove(m);
            RootMoves.Insert(0,m);
        }

        void SearchFailHigh(Move m, int score, int depth, TranspositionTableEntry entry)
        {
            UpdateKillers(m,depth);
            Metrics.FailHigh++;

            if (entry != null && entry.MoveValue == m.Value)
                Metrics.TTFailHigh++;

            else if ((Killers[Ply, 0] != null && m.Value == Killers[Ply, 0].Value) ||
                (Killers[Ply, 1] != null && m.Value == Killers[Ply, 1].Value))
                Metrics.KillerFailHigh++;

            //update the transposition table
            //the move doesn't matter if it is a CUT node
            TranspositionTable.Instance.Store(
                MyBoard.HashKey, m, depth, score, TranspositionTableEntry.EntryType.CUT);
        }

        private void UpdateKillers(Move m, int depth)
        {
            
            if ((m.Bits & (byte)MoveBits.Capture) > 0)
                return;
            
            if (Killers[Ply, 1] != null && Killers[Ply, 1].Value != m.Value)
                Killers[Ply, 0] = Killers[Ply, 1];

            Killers[Ply, 1] = m;
        
//            HistoryHeuristic[(int)Move.GetPieceFromMoveBits((MoveBits)m.Bits)-1,m.To] += depth * depth;
        }
    }
}