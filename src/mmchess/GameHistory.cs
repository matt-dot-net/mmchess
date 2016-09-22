using System.Collections.Generic;
namespace mmchess{

    public class GameHistory 
    {
        List<HistoryMove> _history;
        List<int> _pawnOrCapIndices = new List<int>();

        public HistoryMove LastMove(){
            return _history[_history.Count-1];
        }
        public int Count{
            get{
                return _history.Count;
            }
        }

        public HistoryMove this[int index]
        {
            get{
                return _history[index];
            }
        }
        public GameHistory(){
            _history = new List<HistoryMove>();
        }

        public void Add(HistoryMove move){
            _history.Add(move);
            if((move.Bits & (byte)(MoveBits.Capture | MoveBits.Pawn)) > 0)
                _pawnOrCapIndices.Add(_history.Count-1);
        }

        public bool FiftyMoveRule(){
            if(_pawnOrCapIndices.Count <= 0)
                return false;
            return (_history.Count-1 - _pawnOrCapIndices[_pawnOrCapIndices.Count-1])==100;
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
            if (_pawnOrCapIndices.Count > 0)
                lastIndex = _pawnOrCapIndices[_pawnOrCapIndices.Count - 1];
            int repeats=0;
            for (int i = _history.Count - 1; i >= lastIndex; i--)
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
            if (_pawnOrCapIndices.Count > 0)
                lastIndex = _pawnOrCapIndices[_pawnOrCapIndices.Count - 1];
            for (int i = _history.Count - 1; i >= lastIndex; i--)
            {
                var m = _history[i];
                if (m.HashKey == hashKey)
                    return true;
            }
            return false;
        }

        public void RemoveLast(){
            var lastIndex = _history.Count-1;
            var m = _history[lastIndex];            
            _history.RemoveAt(lastIndex);
            if((m.Bits & (byte)(MoveBits.Capture | MoveBits.Pawn)) > 0)
                _pawnOrCapIndices.RemoveAt(_pawnOrCapIndices.Count-1);
            
        }
    }
}