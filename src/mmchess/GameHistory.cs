using System;
namespace mmchess;

public class GameHistory 
{
    const int InitialHistoryCapacity = 512;
    const int InitialPawnOrCaptureCapacity = 128;

    HistoryMove[] _history;
    int[] _pawnOrCapIndices;
    int _count;
    int _pawnOrCapCount;

    public HistoryMove LastMove(){
        return _history[_count-1];
    }
    public int Count{
        get{
            return _count;
        }
    }

    public HistoryMove this[int index]
    {
        get{
            if ((uint)index >= (uint)_count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _history[index];
        }
    }
    public GameHistory(){
        _history = new HistoryMove[InitialHistoryCapacity];
        _pawnOrCapIndices = new int[InitialPawnOrCaptureCapacity];
    }

    public void Add(HistoryMove move){
        if (_count == _history.Length)
            Array.Resize(ref _history, _history.Length * 2);

        _history[_count] = move;
        if((move.Move.Bits & (byte)(MoveBits.Capture | MoveBits.Pawn)) > 0)
        {
            if (_pawnOrCapCount == _pawnOrCapIndices.Length)
                Array.Resize(ref _pawnOrCapIndices, _pawnOrCapIndices.Length * 2);

            _pawnOrCapIndices[_pawnOrCapCount++] = _count;
        }

        _count++;
    }

    public bool FiftyMoveRule(){
        if(_pawnOrCapCount <= 0)
            return false;
        return (_count-1 - _pawnOrCapIndices[_pawnOrCapCount-1])==100;
    }

    public bool IsGameDrawn(ulong hashKey)
    {
        return DrawnByRepetition(hashKey) || FiftyMoveRule();
    }
                    
    public bool IsPositionDrawn(ulong hashKey){
        return PositionRepeated(hashKey) || FiftyMoveRule();
    }
    public bool DrawnByRepetition(ulong hashKey)
    {
        int lastIndex = 0;
        if (_pawnOrCapCount > 0)
            lastIndex = _pawnOrCapIndices[_pawnOrCapCount - 1];
        int repeats=0;
        for (int i = _count - 1; i >= lastIndex; i--)
        {
            var m = _history[i];
            if (m.HashKey == hashKey)
                if(++repeats==3)
                    return true;
        }
        return false;            
    }

    private bool PositionRepeated(ulong hashKey)
    {
        int lastIndex = 0;
        if (_pawnOrCapCount > 0)
            lastIndex = _pawnOrCapIndices[_pawnOrCapCount - 1];
        for (int i = _count - 1; i >= lastIndex; i--)
        {
            var m = _history[i];
            if (m.HashKey == hashKey)
                return true;
        }
        return false;
    }

    public void RemoveLast(){
        var lastIndex = _count-1;
        var m = _history[lastIndex];            
        _history[lastIndex] = default;
        _count--;
        if((m.Move.Bits & (byte)(MoveBits.Capture | MoveBits.Pawn)) > 0)
            _pawnOrCapCount--;
        
    }
}
