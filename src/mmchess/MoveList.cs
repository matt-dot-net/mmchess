using System;
using System.Collections.Generic;

namespace mmchess;

public ref struct MoveList
{
    public const int StackCapacity = 256;

    readonly Span<Move> _moves;

    public MoveList(Span<Move> moves)
    {
        _moves = moves;
        Count = 0;
    }

    public int Count { get; private set; }

    public Move this[int index]
    {
        get => _moves[index];
        set => _moves[index] = value;
    }

    public void Add(Move move)
    {
        if (Count >= _moves.Length)
            throw new InvalidOperationException("MoveList capacity exceeded.");

        _moves[Count++] = move;
    }

    public void CopyTo(IList<Move> moves)
    {
        for (int i = 0; i < Count; i++)
            moves.Add(_moves[i]);
    }
}
