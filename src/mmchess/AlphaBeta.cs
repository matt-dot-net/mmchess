using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static mmchess.TranspositionTableEntry;

namespace mmchess;

public partial class AlphaBeta
{
    
    public static readonly long InterruptCheckTargetTicks = Stopwatch.Frequency * InterruptCheckTargetMilliseconds / 1000;
    const int InterruptCheckTargetMilliseconds = 75;
    AlphaBetaContext Context{get;set;}
    public TimeSpan TimeLimit { get; set; }
    GameState MyGameState { get; set; }
    public int CurrentDrawScore { get; set; }

    List<Move> RootMoves{get; set;}
    public AlphaBetaContext[] Threads{get;set;}
    public AlphaBeta(GameState state)
    {
        MyGameState = state;

    }
    public AlphaBeta(GameState state, Action interrupt)
    {
        TimeLimit = TimeSpan.FromSeconds(5);
        MyGameState = state;
    }    

    public int SearchRoot(AlphaBetaContext context,int alpha, int beta, int depth)
    {
        context.PvLength[0]=0;

        if (MyGameState.GameBoard.History.IsGameDrawn(context.Board.HashKey) ||
            context.Board.IsInsufficientMaterial())
            return CurrentDrawScore;

        if(RootMoves == null){
            Span<Move> moveBuffer = stackalloc Move[MoveList.StackCapacity];
            var generatedMoves = new MoveList(moveBuffer);
            MoveGenerator.GenerateMoves(context.Board, ref generatedMoves);

            RootMoves = new List<Move>(generatedMoves.Count);
            generatedMoves.CopyTo(RootMoves);
            RootMoves = RootMoves
                .OrderByDescending(m => OrderRootMove(context,m))
                .ToList();
        }

        int score;
        Move bestMove = Move.Null, lastMove = Move.Null;
        bool inCheck = context.Board.InCheck(context.Board.SideToMove);
        foreach (var m in RootMoves)
        {
            if (!context.Make(m))
                continue;

            if (depth > 0){
                if(bestMove.IsNull)
                    score = -Search(context,-beta, -alpha, depth-1);
                else   {
                    score = -Search(context,-alpha-1,-alpha,depth-1);
                    if(score > alpha)
                        score = -Search(context,-beta,-alpha, depth-1);
                }
                
            }
            else
                score = -Quiesce(context,-beta, -alpha);

            context.TakeBack();

            if (MyGameState.TimeUp)
                return alpha;

            if (score >= beta)
            {
                //we want to try this move first next time
                NewRootMove(m);
                context.PvLength[0] = 1;
                context.PrincipalVariation[0,0]=m;
                return score;
            }

            if (score > alpha)
            {
                alpha = score;
                bestMove = m;
                // PV Node
                //update the PV
                UpdatePv(context,bestMove);
                TranspositionTable.Instance.Store(context.Board.HashKey,m,depth,alpha,EntryType.PV,context.Ply);
            }

            lastMove = m;
        }

        //check for mate
        if (lastMove.IsNull)
        {
            //we can't make a move. check for mate or stalemate.
            if (inCheck)
                return -10000 + context.Ply;
            else
                return CurrentDrawScore;
        }


        if (!bestMove.IsNull)
        {
            if(bestMove != RootMoves[0]){
                NewRootMove(bestMove);
            }
        }            
        return alpha;
    }

    public int Search(AlphaBetaContext context, int alpha, int beta, int depth)
    {

        context.CountNode();
        if (context.Ply >= MAX_DEPTH)
        {
            return Evaluator.Evaluate(context.Board,-10000,10000);
        }

        context.PvLength[context.Ply] = context.Ply;
        var inCheck = context.Board.InCheck(context.Board.SideToMove);
        int ext = inCheck ? 1 : 0;

        if (context.GameState.GameBoard.History.IsPositionDrawn(context.Board.HashKey) ||
            context.Board.IsInsufficientMaterial())
            return CurrentDrawScore;

        if (context.GameState.TimeUp)
        {
            return alpha;
        }

        Move bestMove = Move.Null;
        if (depth+ext <= 0)
            return Quiesce(context, alpha, beta);

        //first let's look for a transposition
        var hasEntry = TranspositionTable.Instance.TryProbe(context.Board.HashKey, out var entry);
        if (hasEntry)
        {
            //we have a hit from the TTable
            if (entry.Depth >= depth){
                var ttScore = TranspositionTable.ValueFromTT(entry.Score, context.Ply);
                if(entry.Type == (byte)EntryType.CUT && ttScore >= beta)
                    return beta;
                else if(entry.Type==(byte)EntryType.ALL && ttScore <= alpha)
                    return alpha;
                else if(entry.Type==(byte)EntryType.PV)
                    return ttScore;
            }
        }

        int mateThreat = 0;
        var myPieceCount = context.Board.PieceCount(context.Board.SideToMove);
        //next try a Null Move
        if (context.Ply > 0 &&
            depth > 1 &&
            alpha==beta-1 &&
            !inCheck &&
            !context.Board.History[context.Ply - 1].IsNullMove &&
            myPieceCount > 0 &&
            (myPieceCount > 2 || depth < 7))
        {
            context.Metrics.NullMoveTries++;
            MakeNullMove(context);
            var nullReductionDepth = depth > 6 ? 4 : 3;
            int nmScore;
            if (depth - nullReductionDepth - 1 > 0)
                nmScore = -Search(context,-beta, 1 - beta, depth - nullReductionDepth - 1);
            else
                nmScore = -Quiesce(context,-beta, 1 - beta);
            UnmakeNullMove(context);

            if (MyGameState.TimeUp)
                return alpha;

            if (nmScore >= beta)
            {
                context.Metrics.NullMoveFailHigh++;
                TranspositionTable.Instance.Store(context.Board.HashKey, Move.Null, depth, nmScore, EntryType.CUT, context.Ply);
                return nmScore;
            }

            //giving the opponent a free move let them find a mate score against
            //us - that's a genuine mate threat, not just a quiet position, so
            //don't let LMR/futility pruning skip past our defense below
            if (nmScore <= -9900)
            {
                context.Metrics.MateThreats++;
                mateThreat = 1;
            }
        }

        Span<Move> moveBuffer = stackalloc Move[MoveList.StackCapacity];
        var moves = new MoveList(moveBuffer);
        MoveGenerator.GenerateMoves(context.Board, ref moves);
        OrderMoves(context,ref moves, hasEntry, entry);
        Move lastMove = Move.Null;
        int lmr = 0, nonCaptureMoves = 0, movesSearched = 0;

        for (int moveIndex = 0; moveIndex < moves.Count; moveIndex++)
        {
            var m = moves[moveIndex];
            bool fprune = false;
            int score;
            if (!context.Make(m))
                continue;

            var justGaveCheck = context.Board.InCheck(context.Board.SideToMove);
            var capture = ((m.Bits & (byte)MoveBits.Capture) != 0);
            if (!capture && (!hasEntry || entry.MoveValue!=m.Value)) // don't count the hash move as a non-capture
                ++nonCaptureMoves;                                     // while it might not be a capture, the point 
                                                                       // here is to start counting after generated captures
            var passedpawnpush = (m.Bits & (byte)MoveBits.Pawn) > 0 && (Evaluator.PassedPawnMask[context.Board.SideToMove^1,m.To] & context.Board.Pawns[context.Board.SideToMove]) == 0;                                               
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
                    if (depth == 2 && -Evaluator.EvaluateMaterial(context.Board) + Evaluator.PieceValues[(int)Piece.Rook] <= alpha)
                    {
                        context.Metrics.EFPrune++;
                        fprune = true;
                    }

                    else if (depth == 1 && -Evaluator.EvaluateMaterial(context.Board) + Evaluator.PieceValues[(int)Piece.Knight] <= alpha)
                    {
                        context.Metrics.FPrune++;
                        fprune = true;
                    }

                }
            }
            if (!fprune)
            {
                //if we don't yet have a move, then search full window (PV Node)
                if (bestMove.IsNull)
                    score = -Search(context,-beta, -alpha, depth - 1 - lmr + ext);
                else //otherwise, use a zero window
                {
                    //zero window search
                    score = -Search(context,-alpha - 1, -alpha, depth - 1 - lmr + ext);

                    if (score > alpha)
                    {
                        //this move might be better than our current best move
                        //we have to research with full window

                        score = -Search(context,-beta, -alpha, depth - 1 - lmr + ext);

                        if (score > alpha && lmr > 0)
                        {
                            //let's research again without the lmr
                            context.Metrics.LMRResearch++;
                            score = -Search(context,-beta, -alpha, depth - 1);
                        }
                    }
                }
            }
            else
            {
                score = -Quiesce(context,-beta, -alpha);
            }

            context.TakeBack();
            ++movesSearched;
            if (context.GameState.TimeUp)
                return alpha;

            if (score >= beta)
            {
                SearchFailHigh(context, m, score, depth, hasEntry, entry);
                if (lastMove.IsNull)
                    context.Metrics.FirstMoveFailHigh++;
                return score;
            }

            if (score > alpha)
            {
                alpha = score;
                bestMove = m;
                // PV Node
                //update the PV
                UpdatePv(context, bestMove);
                //Add to hashtable
                TranspositionTable.Instance.Store(
                    context.Board.HashKey, bestMove, depth, alpha, TranspositionTableEntry.EntryType.PV, context.Ply);
            }

            lastMove = m;
        }

        //check for mate
        if (lastMove.IsNull)
        {
            //we can't make a move. check for mate or stalemate.
            if (inCheck)
                return -10000 + context.Ply;
            else
                return CurrentDrawScore;
        }


        if (bestMove.IsNull)
        {
            //ALL NODE
            TranspositionTable.Instance.Store(
                context.Board.HashKey, Move.Null, depth, alpha,
                TranspositionTableEntry.EntryType.ALL, context.Ply);
        }

        return alpha;
    }

    private void UnmakeNullMove(AlphaBetaContext context)
    {
        var nullMove = context.Board.History[context.Board.History.Count - 1];
        context.Board.History.RemoveLast();

        if (nullMove.EnPassant > 0)
        {
            context.Board.EnPassant = nullMove.EnPassant;
            var file = context.Board.EnPassant.BitScanForward().File();
            context.Board.HashKey ^= TranspositionTable.EnPassantFileKey[file];
        }
        context.Ply--;

        context.Board.HashKey^=TranspositionTable.SideToMoveKey;
        context.Board.SideToMove ^= 1;
    }

    private void MakeNullMove(AlphaBetaContext context)
    {
        var nullMove = HistoryMove.NullMove(context.Board.HashKey);

        context.Board.SideToMove ^= 1;
        context.Board.HashKey^=TranspositionTable.SideToMoveKey;

        context.Ply++;
        if (context.Board.EnPassant > 0)
        {
            nullMove.EnPassant = context.Board.EnPassant;
            var file = context.Board.EnPassant.BitScanForward().File();
            context.Board.HashKey ^= TranspositionTable.EnPassantFileKey[file];
            context.Board.EnPassant = 0;
        }

        context.Board.History.Add(nullMove);//store a null move in history
    }

    private void UpdatePv(AlphaBetaContext context, Move bestMove)
    {
        context.PrincipalVariation[context.Ply, context.Ply] = bestMove;

        for (int i = context.Ply + 1; i < context.PvLength[context.Ply + 1]; i++)
            context.PrincipalVariation[context.Ply, i] = context.PrincipalVariation[context.Ply + 1, i];
        context.PvLength[context.Ply] = context.PvLength[context.Ply + 1];
    }

    void NewRootMove(Move m){
        //we need to move this one to the top of root moves
        RootMoves.Remove(m);
        RootMoves.Insert(0,m);
    }


    int OrderRootMove(AlphaBetaContext context, Move m)
    {

        //use the principal variation move first
        if (!context.PrincipalVariation[0, 0].IsNull && context.PrincipalVariation[0, 0].Value == m.Value)
            return int.MaxValue;

        // otherwise we will use Qsearch to order the moves
        if (!context.Board.MakeMove(m))
            return int.MinValue;

        var score = AlphaBeta.Quiesce(context, -10000, 10000);

        context.Board.UnMakeMove();
        return score;

    }

// Returns (tier, score): moves are sorted by tier first, then score
    // within that tier - avoids needing every tier's numeric range to be
    // hand-verified against every other tier's (a single combined int score
    // would need history's accumulated counts to never grow large enough to
    // spill into the killer/capture ranges above it).
    (int Tier, int Score) OrderMove(AlphaBetaContext context, Move m, bool hasEntry, in TranspositionTableEntry entry)
    {
        if (hasEntry && m.Value == entry.MoveValue)
            return (4, 0); // search this move first

        if ((m.Bits & (byte)MoveBits.Capture) > 0)
        {

            //winning and even captures
            if(Evaluator.PieceValueOnSquare(context.Board, m.To) >= Evaluator.PieceValues[(int)Move.GetPiece((MoveBits)m.Bits)])
                return (3, AlphaBeta.LvaMvv(context.Board, m));
            else
            {
                //verify that it is actually losing with SEE
                if(StaticExchange.Eval(context.Board,m, context.Board.SideToMove) >= 0)
                    return (3, AlphaBeta.LvaMvv(context.Board, m));
                return (0, AlphaBeta.LvaMvv(context.Board, m));//losing captures at the bottom
            }

        }
        else
        {
            //killers come before history-ordered quiets
            if (!context.Killers[context.Ply, 0].IsNull && context.Killers[context.Ply, 0].Value == m.Value)
                return (2, 1);
            if (!context.Killers[context.Ply, 1].IsNull && context.Killers[context.Ply, 1].Value == m.Value)
                return (2, 0);

            return (1, context.HistoryHeuristic[context.Board.SideToMove, (int)Move.GetPiece((MoveBits)m.Bits) - 1, m.To]);
        }
    }

    void OrderMoves(AlphaBetaContext context, ref MoveList moves, bool hasEntry, in TranspositionTableEntry entry)
    {
        var count = moves.Count;
        Span<int> tiers = count <= 256 ? stackalloc int[count] : new int[count];
        Span<int> scores = count <= 256 ? stackalloc int[count] : new int[count];

        for (int i = 0; i < count; i++)
        {
            var order = OrderMove(context, moves[i], hasEntry, entry);
            tiers[i] = order.Tier;
            scores[i] = order.Score;
        }

        for (int i = 1; i < count; i++)
        {
            var move = moves[i];
            var tier = tiers[i];
            var score = scores[i];
            var j = i - 1;

            while (j >= 0 && IsMoveScoreLess(tiers[j], scores[j], tier, score))
            {
                moves[j + 1] = moves[j];
                tiers[j + 1] = tiers[j];
                scores[j + 1] = scores[j];
                j--;
            }

            moves[j + 1] = move;
            tiers[j + 1] = tier;
            scores[j + 1] = score;
        }
    }

    static bool IsMoveScoreLess(int leftTier, int leftScore, int rightTier, int rightScore)
    {
        return leftTier < rightTier || (leftTier == rightTier && leftScore < rightScore);
    }

    void SearchFailHigh(AlphaBetaContext context, Move m, int score, int depth, bool hasEntry, in TranspositionTableEntry entry)
    {
        UpdateHeuristics(context, m, depth);
        context.Metrics.FailHigh++;

        if (hasEntry && entry.MoveValue == m.Value)
            context.Metrics.TTFailHigh++;

        else if ((!context.Killers[context.Ply, 0].IsNull && m.Value == context.Killers[context.Ply, 0].Value) ||
            (!context.Killers[context.Ply, 1].IsNull && m.Value == context.Killers[context.Ply, 1].Value))
            context.Metrics.KillerFailHigh++;

        //update the transposition table
        //the move doesn't matter if it is a CUT node
        TranspositionTable.Instance.Store(
            context.Board.HashKey, m, depth, score, TranspositionTableEntry.EntryType.CUT, context.Ply);
    }

    private void UpdateHeuristics(AlphaBetaContext context, Move m, int depth)
    {

        if ((m.Bits & (byte)MoveBits.Capture) > 0)
            return;

        context.HistoryHeuristic[context.Board.SideToMove, (int)Move.GetPiece((MoveBits)m.Bits) - 1, m.To] += depth * depth;

        if (!context.Killers[context.Ply, 1].IsNull && context.Killers[context.Ply, 1].Value != m.Value)
            context.Killers[context.Ply, 0] = context.Killers[context.Ply, 1];

        context.Killers[context.Ply, 1] = m;
    }
}
