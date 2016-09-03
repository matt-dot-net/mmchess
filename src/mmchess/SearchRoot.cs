using System;

namespace mmchess
{
    public static class SearchRoot
    {
        public static void Iterate(Board b)
        {
            AlphaBeta ab = new AlphaBeta(b, TimeSpan.FromSeconds(5));
            int alpha = -10000;
            int beta = 10000;

            Console.WriteLine("Ply\tScore\tNodes\tPV");
            for (int i = 0; i < 64 && !ab.TimeUp; i++)
            {
                int score;
                if (i > 0)
                {
                    beta = alpha + 33;
                    alpha = alpha - 33;
                }

                do
                {
                    score = ab.Search(alpha, beta, i);
                    if (score > alpha && score < beta)
                    {
                        alpha = score;
                        break;
                    }

                    if (score >= beta)
                    {
                        beta = 10000;
                        continue;
                    }

                    else if (score <= alpha)
                        alpha = -10000;

                } while (!ab.TimeUp);
                if (!ab.TimeUp)
                {
                    Console.Write("{0}\t{1}\t{2}\t", i, score, ab.Nodes);
                    if (ab.PrincipalVariation[0] != null)
                    {
                        foreach (var m in ab.PrincipalVariation[0])
                        {
                            Console.Write("{0} ", m.ToAlegbraicNotation(b));
                            b.MakeMove(m);
                        }
                        foreach (var m in ab.PrincipalVariation[0])
                            b.UnMakeMove();
                    }

                }
                Console.WriteLine();

                if(Math.Abs(score) > 9900)
                    break;
            }
            b.MakeMove(ab.PrincipalVariation[0][0]);
        }
    }
}