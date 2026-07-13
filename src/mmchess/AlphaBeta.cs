using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static mmchess.TranspositionTableEntry;

namespace mmchess;

public partial class AlphaBeta
{
    const int MinSplitDepth = 12;
    const int MinSplitMovesRemaining = 3;
    const int MaxSplitNesting = 1;

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

        if (context.Board.History.IsGameDrawn(context.Board.HashKey) ||
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

        if (!TrySearchFirstRootMoveSynchronously(
            context, alpha, beta, depth, out var firstMove, out score, out var nextMoveIndex))
        {
            if (inCheck)
                return -10000 + context.Ply;
            return CurrentDrawScore;
        }

        lastMove = firstMove;
        if (MyGameState.TimeUp)
            return alpha;

        if (score >= beta)
        {
            NewRootMove(firstMove);
            context.PvLength[0] = 1;
            context.PrincipalVariation[0,0] = firstMove;
            return score;
        }

        if (score > alpha)
        {
            alpha = score;
            bestMove = firstMove;
            UpdatePv(context, bestMove);
            TranspositionTable.Instance.Store(context, firstMove, depth, alpha, EntryType.PV);
        }

        if (MyGameState.SearchScheduler.IsEnabled)
        {
            if (SearchRemainingRootMovesParallel(
                context, nextMoveIndex, beta, depth, ref alpha, ref bestMove, ref lastMove, out score))
                return score;
        }
        else
        {
            for (var moveIndex = nextMoveIndex; moveIndex < RootMoves.Count; moveIndex++)
            {
                var move = RootMoves[moveIndex];
                if (!TrySearchRootMoveSequential(context, move, alpha, beta, depth, bestMove, out score))
                    continue;

                if (ApplyRootMoveResult(
                    context, move, score, beta, depth, ref alpha, ref bestMove, ref lastMove, out var result))
                    return result;
            }
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

    bool SearchRemainingRootMovesParallel(
        AlphaBetaContext context,
        int nextMoveIndex,
        int beta,
        int depth,
        ref int alpha,
        ref Move bestMove,
        ref Move lastMove,
        out int terminalScore)
    {
        using var results = new BlockingCollection<SplitResult>();
        var localMoves = new List<Move>();
        var splitPoint = new SplitPoint();
        var scheduledCount = 0;
        var alphaSnapshot = alpha;
        context.Metrics.SplitPointsCreated++;
        context.Metrics.MaxSplitNesting = Math.Max(
            context.Metrics.MaxSplitNesting, context.SplitNesting + 1);

        for (var moveIndex = nextMoveIndex; moveIndex < RootMoves.Count && !MyGameState.TimeUp; moveIndex++)
        {
            var move = RootMoves[moveIndex];
            if (!context.Make(move))
                continue;

            var workerContext = context.Split(splitPoint.Stop);
            context.TakeBack();
            var work = new SplitWork(
                move, depth - 1, depth - 1, 0, alphaSnapshot, workerContext, splitPoint);

            if (MyGameState.SearchScheduler.TrySchedule(() => RunSplitScout(work, results)))
            {
                context.Metrics.WorkItemsScheduled++;
                scheduledCount++;
            }
            else
            {
                localMoves.Add(move);
            }
        }

        context.SplitNesting++;
        var hasTerminalScore = false;
        terminalScore = alpha;
        foreach (var move in localMoves)
        {
            if (hasTerminalScore || MyGameState.TimeUp)
                break;
            if (!TrySearchRootMoveSequential(context, move, alpha, beta, depth, bestMove, out var score))
                continue;

            hasTerminalScore = ApplyRootMoveResult(
                context, move, score, beta, depth, ref alpha, ref bestMove, ref lastMove, out terminalScore);
            if (hasTerminalScore)
            {
                splitPoint.Cancel();
                if (!MyGameState.TimeUp && score >= beta)
                    context.Metrics.ParallelBetaCutoffs++;
            }
        }

        if (MyGameState.TimeUp)
            splitPoint.Cancel();

        for (var resultIndex = 0; resultIndex < scheduledCount; resultIndex++)
        {
            var result = results.Take();
            context.JoinMetrics(result.Metrics, result.TTMetrics);
            if (result.Exception != null)
                throw new InvalidOperationException("Parallel root scout failed.", result.Exception);
            if (result.Cancelled)
                continue;
            if (hasTerminalScore || MyGameState.TimeUp)
                continue;

            int score;
            if (result.Score <= result.AlphaSnapshot)
            {
                context.Metrics.WorkerFailLows++;
                lastMove = result.Move;
                continue;
            }

            context.Metrics.WorkerFailHighCandidates++;
            if (!TrySearchRootScoutCandidate(context, result, alpha, beta, depth, out score))
                continue;
            if (score <= alpha && alpha != result.AlphaSnapshot)
                context.Metrics.CandidatesInvalidatedByAlpha++;

            hasTerminalScore = ApplyRootMoveResult(
                context, result.Move, score, beta, depth, ref alpha, ref bestMove, ref lastMove, out terminalScore);
            if (hasTerminalScore)
            {
                splitPoint.Cancel();
                if (!MyGameState.TimeUp && score >= beta)
                    context.Metrics.ParallelBetaCutoffs++;
            }
        }

        if (MyGameState.TimeUp)
        {
            terminalScore = alpha;
            return true;
        }
        return hasTerminalScore;
    }

    void RunSplitScout(
        SplitWork work,
        BlockingCollection<SplitResult> results)
    {
        Exception exception = null;
        var score = work.AlphaSnapshot;
        try
        {
            work.Context.Metrics.WorkItemsStarted++;
            if (work.SplitPoint.IsClosed)
            {
                work.Context.Metrics.WorkItemsSkipped++;
            }
            else
            {
                var workerSearch = new AlphaBeta(MyGameState)
                {
                    CurrentDrawScore = CurrentDrawScore
                };
                score = work.SearchDepth > 0
                    ? -workerSearch.Search(
                        work.Context, -work.AlphaSnapshot - 1, -work.AlphaSnapshot, work.SearchDepth)
                    : -Quiesce(work.Context, -work.AlphaSnapshot - 1, -work.AlphaSnapshot);
            }
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        finally
        {
            var cancelled = work.Context.StopRequested;
            if (cancelled)
                work.Context.Metrics.WorkItemsCancelled++;
            work.Context.Metrics.WorkItemsCompleted++;
            results.Add(new SplitResult(
                work.Move,
                score,
                work.AlphaSnapshot,
                work.SearchDepth,
                work.UnreducedDepth,
                work.Reduction,
                work.Context.Metrics,
                work.Context.TTMetrics,
                exception,
                cancelled));
        }
    }

    bool TrySearchRootScoutCandidate(
        AlphaBetaContext context,
        SplitResult result,
        int alpha,
        int beta,
        int depth,
        out int score)
    {
        if (!context.Make(result.Move))
        {
            score = alpha;
            return false;
        }

        var research = PvsResearch.Execute(
            result.Score,
            alpha != result.AlphaSnapshot,
            alpha,
            beta,
            depth - 1,
            depth - 1,
            reduction: 0,
            (_, searchAlpha, searchBeta, searchDepth) =>
                SearchRootChild(context, searchAlpha, searchBeta, searchDepth));
        score = research.Score;
        if (research.FullWindowSearched)
            context.Metrics.FullWindowResearches++;

        context.TakeBack();
        return true;
    }

    bool TrySearchRootMoveSequential(
        AlphaBetaContext context,
        Move move,
        int alpha,
        int beta,
        int depth,
        Move bestMove,
        out int score)
    {
        if (!context.Make(move))
        {
            score = alpha;
            return false;
        }

        if (depth > 0 && !bestMove.IsNull)
        {
            score = PvsResearch.Execute(
                alpha,
                scoutRequired: true,
                alpha,
                beta,
                depth - 1,
                depth - 1,
                reduction: 0,
                (_, searchAlpha, searchBeta, searchDepth) =>
                    SearchRootChild(context, searchAlpha, searchBeta, searchDepth)).Score;
        }
        else
        {
            score = depth > 0
                ? -Search(context, -beta, -alpha, depth - 1)
                : -Quiesce(context, -beta, -alpha);
        }

        context.TakeBack();
        return true;
    }

    bool ApplyRootMoveResult(
        AlphaBetaContext context,
        Move move,
        int score,
        int beta,
        int depth,
        ref int alpha,
        ref Move bestMove,
        ref Move lastMove,
        out int terminalScore)
    {
        lastMove = move;
        terminalScore = alpha;
        if (MyGameState.TimeUp)
            return true;

        if (score >= beta)
        {
            NewRootMove(move);
            context.PvLength[0] = 1;
            context.PrincipalVariation[0,0] = move;
            terminalScore = score;
            return true;
        }

        if (score > alpha)
        {
            alpha = score;
            bestMove = move;
            UpdatePv(context, bestMove);
            TranspositionTable.Instance.Store(context, move, depth, alpha, EntryType.PV);
        }
        return false;
    }

    sealed class SplitWork
    {
        public Move Move { get; }
        public int SearchDepth { get; }
        public int UnreducedDepth { get; }
        public int Reduction { get; }
        public int AlphaSnapshot { get; }
        public AlphaBetaContext Context;
        public SplitPoint SplitPoint { get; }

        public SplitWork(
            Move move,
            int searchDepth,
            int unreducedDepth,
            int reduction,
            int alphaSnapshot,
            AlphaBetaContext context,
            SplitPoint splitPoint)
        {
            Move = move;
            SearchDepth = searchDepth;
            UnreducedDepth = unreducedDepth;
            Reduction = reduction;
            AlphaSnapshot = alphaSnapshot;
            Context = context;
            SplitPoint = splitPoint;
        }
    }

    sealed class SplitPoint
    {
        public SearchStop Stop { get; } = new SearchStop();
        public bool IsClosed => Stop.IsRequested;

        public void Cancel()
        {
            Stop.Request();
        }
    }

    sealed class SplitResult
    {
        public Move Move { get; }
        public int Score { get; }
        public int AlphaSnapshot { get; }
        public int SearchDepth { get; }
        public int UnreducedDepth { get; }
        public int Reduction { get; }
        public AlphaBetaMetrics Metrics { get; }
        public TTMetrics TTMetrics { get; }
        public Exception Exception { get; }
        public bool Cancelled { get; }

        public SplitResult(
            Move move,
            int score,
            int alphaSnapshot,
            int searchDepth,
            int unreducedDepth,
            int reduction,
            AlphaBetaMetrics metrics,
            TTMetrics ttMetrics,
            Exception exception,
            bool cancelled)
        {
            Move = move;
            Score = score;
            AlphaSnapshot = alphaSnapshot;
            SearchDepth = searchDepth;
            UnreducedDepth = unreducedDepth;
            Reduction = reduction;
            Metrics = metrics;
            TTMetrics = ttMetrics;
            Exception = exception;
            Cancelled = cancelled;
        }
    }

    bool TrySearchFirstRootMoveSynchronously(
        AlphaBetaContext context,
        int alpha,
        int beta,
        int depth,
        out Move firstMove,
        out int score,
        out int nextMoveIndex)
    {
        for (var moveIndex = 0; moveIndex < RootMoves.Count; moveIndex++)
        {
            var move = RootMoves[moveIndex];
            if (!context.Make(move))
                continue;

            score = depth > 0
                ? -Search(context, -beta, -alpha, depth - 1)
                : -Quiesce(context, -beta, -alpha);
            context.TakeBack();

            firstMove = move;
            nextMoveIndex = moveIndex + 1;
            return true;
        }

        firstMove = Move.Null;
        score = alpha;
        nextMoveIndex = RootMoves.Count;
        return false;
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

        if (context.Board.History.IsPositionDrawn(context.Board.HashKey) ||
            context.Board.IsInsufficientMaterial())
            return CurrentDrawScore;

        if (context.StopRequested)
        {
            return alpha;
        }

        Move bestMove = Move.Null;
        if (depth+ext <= 0)
            return Quiesce(context, alpha, beta);

        //first let's look for a transposition
        var hasEntry = TranspositionTable.Instance.TryProbe(context, out var entry);
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

            if (context.StopRequested)
                return alpha;

            if (nmScore >= beta)
            {
                context.Metrics.NullMoveFailHigh++;
                TranspositionTable.Instance.Store(context, Move.Null, depth, nmScore, EntryType.CUT);
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
            if (context.StopRequested)
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
                    context, bestMove, depth, alpha, TranspositionTableEntry.EntryType.PV);
            }

            lastMove = m;

            if (movesSearched == 1 &&
                CanSplitInternal(context, depth, moves.Count - moveIndex - 1))
            {
                var remainingMoves = new Move[moves.Count - moveIndex - 1];
                for (var remainingIndex = 0; remainingIndex < remainingMoves.Length; remainingIndex++)
                    remainingMoves[remainingIndex] = moves[moveIndex + remainingIndex + 1];

                return SearchInternalRemainingParallel(
                    context,
                    remainingMoves,
                    alpha,
                    beta,
                    depth,
                    ext,
                    inCheck,
                    mateThreat,
                    hasEntry,
                    entry,
                    bestMove,
                    lastMove,
                    nonCaptureMoves,
                    movesSearched,
                    lmr);
            }
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
                context, Move.Null, depth, alpha,
                TranspositionTableEntry.EntryType.ALL);
        }

        return alpha;
    }

    bool CanSplitInternal(AlphaBetaContext context, int depth, int movesRemaining)
    {
        return depth >= MinSplitDepth &&
            movesRemaining >= MinSplitMovesRemaining &&
            context.SplitNesting < MaxSplitNesting &&
            !context.StopRequested &&
            MyGameState.SearchScheduler.HasIdleWorker;
    }

    int SearchInternalRemainingParallel(
        AlphaBetaContext context,
        Move[] remainingMoves,
        int alpha,
        int beta,
        int depth,
        int extension,
        bool inCheck,
        int mateThreat,
        bool hasEntry,
        in TranspositionTableEntry entry,
        Move bestMove,
        Move lastMove,
        int nonCaptureMoves,
        int movesSearched,
        int lmr)
    {
        using var results = new BlockingCollection<SplitResult>();
        var localWork = new List<SplitWork>();
        var splitPoint = new SplitPoint();
        var alphaSnapshot = alpha;
        var scheduledCount = 0;
        context.Metrics.SplitPointsCreated++;
        context.Metrics.MaxSplitNesting = Math.Max(
            context.Metrics.MaxSplitNesting, context.SplitNesting + 1);

        foreach (var move in remainingMoves)
        {
            if (context.StopRequested || !context.Make(move))
                continue;

            var justGaveCheck = context.Board.InCheck(context.Board.SideToMove);
            var capture = (move.Bits & (byte)MoveBits.Capture) != 0;
            if (!capture && (!hasEntry || entry.MoveValue != move.Value))
                nonCaptureMoves++;
            var passedPawnPush =
                (move.Bits & (byte)MoveBits.Pawn) > 0 &&
                (Evaluator.PassedPawnMask[context.Board.SideToMove ^ 1, move.To] &
                    context.Board.Pawns[context.Board.SideToMove]) == 0;

            if (extension == 0 &&
                !inCheck &&
                !justGaveCheck &&
                mateThreat == 0 &&
                !passedPawnPush &&
                nonCaptureMoves > 0)
            {
                lmr = movesSearched > 2 ? 2 : 1;
            }

            var searchDepth = depth - 1 - lmr + extension;
            var workerContext = context.Split(splitPoint.Stop);
            context.TakeBack();
            var work = new SplitWork(
                move,
                searchDepth,
                depth - 1,
                lmr,
                alphaSnapshot,
                workerContext,
                splitPoint);

            if (MyGameState.SearchScheduler.TrySchedule(() => RunSplitScout(work, results)))
            {
                context.Metrics.WorkItemsScheduled++;
                scheduledCount++;
            }
            else
            {
                localWork.Add(work);
            }
            movesSearched++;
        }

        context.SplitNesting++;
        var hasTerminalScore = false;
        var terminalScore = alpha;
        foreach (var work in localWork)
        {
            if (hasTerminalScore || context.StopRequested)
                break;
            if (!TrySearchInternalWorkSequential(
                context, work, alpha, beta, bestMove, out var score))
                continue;

            hasTerminalScore = ApplyInternalMoveResult(
                context,
                work.Move,
                score,
                beta,
                depth,
                hasEntry,
                entry,
                ref alpha,
                ref bestMove,
                ref lastMove,
                out terminalScore);
            if (hasTerminalScore)
            {
                splitPoint.Cancel();
                if (!MyGameState.TimeUp && score >= beta)
                    context.Metrics.ParallelBetaCutoffs++;
            }
        }

        if (context.StopRequested)
            splitPoint.Cancel();

        for (var resultIndex = 0; resultIndex < scheduledCount; resultIndex++)
        {
            var result = results.Take();
            context.JoinMetrics(result.Metrics, result.TTMetrics);
            if (result.Exception != null)
                throw new InvalidOperationException("Parallel internal scout failed.", result.Exception);
            if (result.Cancelled || hasTerminalScore || context.StopRequested)
                continue;

            if (result.Score <= result.AlphaSnapshot)
            {
                context.Metrics.WorkerFailLows++;
                lastMove = result.Move;
                continue;
            }

            context.Metrics.WorkerFailHighCandidates++;
            if (!TrySearchInternalScoutCandidate(context, result, alpha, beta, out var score))
                continue;
            if (score <= alpha && alpha != result.AlphaSnapshot)
                context.Metrics.CandidatesInvalidatedByAlpha++;

            hasTerminalScore = ApplyInternalMoveResult(
                context,
                result.Move,
                score,
                beta,
                depth,
                hasEntry,
                entry,
                ref alpha,
                ref bestMove,
                ref lastMove,
                out terminalScore);
            if (hasTerminalScore)
            {
                splitPoint.Cancel();
                if (!MyGameState.TimeUp && score >= beta)
                    context.Metrics.ParallelBetaCutoffs++;
            }
        }

        if (context.StopRequested)
            return alpha;
        if (hasTerminalScore)
            return terminalScore;
        if (bestMove.IsNull)
        {
            TranspositionTable.Instance.Store(
                context, Move.Null, depth, alpha, EntryType.ALL);
        }
        return alpha;
    }

    bool TrySearchInternalWorkSequential(
        AlphaBetaContext context,
        SplitWork work,
        int alpha,
        int beta,
        Move bestMove,
        out int score)
    {
        if (!context.Make(work.Move))
        {
            score = alpha;
            return false;
        }

        if (bestMove.IsNull)
        {
            score = SearchChild(context, alpha, beta, work.SearchDepth);
        }
        else
        {
            var research = PvsResearch.Execute(
                alpha,
                scoutRequired: true,
                alpha,
                beta,
                work.SearchDepth,
                work.UnreducedDepth,
                work.Reduction,
                (_, searchAlpha, searchBeta, searchDepth) =>
                    SearchChild(context, searchAlpha, searchBeta, searchDepth));
            score = research.Score;
            if (research.UnreducedSearched)
                context.Metrics.LMRResearch++;
        }

        context.TakeBack();
        return true;
    }

    bool TrySearchInternalScoutCandidate(
        AlphaBetaContext context,
        SplitResult result,
        int alpha,
        int beta,
        out int score)
    {
        if (!context.Make(result.Move))
        {
            score = alpha;
            return false;
        }

        var research = PvsResearch.Execute(
            result.Score,
            alpha != result.AlphaSnapshot,
            alpha,
            beta,
            result.SearchDepth,
            result.UnreducedDepth,
            result.Reduction,
            (_, searchAlpha, searchBeta, searchDepth) =>
                SearchChild(context, searchAlpha, searchBeta, searchDepth));
        score = research.Score;
        if (research.FullWindowSearched)
            context.Metrics.FullWindowResearches++;
        if (research.UnreducedSearched)
            context.Metrics.LMRResearch++;

        context.TakeBack();
        return true;
    }

    int SearchChild(AlphaBetaContext context, int alpha, int beta, int depth)
    {
        return depth > 0
            ? -Search(context, -beta, -alpha, depth)
            : -Quiesce(context, -beta, -alpha);
    }

    int SearchRootChild(AlphaBetaContext context, int alpha, int beta, int depth)
    {
        return depth >= 0
            ? -Search(context, -beta, -alpha, depth)
            : -Quiesce(context, -beta, -alpha);
    }

    bool ApplyInternalMoveResult(
        AlphaBetaContext context,
        Move move,
        int score,
        int beta,
        int depth,
        bool hasEntry,
        in TranspositionTableEntry entry,
        ref int alpha,
        ref Move bestMove,
        ref Move lastMove,
        out int terminalScore)
    {
        lastMove = move;
        terminalScore = alpha;
        if (context.StopRequested)
            return true;

        if (score >= beta)
        {
            SearchFailHigh(context, move, score, depth, hasEntry, entry);
            terminalScore = score;
            return true;
        }

        if (score > alpha)
        {
            alpha = score;
            bestMove = move;
            UpdatePv(context, bestMove);
            TranspositionTable.Instance.Store(context, bestMove, depth, alpha, EntryType.PV);
        }
        return false;
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
        TranspositionTable.Instance.Store(context, m, depth, score, EntryType.CUT);
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
