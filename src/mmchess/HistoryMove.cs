namespace mmchess;

// Was a subclass of Move; Move is now a struct (can't be a base type), so this
// composes a Move value plus the extra state MakeMove/UnMakeMove needs to undo
// a move. It's a struct too - stored inline in the History list, no per-make
// heap allocation. Access the move payload via .Move (e.g. hm.Move.To).
public struct HistoryMove
{
    public Move Move;
    public bool IsNullMove;
    public ulong EnPassant;
    public byte CastleStatus;
    public ulong HashKey;
    public MoveBits CapturedPiece;

    public HistoryMove(ulong hashKey, Move m)
    {
        HashKey = hashKey;
        Move = m;
        IsNullMove = false;
        EnPassant = 0;
        CastleStatus = 0;
        CapturedPiece = 0;
    }

    // A null move (see AlphaBeta null-move pruning): no move payload, flagged so
    // the search can tell it apart from a real move in History.
    public static HistoryMove NullMove(ulong hashKey) =>
        new HistoryMove(hashKey, Move.Null) { IsNullMove = true };
}
