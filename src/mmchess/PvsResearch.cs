using System;

namespace mmchess;

internal enum PvsResearchKind
{
    Scout,
    FullWindow,
    Unreduced
}

internal readonly record struct PvsResearchResult(
    int Score,
    bool ScoutSearched,
    bool FullWindowSearched,
    bool UnreducedSearched);

internal static class PvsResearch
{
    internal static PvsResearchResult Execute(
        int initialScore,
        bool scoutRequired,
        int alpha,
        int beta,
        int searchDepth,
        int unreducedDepth,
        int reduction,
        Func<PvsResearchKind, int, int, int, int> search)
    {
        var score = initialScore;
        var scoutSearched = false;
        var fullWindowSearched = false;
        var unreducedSearched = false;

        if (scoutRequired)
        {
            score = search(PvsResearchKind.Scout, alpha, alpha + 1, searchDepth);
            scoutSearched = true;
        }

        if (score > alpha)
        {
            score = search(PvsResearchKind.FullWindow, alpha, beta, searchDepth);
            fullWindowSearched = true;

            if (score > alpha && reduction > 0)
            {
                score = search(PvsResearchKind.Unreduced, alpha, beta, unreducedDepth);
                unreducedSearched = true;
            }
        }

        return new PvsResearchResult(
            score,
            scoutSearched,
            fullWindowSearched,
            unreducedSearched);
    }
}
