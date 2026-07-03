using System.Linq;

namespace mmchess;

public partial class AlphaBeta
{
    int OrderQuiesceMove(Move m)
    {
        return LvaMvv(m);
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
}
