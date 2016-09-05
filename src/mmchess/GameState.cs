using System;
namespace mmchess{

    public class GameState
    {
        public bool ShowThinking{get;set;}
        public bool UsingGui{get;set;}

        public int ComputerSide{get;set;}
        public String Opponent{get;set;}

        public TimeSpan WhiteClock{get;set;}
        public TimeSpan BlackClock{get;set;}

        public Board GameBoard{get;set;}

        public bool TimeUp { get; set; }     

        public int DepthLimit{get;set;}
        public TimeSpan TimeLimit{get;set;}

        public bool IsMyTurn{
            get{
                return ComputerSide==GameBoard.SideToMove;
            }
        }

        public GameState()
        {
            GameBoard = new Board();
            ComputerSide=1;
        }
    }
}