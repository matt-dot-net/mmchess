using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;

namespace mmchess;

public class TranspositionTableConcurrencyTests
{
    [Fact]
    public void ConcurrentStoresNeverReturnAnotherKeysData()
    {
        var tt = TranspositionTable.Instance;
        var key1 = 0x1234567800000001UL;
        var key2 = key1 + (ulong)tt.EntryCapacity;
        var move1 = new Move(0x00001234);
        var move2 = new Move(0x00005678);
        var failures = new ConcurrentQueue<string>();

        Parallel.Invoke(
            () => StoreRepeatedly(tt, key1, move1, 111),
            () => StoreRepeatedly(tt, key2, move2, 222),
            () => ProbeRepeatedly(tt, key1, move1, 111, failures),
            () => ProbeRepeatedly(tt, key2, move2, 222, failures));

        Assert.True(failures.IsEmpty, failures.TryPeek(out var failure) ? failure : null);
    }

    static void StoreRepeatedly(TranspositionTable tt, ulong key, Move move, int score)
    {
        for (var i = 0; i < 100_000; i++)
            tt.Store(key, move, 6, score, TranspositionTableEntry.EntryType.PV, 0);
    }

    static void ProbeRepeatedly(
        TranspositionTable tt,
        ulong key,
        Move expectedMove,
        int expectedScore,
        ConcurrentQueue<string> failures)
    {
        for (var i = 0; i < 100_000; i++)
        {
            if (!tt.TryProbe(key, out var entry))
                continue;

            if (entry.MoveValue != expectedMove.Value || entry.Score != expectedScore)
            {
                failures.Enqueue(
                    $"Key {key:x16} returned move {entry.MoveValue:x8}, score {entry.Score}.");
                return;
            }
        }
    }
}
