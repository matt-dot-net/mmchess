using System;
using System.Linq;

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
            Move pvMove = null;
            for(int i=0;i<64&&!ab.TimeUp;i++)
            {
                int score;
                do
                {
                    score = ab.Search(alpha, beta, i);
                    if(!ab.TimeUp && ab.PrincipalVariation.Count > 0)
                        pvMove = ab.PrincipalVariation[0];
                    if (score > alpha)
                        alpha = score;
                    else if (score <= alpha)
                        alpha = -10000;
                    else if (score > beta)
                        beta = 10000;
                } while (score < alpha || score > beta);
                if(!ab.TimeUp){
                    Console.Write("{0}\t{1}\t{2}\t",i,score,ab.Nodes);
                    foreach(var m in ab.PrincipalVariation){
                        Console.Write("{0} ",m.ToAlegbraicNotation(b));
                        b.MakeMove(m);
                    }
                    for(int u=0;u<ab.PrincipalVariation.Count;u++)
                        b.UnMakeMove();

                }
                Console.WriteLine();
            }
            b.MakeMove(pvMove);
        }
    }
}