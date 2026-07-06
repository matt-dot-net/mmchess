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

    [Fact]
    public void StoreThenProbeRoundTripsEntry()
    {
        var tt = TranspositionTable.Instance;
        ulong key = 0x123456789abcdef0;
        var move = new Move(0x0000abcd);

        tt.Store(key, move, depth: 5, score: 123, TranspositionTableEntry.EntryType.PV, ply: 0);

        Assert.True(tt.TryProbe(key, out var entry));
        Assert.Equal(move.Value, entry.MoveValue);
        Assert.Equal(123, entry.Score);
        Assert.Equal(5, entry.Depth);
        Assert.Equal((byte)TranspositionTableEntry.EntryType.PV, entry.Type);
    }

    [Fact]
    public void ProbeOfNeverStoredKeyReturnsFalse()
    {
        // The lock check only matches when the stored lock equals this exact
        // key, so an empty (or foreign) slot is a miss regardless of what else
        // the process-wide table happens to hold.
        var tt = TranspositionTable.Instance;
        Assert.False(tt.TryProbe(0xdeadbeefcafef00d, out _));
    }

    [Fact]
    public void SetSizeChangesCapacityAndKeepsProbeWorking()
    {
        var tt = TranspositionTable.Instance;
        try
        {
            TranspositionTable.SetSize(16);
            var cap16 = tt.EntryCapacity;

            TranspositionTable.SetSize(64);
            var cap64 = tt.EntryCapacity;

            Assert.True(cap64 > cap16);
            Assert.Equal(0L, cap64 & (cap64 - 1)); // power of two

            // a round-trip still works after reallocation
            ulong key = 0x0f0f0f0f0f0f0f0f;
            tt.Store(key, new Move(0x00001234), 3, 50, TranspositionTableEntry.EntryType.CUT, 0);
            Assert.True(tt.TryProbe(key, out var e));
            Assert.Equal(50, e.Score);
        }
        finally
        {
            TranspositionTable.SetSize(TranspositionTable.DefaultSizeMb);
        }
    }

    [Fact]
    public void SetSizeClampsBelowMinimum()
    {
        var tt = TranspositionTable.Instance;
        try
        {
            TranspositionTable.SetSize(0);
            Assert.Equal(1, tt.SizeInMb);

            TranspositionTable.SetSize(-50);
            Assert.Equal(1, tt.SizeInMb);
        }
        finally
        {
            TranspositionTable.SetSize(TranspositionTable.DefaultSizeMb);
        }
    }
}