using System;
using System.Collections.Generic;

namespace mmchess
{
    public static class Iterate
    {
        static void PrintMetrics(AlphaBetaMetrics metrics, TimeSpan searchTime)
        {
            //prevent index out of bounds.
            //note, this will not effect the calculation
            if (metrics.Depth == 0)
            {
                metrics.Depth = 1;
            }
            float ebf = 0;
            for (int d = 1; d < metrics.Depth; d++)
            {
                var bf = (float)metrics.DepthNodes[d] / (float)(metrics.DepthNodes[d - 1] + 1);
                if (ebf > 0)
                    ebf = (ebf + bf) / 2;
                else
                    ebf = bf;
            }
            Console.WriteLine("Nodes={0}, QNodes={1}, Qsearch%={2:0.0}, Knps={3:0}, EBF({4})={5:0.00}",
                metrics.Nodes,
                metrics.QNodes,
                100 * (double)metrics.QNodes / ((double)metrics.Nodes + 1),
                (metrics.Nodes / 1000 / searchTime.TotalSeconds),
                metrics.Depth,
                ebf);
            Console.WriteLine("FirstMoveFH%={0:0.0}, Killers%={1:0.0} FutilePrune={2}, EFutilePrune={3}",
                100 * (double)metrics.FirstMoveFailHigh / ((double)metrics.FailHigh + 1),
                100 * (double)metrics.KillerFailHigh / ((double)metrics.FailHigh + 1),
                metrics.FPrune,
                metrics.EFPrune);
            Console.WriteLine("NullMoveTries={0} NullMove%={1:0.0}, NMResearch={2}, MateThreats={3}, LMRResearch={4}",
                metrics.NullMoveTries,
                100 * (double)metrics.NullMoveFailHigh / ((double)metrics.NullMoveTries + 1),
                metrics.NullMoveResearch,
                metrics.MateThreats,
                metrics.LMRResearch);
            Console.WriteLine("HashTable: FH%={0:0.0} Hit%={1:0.0}",
                100*(double)metrics.TTFailHigh/(double)metrics.FirstMoveFailHigh+1,
                100*(double)TranspositionTable.Instance.Hits/(double)TranspositionTable.Instance.Probes);
        }

        static TimeSpan GetThinkTimeSpan(GameState state)
        {
            if (state == null)
                throw new ArgumentNullException("state");
            if (state.TimeControl == null)
                throw new ArgumentException("No Time Control set!");

            switch (state.TimeControl.Type)
            {
                case TimeControlType.FixedTimePerMove:
                    return TimeSpan.FromSeconds(state.TimeControl.FixedTimePerSearchSeconds);
                case TimeControlType.TimePerGame:
                    return TimeSpan.FromSeconds(
                            (state.MyClock.TotalSeconds / 40) +
                            (state.TimeControl.IncrementSeconds / 2)
                        );
                case TimeControlType.NumberOfMoves:
                    int moves = (state.GameBoard.History.Count / 2);
                    if (moves >= state.TimeControl.MovesInTimeControl)
                    {
                        //find the remainder
                        int timeControlsReached = moves / state.TimeControl.MovesInTimeControl;
                        moves -= state.TimeControl.MovesInTimeControl * timeControlsReached;
                    }
                    var movesRemaining = state.TimeControl.MovesInTimeControl - moves;
                    return TimeSpan.FromSeconds(state.MyClock.TotalSeconds / (movesRemaining+1));
                default:
                    return TimeSpan.MaxValue;
            }
        }

        public static Move DoIterate(GameState state, Action interrupt)
        {
            state.TimeUp = false;
            var startTime = DateTime.Now;
            var timeLimit = GetThinkTimeSpan(state);
            AlphaBeta ab = new AlphaBeta(state, () =>
            {
                if ((DateTime.Now - startTime) > timeLimit)
                    state.TimeUp = true;
                interrupt();
            });

            //increment transposition table search Id
            TranspositionTable.Instance.NextSearchId();

            int alpha = -10000;
            int beta = 10000;
            Move bestMove = null;
            //Console.WriteLine("Ply\tScore\tMillis\tNodes\tPV");
            ab.Metrics.Depth = 0;
            for (int i = 0; i < AlphaBeta.MAX_DEPTH && !state.TimeUp; i++)
            {
                int score;
                int alphaRelax = 1, betaRelax = 1;
                if (i > 0)
                {
                    beta = alpha + 33;
                    alpha = alpha - 33;
                }

                do
                {
                    score = ab.SearchRoot(alpha, beta, i);
                    bestMove=ab.PrincipalVariation[0,0];
                    
                    if(!state.TimeUp)
                        PrintSearchResult(state, startTime, ab, i, score);
                    if (score > alpha && score < beta)
                    {
                        alpha = score;
                        break;
                    }
                    else if (score >= beta)
                    {
                        if(score == 10000)
                            break;
                        
                        beta = Math.Min(10000, beta + (33 * betaRelax));
                        betaRelax *= 4;
                    }
                    else if (score <= alpha)
                    {
                        if(score == -10000)
                            break;
                        alpha = Math.Max(-10000, alpha - (33 * alphaRelax));
                        alphaRelax *= 4;
                    }
                } while (!state.TimeUp); //keep searching for a PV move until time is up

                if (!state.TimeUp)
                {
                    ab.Metrics.DepthNodes[i] = ab.Metrics.Nodes;
                    ab.Metrics.Depth = i;
                }

                if (Math.Abs(score) > 9900) // stop if we have found mate
                    break;
            }
            PrintMetrics(ab.Metrics, DateTime.Now - startTime);
            return bestMove;
        }

        private static void PrintSearchResult(GameState state, DateTime startTime, AlphaBeta ab, int i, int score)
        {
            Console.Write("{0}\t{1}\t{2:0}\t{3}\t", i, score,
                (DateTime.Now - startTime).TotalMilliseconds / 10, ab.Metrics.Nodes);
            PrintPV(state.GameBoard, ab);
            Console.WriteLine();
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
            int hashTableMoves = 0;
            List<ulong> hashKeys = new List<ulong>();
            while (true)
            {
                //don't let the hashtable send us into a cycle
                if (hashKeys.Contains(b.HashKey))
                    break;

                var entry = TranspositionTable.Instance.Read(b.HashKey);
                if (entry == null || entry.Type != (byte)TranspositionTableEntry.EntryType.PV)
                    break;
                var m = new Move(entry.MoveValue);
                Console.Write("{0}(HT) ", m.ToAlegbraicNotation(b));
                if (!b.MakeMove(m))
                    throw new Exception("invalid move from HT!");
                hashTableMoves++;
                hashKeys.Add(b.HashKey);
            }
            while (hashTableMoves-- > 0)
                b.UnMakeMove();

            for (int j = ab.PvLength[0] - 1; j >= 0; j--)
                b.UnMakeMove();

        }
    }
}