using System;

namespace mmchess
{
    public static class SearchRoot
    {
        static void PrintMetrics(AlphaBetaMetrics metrics){
            Console.WriteLine("Nodes={0}, QNodes={1}, Qsearch%={2:0.0}",metrics.Nodes,metrics.QNodes,
                100*(float)metrics.QNodes/((float)metrics.Nodes+1));
            Console.WriteLine("FH%={0}, Killers%={1}",
                100*(float)metrics.FirstMoveFailHigh/((float)metrics.FailHigh+1),
                100*(float)metrics.KillerFailHigh/((float)metrics.FailHigh+1));
            Console.WriteLine("----Transposition Table----");
            Console.WriteLine("Collisions={0}, Collide%={1:0.0}, Hits={2}",TranspositionTable.Instance.Collisions,
                (double)TranspositionTable.Instance.Collisions/
                (double)TranspositionTable.Instance.Stores+1, TranspositionTable.Instance.Hits);
        }
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
                    Console.Write("{0}\t{1}\t{2}\t", i, score, ab.Metrics.Nodes);
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
            PrintMetrics(ab.Metrics);
            b.MakeMove(ab.PrincipalVariation[0][0]);
        }
    }
}