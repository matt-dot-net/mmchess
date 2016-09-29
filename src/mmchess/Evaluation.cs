namespace mmchess
{
    ///
    // All values are in the perspective of the SideToMove
    ///
    public class Evaluation
    {
        public Board Board{get;set;}
        int _material;
        int [] _minors = new int[2];
        int [] _majors = new int[2];
        public Evaluation(Board b)
        {
            Board = b;
            _material = Evaluator.EvaluateMaterial(b);
            for(int i=0;i<2;i++)
            {
                _minors[i] = b.Minors(i).Count();
                _majors[i] = b.Majors(i).Count();
            }
        }

        public int [] Minors{get{return _minors;}}
        public int [] Majors{get{return _majors;}}

        public PawnScore PawnScore {get;set;}
        public int Material{
            get{return _material;}
        }

    }
}