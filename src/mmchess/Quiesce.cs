using System.Linq;

namespace mmchess;

public partial class AlphaBeta
{
    public const int MaxCheckChaseDepth = 6;

    // "Non-capture" means something different depending on check status:
    // - Not in check: GenerateQuiescenceMoves only ever returns captures,
    //   plus quiet (non-capturing) promotion pushes - those still deserve
    //   LvaMvv's promotion-value ordering, same as always.
    // - In check: non-captures are real evasions (king flight or blocks), which
    //   LvaMvv doesn't meaningfully order at all (it'd rank them by the moving
    //   piece's own value, an accident of an empty destination square scoring 0).
    //   Order captures - including capturing the checking piece - first by
    //   LvaMvv, then king flight, then blocking moves last.
    int OrderQuiesceMove(Move m, bool inCheck)
    {
        if ((m.Bits & (byte)MoveBits.Capture) != 0)
            return 200_000 + LvaMvv(m);

        if (!inCheck)
            return LvaMvv(m); // quiet promotion push - still rank by what it promotes to

        if ((m.Bits & (byte)MoveBits.King) != 0)
            return 100_000;

        return 0;
    }

    // checkChaseDepth counts consecutive quiescence plies spent resolving an
    // ongoing check (as opposed to chasing captures) - it resets to 0 whenever
    // we're not in check. Removing the (unsound) stand-pat shortcut while in
    // check means a long forcing sequence of checks can no longer bail out
    // early via the static eval, so this bounds how many plies of full evasion
    // search we'll do before falling back to the static eval anyway - a much
    // narrower, standard heuristic than "always trust stand-pat in check".
    public int Quiesce(int alpha, int beta, int checkChaseDepth = 0)
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
        //attempt to stand pat (don't search if eval tells us we are in a good position)
        //only valid when not in check - you can't "pass" and decline to respond to check
        var inCheck = MyBoard.InCheck(MyBoard.SideToMove);

        //we've chased a forcing check sequence too long - cut our losses and
        //fall back to the static eval rather than let it balloon unboundedly
        if (inCheck && checkChaseDepth >= MaxCheckChaseDepth)
            return score;

        if (!inCheck)
        {
            if(score > alpha)
                alpha = score;
            if (score >= beta)
                return beta;

            //Don't bother searching if we are evaluating at less than a Queen
            if (score < alpha - Evaluator.PieceValues[(int)Piece.Queen])
                return alpha;

        }

        var moves = MoveGenerator
            .GenerateQuiescenceMoves(MyBoard,false)
            .OrderByDescending((m) => OrderQuiesceMove(m, inCheck));

        var nextCheckChaseDepth = inCheck ? checkChaseDepth + 1 : 0;
        bool anyMoveTried = false;

        foreach (var m in moves)
        {
            if((BitMask.Mask[m.To] & (MyBoard.King[0]|MyBoard.King[1])) > 0)
                return beta;

            //if SEE says this is a losing capture, we prune it
            if(StaticExchange.Eval(MyBoard,m,MyBoard.SideToMove) < 0)
                continue;

            anyMoveTried = true;
            MyBoard.MakeMove(m,false);
            Ply++;

            score = -Quiesce(-beta, -alpha, nextCheckChaseDepth);
            TakeBack();
            if (score >= beta)
                return beta;
            if (score > alpha)
                alpha = score;

        }

        //if we're in check and never found a legal evasion to even try, this is
        //checkmate - without this, we'd silently fall through to "return alpha",
        //returning whatever bound happened to be passed in rather than a real,
        //ply-adjusted mate score, which can corrupt scores propagated back up
        //through the whole search (see: engine emitting no move at all in a
        //genuinely lost-but-not-yet-detected position)
        if (inCheck && !anyMoveTried)
            return -10000 + Ply;

        return alpha;

    }
}
