using System;

namespace mmchess
{
    public static class SearchRoot
    {
        static void PrintMetrics(AlphaBetaMetrics metrics){
            Console.WriteLine("Nodes={0}, QNodes={1}, Qsearch%={2:0.0}, Knps={3}",
                metrics.Nodes,
                metrics.QNodes,
                100*(double)metrics.QNodes/((double)metrics.Nodes+1),
                (metrics.Nodes/1000/5));
            Console.WriteLine("FirstMoveFH%={0:0.0}, Killers%={1:0.0}, NullMove%={2:0.0}",
                100*(double)metrics.FirstMoveFailHigh/((double)metrics.FailHigh+1),
                100*(double)metrics.KillerFailHigh/((double)metrics.FailHigh+1),
                100%(double)metrics.NullMoveFailHigh/((double)metrics.FirstMoveFailHigh+1));
            Console.WriteLine("HashTable: Collisions={0}, Hits={1}",
                TranspositionTable.Instance.Collisions,
                TranspositionTable.Instance.Hits);
        }

        static TimeSpan GetThinkTimeSpan(GameState state){
            return TimeSpan.FromSeconds(5);
        }

        public static void Iterate(GameState state, Action interrupt)
        {
            state.TimeUp=false;
            var startTime = DateTime.Now;
            var timeLimit = GetThinkTimeSpan(state);
            AlphaBeta ab = new AlphaBeta(state,()=>{
                if ((DateTime.Now - startTime) > timeLimit)
                    state.TimeUp = true;
                    interrupt();
            });
            int alpha = -10000;
            int beta = 10000;
            Move bestMove=null;
            //Console.WriteLine("Ply\tScore\tMillis\tNodes\tPV");
            for (int i = 0; i < 64 && !state.TimeUp; i++)
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

                } while (!state.TimeUp);
                if (!state.TimeUp)
                {
                    //must make sure this wasn't updated while running out of time
                    bestMove = ab.PrincipalVariation[0,0];               
                    Console.Write("{0}\t{1}\t{2:0}\t{3}\t", i, score, 
                        (DateTime.Now - startTime).TotalMilliseconds/10, ab.Metrics.Nodes);
                    PrintPV(state.GameBoard, ab);   
                }
                Console.WriteLine();

                if(Math.Abs(score) > 9900)
                    break;
            }
            PrintMetrics(ab.Metrics);
            Console.WriteLine("move {0}",bestMove.ToAlegbraicNotation(state.GameBoard));
            state.GameBoard.MakeMove(bestMove);
        }

        private static void PrintPV(Board b, AlphaBeta ab)
        {
            for (int j = 0; j < ab.PvLength[0]; j++)
            {
                var m = ab.PrincipalVariation[0, j];
                Console.Write("{0} ", m.ToAlegbraicNotation(b));
                b.MakeMove(m);
            }

            //Walk through the TT table to augment the PV
            var tempHashKey = b.HashKey;
            while (true)
            {
                var entry = TranspositionTable.Instance.Read(b.HashKey);
                if (entry == null || entry.Type != (byte)TranspositionTableEntry.EntryType.PV)
                    break;
                var m = new Move(entry.MoveValue);
                Console.Write("{0}(HT) ", m.ToAlegbraicNotation(b));
                b.MakeMove(m);
            }
            while (b.HashKey != tempHashKey)
                b.UnMakeMove();

            for (int j = ab.PvLength[0] - 1; j >= 0; j--)
                b.UnMakeMove();
                
        }
    }
}