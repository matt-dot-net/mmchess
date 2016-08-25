namespace mmchess
{
    public class Move
    {
        public enum MoveBits{
            Capture=1,
            DoublePawnMove=2,

        }

        public byte From{get;set;}
        public byte To {get;set;}
        public byte Bits{get;set;}
        public byte Promotion{get;set;}
    }
}