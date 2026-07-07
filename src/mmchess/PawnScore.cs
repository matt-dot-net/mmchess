using System;

namespace mmchess;

public struct PawnScore{
    public int Eval{get;set;}

    ulong _whiteFile0;
    ulong _whiteFile1;
    ulong _whiteFile2;
    ulong _whiteFile3;
    ulong _whiteFile4;
    ulong _whiteFile5;
    ulong _whiteFile6;
    ulong _whiteFile7;
    ulong _blackFile0;
    ulong _blackFile1;
    ulong _blackFile2;
    ulong _blackFile3;
    ulong _blackFile4;
    ulong _blackFile5;
    ulong _blackFile6;
    ulong _blackFile7;

    public ulong GetFile(int side, int file)
    {
        switch (side * 8 + file)
        {
            case 0: return _whiteFile0;
            case 1: return _whiteFile1;
            case 2: return _whiteFile2;
            case 3: return _whiteFile3;
            case 4: return _whiteFile4;
            case 5: return _whiteFile5;
            case 6: return _whiteFile6;
            case 7: return _whiteFile7;
            case 8: return _blackFile0;
            case 9: return _blackFile1;
            case 10: return _blackFile2;
            case 11: return _blackFile3;
            case 12: return _blackFile4;
            case 13: return _blackFile5;
            case 14: return _blackFile6;
            case 15: return _blackFile7;
            default: throw new ArgumentOutOfRangeException();
        }
    }

    public void SetFile(int side, int file, ulong value)
    {
        switch (side * 8 + file)
        {
            case 0: _whiteFile0 = value; break;
            case 1: _whiteFile1 = value; break;
            case 2: _whiteFile2 = value; break;
            case 3: _whiteFile3 = value; break;
            case 4: _whiteFile4 = value; break;
            case 5: _whiteFile5 = value; break;
            case 6: _whiteFile6 = value; break;
            case 7: _whiteFile7 = value; break;
            case 8: _blackFile0 = value; break;
            case 9: _blackFile1 = value; break;
            case 10: _blackFile2 = value; break;
            case 11: _blackFile3 = value; break;
            case 12: _blackFile4 = value; break;
            case 13: _blackFile5 = value; break;
            case 14: _blackFile6 = value; break;
            case 15: _blackFile7 = value; break;
            default: throw new ArgumentOutOfRangeException();
        }
    }
}
