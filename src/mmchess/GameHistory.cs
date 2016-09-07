using System.Collections.Generic;
namespace mmchess{

    public class GameHistory : List<HistoryMove>
    {

        public GameHistory(){
            
        }

        public bool IsGameDrawn(ulong hashKey)
        {
            return DrawnByRepetition(hashKey) || HalfMovesSinceLastCaptureOrPawn >= 100;
        }
        
        public int HalfMovesSinceLastCaptureOrPawn{
            get{
                int j=0;
                for(int i=this.Count-1;i>=0;i--,j++){
                    var m = this[i];
                    if((m.Bits & (byte)MoveBits.Capture)>0 || (m.Bits & (byte)MoveBits.Pawn)>0)
                        break;
                }
                return j;
            }
        }

        public bool DrawnByRepetition(ulong hashKey){
                int repeats=0;
                for(int i=this.Count-1;i>=0;i--){
                    var m = this[i];
                    if((m.Bits & (byte)MoveBits.Capture)>0 || (m.Bits & (byte)MoveBits.Pawn)>0)
                        break;
                    if(m.HashKey == hashKey)
                        if(++repeats == 3)
                            return true;
                }
                return false;
        }

        public void RemoveLast(){
            this.RemoveAt(this.Count-1);
        }
    }
}