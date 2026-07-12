namespace mmchess;

using System.Threading;

// Caches the pure pawn-structure part of pawn evaluation (doubled/passed
// pawns, per-file occupancy), keyed by Board.PawnHashKey (pawn placement
// only - see TranspositionTable.GetPawnHashKeyForPosition). Unlike the main
// TranspositionTable, entries here are search-independent, exact values: the
// same pawn placement always evaluates to the same structural score, so
// there's no depth/age/replacement strategy needed, and entries never go
// stale on their own. Deliberately excludes anything that depends on other
// pieces' positions (e.g. blocked-pawn status depends on Board.AllPieces,
// not just pawns) - see Evaluator.EvaluateBlockedPawns, which is computed
// fresh every call instead of being cached here.
public class PawnHashTable
{
    const int EntryCount = 1 << 20; // ~1M entries, far fewer distinct pawn structures than positions
    static readonly ulong KeyMask = EntryCount - 1;

    class Entry
    {
        public ulong Key;
        public PawnScore Score;
    }

    readonly Entry[] table = new Entry[EntryCount];

    long hits;
    long probes;

    public ulong Hits => (ulong)Interlocked.Read(ref hits);
    public ulong Probes => (ulong)Interlocked.Read(ref probes);

    static readonly object _lock = new object();
    static PawnHashTable _instance;
    public static PawnHashTable Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                        _instance = new PawnHashTable();
                }
            }
            return _instance;
        }
    }

    public bool TryProbe(ulong pawnHashKey, out PawnScore score)
    {
        Interlocked.Increment(ref probes);
        var e = Volatile.Read(ref table[pawnHashKey & KeyMask]);
        if (e != null && e.Key == pawnHashKey)
        {
            Interlocked.Increment(ref hits);
            score = e.Score;
            return true;
        }

        score = default(PawnScore);
        return false;
    }

    public void Store(ulong pawnHashKey, PawnScore score)
    {
        Volatile.Write(ref table[pawnHashKey & KeyMask], new Entry { Key = pawnHashKey, Score = score });
    }

    public void Clear()
    {
        System.Array.Clear(table, 0, table.Length);
        Interlocked.Exchange(ref hits, 0);
        Interlocked.Exchange(ref probes, 0);
    }
}
