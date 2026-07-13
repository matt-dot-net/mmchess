using System.Collections.Generic;
using Xunit;

namespace mmchess;

public class PvsResearchTests
{
    [Fact]
    public void ScoutFailLowStopsWithoutResearch()
    {
        var calls = new List<SearchCall>();

        var result = PvsResearch.Execute(
            initialScore: 0,
            scoutRequired: true,
            alpha: 10,
            beta: 20,
            searchDepth: 5,
            unreducedDepth: 7,
            reduction: 2,
            (kind, alpha, beta, depth) =>
            {
                calls.Add(new SearchCall(kind, alpha, beta, depth));
                return 10;
            });

        Assert.Equal(10, result.Score);
        Assert.True(result.ScoutSearched);
        Assert.False(result.FullWindowSearched);
        Assert.False(result.UnreducedSearched);
        Assert.Equal(
            new[] { new SearchCall(PvsResearchKind.Scout, 10, 11, 5) },
            calls);
    }

    [Fact]
    public void ScoutFailHighRunsFullWindowResearch()
    {
        var calls = new List<SearchCall>();
        var scores = new Queue<int>(new[] { 11, 10 });

        var result = PvsResearch.Execute(
            initialScore: 0,
            scoutRequired: true,
            alpha: 10,
            beta: 20,
            searchDepth: 5,
            unreducedDepth: 5,
            reduction: 0,
            (kind, alpha, beta, depth) =>
            {
                calls.Add(new SearchCall(kind, alpha, beta, depth));
                return scores.Dequeue();
            });

        Assert.Equal(10, result.Score);
        Assert.True(result.ScoutSearched);
        Assert.True(result.FullWindowSearched);
        Assert.False(result.UnreducedSearched);
        Assert.Equal(
            new[]
            {
                new SearchCall(PvsResearchKind.Scout, 10, 11, 5),
                new SearchCall(PvsResearchKind.FullWindow, 10, 20, 5)
            },
            calls);
    }

    [Fact]
    public void CurrentWorkerFailHighSkipsDuplicateScout()
    {
        var calls = new List<SearchCall>();

        var result = PvsResearch.Execute(
            initialScore: 11,
            scoutRequired: false,
            alpha: 10,
            beta: 20,
            searchDepth: 5,
            unreducedDepth: 5,
            reduction: 0,
            (kind, alpha, beta, depth) =>
            {
                calls.Add(new SearchCall(kind, alpha, beta, depth));
                return 12;
            });

        Assert.Equal(12, result.Score);
        Assert.False(result.ScoutSearched);
        Assert.True(result.FullWindowSearched);
        Assert.Equal(
            new[] { new SearchCall(PvsResearchKind.FullWindow, 10, 20, 5) },
            calls);
    }

    [Fact]
    public void ReducedFailHighRunsUnreducedResearch()
    {
        var calls = new List<SearchCall>();
        var scores = new Queue<int>(new[] { 11, 12, 13 });

        var result = PvsResearch.Execute(
            initialScore: 0,
            scoutRequired: true,
            alpha: 10,
            beta: 20,
            searchDepth: 5,
            unreducedDepth: 7,
            reduction: 2,
            (kind, alpha, beta, depth) =>
            {
                calls.Add(new SearchCall(kind, alpha, beta, depth));
                return scores.Dequeue();
            });

        Assert.Equal(13, result.Score);
        Assert.True(result.ScoutSearched);
        Assert.True(result.FullWindowSearched);
        Assert.True(result.UnreducedSearched);
        Assert.Equal(
            new[]
            {
                new SearchCall(PvsResearchKind.Scout, 10, 11, 5),
                new SearchCall(PvsResearchKind.FullWindow, 10, 20, 5),
                new SearchCall(PvsResearchKind.Unreduced, 10, 20, 7)
            },
            calls);
    }

    readonly record struct SearchCall(
        PvsResearchKind Kind,
        int Alpha,
        int Beta,
        int Depth);
}
