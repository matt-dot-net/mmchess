namespace mmchess{
    public class Board
    {
        ulong _wpawns;
        ulong _wknights;
        ulong _wbishops;
        ulong _wrooks;
        ulong _wqueens;

        ulong _bpawns;
        ulong _bknights;
        ulong _bbishops;
        ulong _brooks;
        ulong _bqueens;
        ulong _bking;
        ulong _wking;

        ulong _allpieces;
        ulong _wpieces;
        ulong _bpieces;


        public void Reset(){
            _wpawns = (0xff << 8);
            _bpawns = (0xff << 48);

            _wrooks |= BitMask.Mask[0] | BitMask.Mask[7];
            _brooks |= BitMask.Mask[63] | BitMask.Mask[56];

            _wknights |= BitMask.Mask[1] | BitMask.Mask[6];
            _bknights |= BitMask.Mask[62] | BitMask.Mask[57];

            _wbishops |= BitMask.Mask[2] | BitMask.Mask[5];
            _bbishops |= BitMask.Mask[61] | BitMask.Mask[58];
            
            _wqueens |= BitMask.Mask[3];
            _bqueens |= BitMask.Mask[60];
            
            _wking = BitMask.Mask[4];
            _bking = BitMask.Mask[59];

            _wpieces = _wpawns | _wrooks | _wknights | _wbishops | _wqueens | _wking;
            _bpieces = _bpawns | _brooks | _bknights | _bbishops | _bqueens | _bking;

            _allpieces = _wpieces | _bpieces;
        }
    }
}