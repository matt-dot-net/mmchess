using Xunit;

namespace mmchess.Test;

public class AspirationWindowTests
{
    // SearchRoot is fail-soft on a beta cutoff: the returned score can sit far
    // above beta. Widening must jump past that score in one step instead of
    // walking the fixed 33*relax ladder out one full re-search at a time.
    [Fact]
    public void WidenBeta_JumpsPastFailSoftScore_WhenScoreExceedsFixedStep()
    {
        // previous score 100 -> window (67, 133); re-search returns 500
        var newBeta = Iterate.WidenBeta(133, 1, 500);
        Assert.Equal(500 + Iterate.ASPIRATION_WINDOW, newBeta);
    }

    [Fact]
    public void WidenBeta_ClampsJumpAtMateBound()
    {
        var newBeta = Iterate.WidenBeta(133, 1, 9995);
        Assert.Equal(10000, newBeta);
    }

    [Fact]
    public void WidenBeta_UsesFixedStep_WhenScoreBarelyAboveBeta()
    {
        // score+33 (173) is inside the fixed step (133 + 33*4 = 265):
        // keep the larger widening so repeated small fail-highs still
        // escalate geometrically
        var newBeta = Iterate.WidenBeta(133, 4, 140);
        Assert.Equal(265, newBeta);
    }

    [Fact]
    public void WidenBeta_ClampsFixedStepAtMateBound()
    {
        var newBeta = Iterate.WidenBeta(9000, 64, 9001);
        Assert.Equal(10000, newBeta);
    }

    [Fact]
    public void WidenAlpha_JumpsPastFailSoftScore_WhenScoreExceedsFixedStep()
    {
        var newAlpha = Iterate.WidenAlpha(-133, 1, -500);
        Assert.Equal(-500 - Iterate.ASPIRATION_WINDOW, newAlpha);
    }

    [Fact]
    public void WidenAlpha_ClampsJumpAtMateBound()
    {
        var newAlpha = Iterate.WidenAlpha(-133, 1, -9995);
        Assert.Equal(-10000, newAlpha);
    }

    [Fact]
    public void WidenAlpha_UsesFixedStep_WhenScoreBarelyBelowAlpha()
    {
        var newAlpha = Iterate.WidenAlpha(-133, 4, -140);
        Assert.Equal(-265, newAlpha);
    }

    [Fact]
    public void WidenAlpha_FailHardScore_MatchesPlainStep()
    {
        // today SearchRoot is fail-hard on the low side (returns alpha
        // unchanged), so score == alpha and the jump term must not shrink
        // the widening below the plain 33*relax step
        var newAlpha = Iterate.WidenAlpha(-133, 1, -133);
        Assert.Equal(-133 - Iterate.ASPIRATION_WINDOW, newAlpha);
    }
}
