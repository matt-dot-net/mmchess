using System;
namespace mmchess;

public class GameState
{
    public bool ShowThinking{get;set;}
    public bool UsingGui{get;set;}
    public bool UciMode{get;set;}

    public int ComputerSide{get;set;}
    public String Opponent{get;set;}
    public bool PonderEnabled{get;set;}
    public Move PonderMove{get;set;}
    public bool PonderMoveMade{get;set;}
    public ulong PonderStartHash{get;set;}
    public int PonderHits{get;set;}
    public int PonderMisses{get;set;}

    public TimeSpan WhiteClock{get;set;}
    public TimeSpan BlackClock{get;set;}

    public Board GameBoard{get;set;}

    public SearchStop SearchStop { get; } = new SearchStop();
    public bool TimeUp
    {
        get => SearchStop.IsRequested;
        set
        {
            if (value)
                SearchStop.Request();
            else
                SearchStop.Reset();
        }
    }

    public int DepthLimit{get;set;}
    public bool UciGoRequested{get;set;}
    public bool UciPonderSearch{get;set;}
    public bool UciPonderHit{get;set;}

    public TimeControl TimeControl{get;set;}

    public bool IsMyTurn{
        get{
            return ComputerSide==GameBoard.SideToMove;
        }
    }

    public TimeSpan MyClock{
        get {
            return ComputerSide == 0 ? WhiteClock : BlackClock;
        }
    }
    public GameState()
    {
        GameBoard = new Board();
        TimeControl = new TimeControl();
        ComputerSide=1;
    }

    public void WinBoardUpdateMyClock(int centiseconds){
        var newVal =GetTimeSpanFromWinBoardCentiSeconds(centiseconds);
        if(ComputerSide==0)
            WhiteClock = newVal;
        else
            BlackClock=newVal;
    }

    public void WinBoardUpdateOpponentClock(int centiseconds){
        var newVal =GetTimeSpanFromWinBoardCentiSeconds(centiseconds);
        if(ComputerSide==0)
            BlackClock = newVal;
        else
            WhiteClock=newVal;            
    }

    static TimeSpan GetTimeSpanFromWinBoardCentiSeconds(int centiseconds){
        return TimeSpan.FromMilliseconds(centiseconds*10);
    }
}
