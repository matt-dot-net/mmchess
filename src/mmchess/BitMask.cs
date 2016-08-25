namespace mmchess{

    public sealed class BitMask
    {
        public static readonly ulong [] Mask = new ulong[64];
        
        static BitMask()
        {
            for(int i=0;i<64;i++)
            {
                Mask[i] = (ulong)1 << i;
            }
        }   
    }
}