namespace mmchess
{
    ///
    // All values are in the perspective of the SideToMove
    ///
    public class Evaluation
    {
        public Board Board{get;set;}
        int _material;
        bool _materialFresh;
        public Evaluation(Board b)
        {
            Board = b;
        }

        public PawnScore PawnScore {get;set;}
        public int Material{
            get{
                if(!_materialFresh){
                    _material = Evaluator.EvaluateMaterial(Board);
                    _materialFresh=true;
                }
                return _material;
            }
        }

    }
}