
namespace mmchess{

    public enum TimeControlType{
        FixedTimePerMove,
        TimePerGame,
        NumberOfMoves,
        FixedDepth
    }

    public class TimeControl
    {
        public TimeControlType Type{get;set;}
        public int MovesInTimeControl{get;set;}
        public int GameTimeSeconds{get;set;}
        public int IncrementSeconds{get;set;}
        public int FixedTimePerSearchSeconds{get;set;}
    }
}