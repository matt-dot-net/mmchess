using Xunit;
namespace mmchess;

public class TranspositionTableTests{

    [Fact]
    public void NextSearchIdCyclesFromZeroToThree(){
        // TranspositionTable.Instance is a process-wide singleton, so other tests
        // (e.g. anything calling Iterate.DoIterate) may have already bumped SearchId
        // before this test runs - assert the cycling behavior relative to wherever
        // it currently is, rather than assuming it starts at 0.
        var test= TranspositionTable.Instance;
        var start = test.SearchId;
        test.NextSearchId();
        Assert.Equal((start+1)&3,test.SearchId);
        test.NextSearchId();
        Assert.Equal((start+2)&3,test.SearchId);
        test.NextSearchId();
        Assert.Equal((start+3)&3,test.SearchId);
        test.NextSearchId();
        Assert.Equal(start,test.SearchId);
    }

    [Fact]
    public void ValueToTTNormalizesMateScoreRelativeToStoringNode(){
        // A losing/mated score of -9996 found while searching from ply=2 (i.e. the
        // actual mate is 2 plies further down, at absolute ply 4) should normalize
        // to -9998 (= -10000 + 2), the ply-independent "mate in 2 from here" form.
        Assert.Equal(-9998, TranspositionTable.ValueToTT(-9996, 2));
    }

    [Fact]
    public void ValueFromTTReappliesCurrentPlyWhenReadingBack(){
        // Reading the normalized -9998 ("mate in 2") back at a different node,
        // reached at ply=5, should re-anchor it to that node's root distance:
        // mate would occur at absolute ply 5+2=7, i.e. -10000+7 = -9993.
        Assert.Equal(-9993, TranspositionTable.ValueFromTT(-9998, 5));
    }

    [Fact]
    public void SameLocalMateDistanceNormalizesToSameStoredValueFromAnyPly(){
        // "Mate in 2" reached via ply=2 (raw -9996) or via ply=5 (raw -9993, since
        // the absolute ply of the mate itself is 3 plies further away in this path)
        // should normalize to the identical ply-independent stored value - the
        // whole point is that the same local mate distance stores the same either way.
        var storedFromPly2 = TranspositionTable.ValueToTT(-9996, 2);
        var storedFromPly5 = TranspositionTable.ValueToTT(-9993, 5);

        Assert.Equal(storedFromPly2, storedFromPly5);
    }
}