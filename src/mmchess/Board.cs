
using System;
using System.Collections.Generic;

namespace mmchess
{
    public class Board
    {
        public List<HistoryMove> History { get; set; }
        public byte CastleStatus { get; set; }
        public ulong[] Pawns;
        public ulong[] Knights;
        public ulong[] Bishops;
        public ulong[] Rooks;
        public ulong[] Queens;
        public ulong[] King;
        public ulong AllPieces { get; set; }
        public ulong AllPiecesR90 { get; set; }
        public ulong AllPiecesR45 { get; set; }
        public ulong AllPiecesL45 { get; set; }
        public ulong[] Pieces { get; set; }
        public ulong EnPassant { get; set; }
        public int SideToMove { get; set; }

        public ulong HashKey{get;set;}

        public static readonly ulong[] FileMask = new ulong[8];
        public static readonly ulong[] RankMask = new ulong[8];

        public static readonly string[] SquareNames = new string[64]{
    "a8","b8","c8","d8","e8","f8","g8","h8",
    "a7","b7","c7","d7","e7","f7","g7","h7",
    "a6","b6","c6","d6","e6","f6","g6","h6",
    "a5","b5","c5","d5","e5","f5","g5","h5",
    "a4","b4","c4","d4","e4","f4","g4","h4",
    "a3","b3","c3","d3","e3","f3","g3","h3",
    "a2","b2","c2","d2","e2","f2","g2","h2",
    "a1","b1","c1","d1","e1","f1","g1","h1",
};
        public static readonly byte[] RotatedR45Map = new byte[64] {
 0,1,3,6,10,15,21,28,
 2,4,7,11,16,22,29,36,
 5,8,12,17,23,30,37,43,
 9,13,18,24,31,38,44,49,
 14,19,25,32,39,45,50,54,
 20,26,33,40,46,51,55,58,
 27,34,41,47,52,56,59,61,
 35,42,48,53,57,60,62,63
        };

        public static readonly byte[] RotatedL45Map = new byte[64] {
  28,21,15,10,6,3,1,0,
 36,29,22,16,11,7,4,2,
 43,37,30,23,17,12,8,5,
 49,44,38,31,24,18,13,9,
 54,50,45,39,32,25,19,14,
 58,55,51,46,40,33,26,20,
 61,59,56,52,47,41,34,27,
 63,62,60,57,53,48,42,35
        };

        public static readonly byte[] Rotated90Map = new byte[64]{
    0,8,16,24,32,40,48,56,
    1,9,17,25,33,41,49,57,
    2,10,18,26,34,42,50,58,
    3,11,19,27,35,43,51,59,
    4,12,20,28,36,44,52,60,
    5,13,21,29,37,45,53,61,
    6,14,22,30,38,46,54,62,
    7,15,23,31,39,47,55,63
};

        static Board()
        {
            for (int i = 0; i < 64; i++)
            {
                FileMask[i.File()] |= BitMask.Mask[i];
                RankMask[i.Rank()] |= BitMask.Mask[i];
            }
        }
        public Board(Board b)
        {
            this.SideToMove=b.SideToMove;
            this.AllPieces= b.AllPieces;
            this.AllPiecesL45= b.AllPiecesL45;
            this.AllPiecesR45 = b.AllPiecesR45;
            this.AllPiecesR90 = b.AllPiecesR90;
            this.CastleStatus= b.CastleStatus;
            this.EnPassant = b.EnPassant;
            this.History = new List<HistoryMove>(b.History);
            this.King = new ulong[2];
            this.Knights = new ulong[2];
            this.Rooks = new ulong[2];
            this.Bishops = new ulong[2];
            this.Queens = new ulong [2];
            this.Pawns = new ulong[2];
            this.Pieces = new ulong [2];
            for(int i=0;i<2;i++){
                this.King[i]= b.King[i];
                this.Knights[i] = b.Knights[i];
                this.Rooks[i] = b.Rooks[i];
                this.Bishops[i] = b.Bishops[i];
                this.Queens[i] = b.Queens[i];
                this.Pawns[i] = b.Pawns[i];
                this.Pieces[i] = b.Pieces[i];
            }
            HashKey = b.HashKey;
        }

        public Board()
        {
            Initialize();
        }

        public void Initialize()
        {
            CastleStatus = 0xF;

            History = new List<HistoryMove>();
            Pawns = new ulong[2];
            Pawns[1] = 0xff00;
            Pawns[0] = 0x00ff000000000000;

            Rooks = new ulong[2];
            Rooks[1] |= BitMask.Mask[0] | BitMask.Mask[7];
            Rooks[0] |= BitMask.Mask[63] | BitMask.Mask[56];

            Knights = new ulong[2];
            Knights[1] |= BitMask.Mask[1] | BitMask.Mask[6];
            Knights[0] |= BitMask.Mask[62] | BitMask.Mask[57];

            Bishops = new ulong[2];
            Bishops[1] |= BitMask.Mask[2] | BitMask.Mask[5];
            Bishops[0] |= BitMask.Mask[61] | BitMask.Mask[58];

            Queens = new ulong[2];
            Queens[1] |= BitMask.Mask[3];
            Queens[0] |= BitMask.Mask[59];

            King = new ulong[2];
            King[1] = BitMask.Mask[4];
            King[0] = BitMask.Mask[60];

            Pieces = new ulong[2];
            Pieces[0] = Pawns[0] | Rooks[0] | Knights[0] | Bishops[0] | Queens[0] | King[0];
            Pieces[1] = Pawns[1] | Rooks[1] | Knights[1] | Bishops[1] | Queens[1] | King[1];

            AllPieces = Pieces[0] | Pieces[1];

            BuildRotatedBoards(this);
            HashKey = TranspositionTable.GetHashKeyForPosition(this);
        }

        private static void BuildRotatedBoards(Board b)
        {
            for (int i = 0; i < 64; i++)
            {
                if (b.AllPieces.IsSet(i))
                {
                    b.AllPiecesR90 |= BitMask.Mask[Rotated90Map[i]];
                    b.AllPiecesL45 |= BitMask.Mask[RotatedL45Map[i]];
                    b.AllPiecesR45 |= BitMask.Mask[RotatedR45Map[i]];
                }
            }
        }

        public Boolean InCheck(int sideToMove)
        {
            int kingsq = King[sideToMove].BitScanForward();
            int xside = sideToMove ^ 1;
            if ((MoveGenerator.BishopAttacks(this, kingsq) & (Bishops[xside] | Queens[xside])) > 0)
                return true;
            else if ((MoveGenerator.RookAttacks(this, kingsq) & (Rooks[xside] | Queens[xside])) > 0)
                return true;
            else if ((MoveGenerator.PawnAttacks[sideToMove, kingsq] & Pawns[xside]) > 0)
                return true;
            else if ((MoveGenerator.KnightMoves[kingsq] & Knights[xside]) > 0)
                return true;
            else if ((MoveGenerator.KingMoves[kingsq] & King[xside])>0)
                return true;

            return false;
        }

        void UpdateCastleStatus(Move m)
        {
            //determine if we even care
            if (((3 << (SideToMove*2)) & CastleStatus) == 0)
                return;

            if ((m.Bits & (byte)(MoveBits.King | MoveBits.Rook)) > 0)
            {
                //we've moved a king or a rook
                if ((BitMask.Mask[m.From] & King[SideToMove]) > 0) //if we are moving the king        
                {
                    //moved a king, wipe out castle status
                    CastleStatus &= (byte)(SideToMove == 1 ? 3 : 12);
                    HashKey ^= TranspositionTable.CastleStatusKey[CastleStatus];
                }
                else
                { //moving a rook
                    if (SideToMove == 1)
                        CastleStatus &= (byte)(m.From.File() == 7 ? 11:7);
                    else
                        CastleStatus &= (byte)(m.From.File() == 7 ? 14:13);
                    HashKey ^= TranspositionTable.CastleStatusKey[CastleStatus];
                }
            }
            
        }

        void MakeCastleMove(Move m)
        {
            //king from->to work will be handled by normal move 
            //code.  We need to handle the rook movement here
            int from = -1, to = -1;
            if (SideToMove == 0)
            {
                //White
                if (m.To == 62)
                {
                    from = 63;
                    to = 61;
                }
                else
                {
                    from = 56;
                    to = 59;
                }
            }
            else
            {
                //Black
                if (m.To == 06)
                {
                    to = 05;
                    from = 07;
                }
                else
                {
                    to = 3;
                    from = 0;
                }
            }
            var moveMask = (BitMask.Mask[from] | BitMask.Mask[to]);
            Rooks[SideToMove] ^= moveMask;
            Pieces[SideToMove] ^= moveMask;
            AllPieces ^= moveMask;
            AllPiecesR90 ^= (BitMask.Mask[Rotated90Map[from]] | BitMask.Mask[Rotated90Map[to]]);
            AllPiecesL45 ^= (BitMask.Mask[RotatedL45Map[from]] | BitMask.Mask[RotatedL45Map[to]]);
            AllPiecesR45 ^= (BitMask.Mask[RotatedR45Map[from]] | BitMask.Mask[RotatedR45Map[to]]);
            HashKey ^= TranspositionTable.HashKeys[SideToMove,(int)Piece.Rook,from];
            HashKey ^= TranspositionTable.HashKeys[SideToMove,(int)Piece.Rook,to];

        }

        public bool MakeMove(Move m)
        {
            var hm = new HistoryMove(m);
            hm.EnPassant = EnPassant;
            hm.CastleStatus = CastleStatus;

            //castle
            if ((m.Bits & (byte)MoveBits.King) > 0 && Math.Abs(m.To.File() - m.From.File()) == 2)           
                MakeCastleMove(m);

            UpdateCastleStatus(m);
            UpdateCapture(m, hm);
            UpdateBitBoards(m);

            if(m.Promotion >0)
                UpdatePromotion(m);

            //set enpassant sq
            if (((m.Bits & (byte)MoveBits.Pawn) > 0) && 
                Math.Abs(m.To - m.From) == 16){
                    EnPassant = BitMask.Mask[m.From + (SideToMove == 1 ? 8 : -8)];
                    HashKey^=TranspositionTable.EnPassantFileKey[m.To.File()];
                }
            else
                EnPassant=0;

            SideToMove ^= 1;
            HashKey ^= TranspositionTable.SideToMoveKey[SideToMove];
            // push the move onto the list of moves
            History.Add(hm);

            //make sure we are legal
            if (InCheck(SideToMove ^ 1))
            {
                InCheck(SideToMove ^ 1);
                UnMakeMove();
                return false;
            }
            return true;

        }

        void UpdatePromotion(Move m){
            Pawns[SideToMove] ^= BitMask.Mask[m.To]; 
            var piece = (Piece)m.Promotion;
            switch(piece){
                case Piece.Knight:
                    Knights[SideToMove]^= BitMask.Mask[m.To];
                    break;
                case Piece.Bishop:
                    Bishops[SideToMove]^= BitMask.Mask[m.To];
                    break;
                case Piece.Rook:
                    Rooks[SideToMove]^= BitMask.Mask[m.To];
                    break;
                case Piece.Queen:
                    Queens[SideToMove]^=BitMask.Mask[m.To];
                    break;
            }
            HashKey^=TranspositionTable.HashKeys[SideToMove,(int)piece,m.To];
        }

        void UpdateCapture(Move m, HistoryMove hm)
        {
            if ((m.Bits & (byte)MoveBits.Capture) > 0)
            {
                //find the captured piece
                var xside = SideToMove ^ 1;
                int sq = m.To;
                Piece p = Piece.King;//needs to be assigned
                if ((Knights[xside] & BitMask.Mask[m.To]) > 0)
                {
                    p = Piece.Knight;
                    Knights[xside] ^= BitMask.Mask[m.To];
                    hm.CapturedPiece = MoveBits.Knight;
                }
                else if ((Bishops[xside] & BitMask.Mask[m.To]) > 0)
                {
                    p=Piece.Bishop;
                    Bishops[xside] ^= BitMask.Mask[m.To];
                    hm.CapturedPiece = MoveBits.Bishop;
                }
                else if ((Rooks[xside] & BitMask.Mask[m.To]) > 0)
                {
                    p = Piece.Rook;
                    Rooks[xside] ^= BitMask.Mask[m.To];
                    hm.CapturedPiece = MoveBits.Rook;
                }
                else if ((Queens[xside] & BitMask.Mask[m.To]) > 0)
                {
                    p = Piece.Queen;
                    Queens[xside] ^= BitMask.Mask[m.To];
                    hm.CapturedPiece = MoveBits.Queen;
                }
                else if ((Pawns[xside] & BitMask.Mask[m.To]) > 0)
                {
                    p=Piece.Pawn;
                    Pawns[xside] ^= BitMask.Mask[m.To];
                    hm.CapturedPiece = MoveBits.Pawn;
                }
                else if ((BitMask.Mask[m.To] & EnPassant) > 0)
                {
                    var epSq = xside == 1 ? m.To + 8 : m.To - 8;
                    Pawns[xside] ^= BitMask.Mask[epSq];
                    hm.CapturedPiece = MoveBits.Pawn;
                    AllPieces ^= BitMask.Mask[epSq];
                    Pieces[xside] ^= BitMask.Mask[epSq];
                    AllPiecesL45 ^= BitMask.Mask[RotatedL45Map[epSq]];
                    AllPiecesR45 ^= BitMask.Mask[RotatedR45Map[epSq]];
                    AllPiecesR90 ^= BitMask.Mask[Rotated90Map[epSq]];
                    HashKey ^= TranspositionTable.HashKeys[xside,(int)Piece.Pawn,epSq];
                    return;
                }
                else if ((King[xside] & BitMask.Mask[m.To]) > 0)
                    throw new InvalidOperationException("Cannot capture the king");

                AllPieces ^= BitMask.Mask[sq];
                Pieces[xside] ^= BitMask.Mask[sq];
                AllPiecesL45 ^= BitMask.Mask[RotatedL45Map[sq]];
                AllPiecesR45 ^= BitMask.Mask[RotatedR45Map[sq]];
                AllPiecesR90 ^= BitMask.Mask[Rotated90Map[sq]];
                HashKey ^= TranspositionTable.HashKeys[xside,(int)p,sq];
            }
        }

        private void UpdateBitBoards(Move m)
        {
            var p = Move.GetPieceFromMoveBits((MoveBits)m.Bits);
            var moveMask = BitMask.Mask[m.From] | BitMask.Mask[m.To];

            UpdateBoards(p,m, SideToMove, moveMask);

            AllPieces ^= moveMask;
            AllPiecesL45 ^= (BitMask.Mask[RotatedL45Map[m.From]] | BitMask.Mask[RotatedL45Map[m.To]]);
            AllPiecesR45 ^= (BitMask.Mask[RotatedR45Map[m.From]] | BitMask.Mask[RotatedR45Map[m.To]]);
            AllPiecesR90 ^= (BitMask.Mask[Rotated90Map[m.From]] | BitMask.Mask[Rotated90Map[m.To]]);
            HashKey ^= TranspositionTable.HashKeys[SideToMove,(int)p,m.From];
            HashKey ^= TranspositionTable.HashKeys[SideToMove,(int)p,m.To];

        }

        public void UnMakeMove()
        {
            var index = History.Count - 1;
            if (index < 0)
                return;
            var m = History[index];
            HashKey ^= TranspositionTable.SideToMoveKey[SideToMove];
            SideToMove ^= 1;


            //restore captured piece
            if (m.CapturedPiece > 0)
                UnmakeCapture(m);

            if ((m.Bits &(byte)MoveBits.King)>0 && Math.Abs(m.From.File()-m.To.File())==2){
                UnmakeCastleMove(m);
            }

            if(m.Promotion>0)
                UpdatePromotion(m);
            if(EnPassant>0)
                HashKey^=TranspositionTable.EnPassantFileKey[m.To.File()];
            EnPassant = m.EnPassant;
            if(CastleStatus != m.CastleStatus)
                HashKey ^= TranspositionTable.CastleStatusKey[CastleStatus];
            CastleStatus = m.CastleStatus;
            UpdateBitBoards(m);
            History.RemoveAt(index);
        }

        void UnmakeCastleMove(Move m)
        {
            ulong moveMask, l45Mask, r45Mask, r90Mask;
            int from, to;
            //we just need to undo the rook shenanigans
            if(SideToMove==0){
                if(m.To==62) //kingside
                {
                    from = 61;
                    to = 63;
                    moveMask = BitMask.Mask[from]|BitMask.Mask[to];
                    l45Mask = BitMask.Mask[RotatedL45Map[from]] | BitMask.Mask[RotatedL45Map[to]];
                    r45Mask = BitMask.Mask[RotatedR45Map[from]] | BitMask.Mask[RotatedR45Map[to]];
                    r90Mask = BitMask.Mask[Rotated90Map[from]]|BitMask.Mask[Rotated90Map[to]];
                }
                else
                {
                    from = 56;
                    to=59;
                    moveMask = BitMask.Mask[from]|BitMask.Mask[to];
                    l45Mask = BitMask.Mask[RotatedL45Map[from]] | BitMask.Mask[RotatedL45Map[to]];
                    r45Mask = BitMask.Mask[RotatedR45Map[from]] | BitMask.Mask[RotatedR45Map[to]];
                    r90Mask = BitMask.Mask[Rotated90Map[from]]|BitMask.Mask[Rotated90Map[to]];
                }

            }
            else{
                if(m.To==06)
                {
                    from = 7;
                    to=5;
                    moveMask = BitMask.Mask[from]|BitMask.Mask[to];
                    l45Mask = BitMask.Mask[RotatedL45Map[from]] | BitMask.Mask[RotatedL45Map[to]];
                    r45Mask = BitMask.Mask[RotatedR45Map[from]] | BitMask.Mask[RotatedR45Map[to]];
                    r90Mask = BitMask.Mask[Rotated90Map[from]]|BitMask.Mask[Rotated90Map[to]];
                }
                    
                else
                {
                    from = 0;
                    to = 3;
                    moveMask = BitMask.Mask[from]|BitMask.Mask[to];
                    l45Mask = BitMask.Mask[RotatedL45Map[from]] | BitMask.Mask[RotatedL45Map[to]];
                    r45Mask = BitMask.Mask[RotatedR45Map[from]] | BitMask.Mask[RotatedR45Map[to]];
                    r90Mask = BitMask.Mask[Rotated90Map[from]]|BitMask.Mask[Rotated90Map[to]];
                }
                    
            }
            Rooks[SideToMove] ^= moveMask;
            Pieces[SideToMove] ^= moveMask;
            AllPieces ^= moveMask;
            AllPiecesL45 ^= l45Mask;
            AllPiecesR45 ^= r45Mask;
            AllPiecesR90 ^= r90Mask;
            HashKey ^= TranspositionTable.HashKeys[SideToMove,(int)Piece.Rook,from];
            HashKey ^= TranspositionTable.HashKeys[SideToMove,(int)Piece.Rook,to];
            
        }

        void UnmakeCapture(HistoryMove m)
        {
            var xside = SideToMove ^ 1;
            int sq = m.To;
            Piece p=Piece.Empty;
            if (m.CapturedPiece == MoveBits.Pawn)
            {
                p=Piece.Pawn;
                if (BitMask.Mask[m.To] == m.EnPassant)
                {
                    sq = SideToMove == 0 ? m.To + 8 : m.To - 8;
                    Pawns[xside] |= BitMask.Mask[sq];
                }
                else
                    Pawns[xside] |= BitMask.Mask[m.To];
            }
            if (m.CapturedPiece == MoveBits.Knight){
                Knights[xside] |= BitMask.Mask[m.To];
                p = Piece.Knight;
            }
            else if (m.CapturedPiece == MoveBits.Bishop){
                Bishops[xside] |= BitMask.Mask[m.To];
                p = Piece.Bishop;
            }
            else if (m.CapturedPiece == MoveBits.Rook){
                Rooks[xside] |= BitMask.Mask[m.To];
                p = Piece.Rook;
            }
            else if (m.CapturedPiece == MoveBits.Queen){
                Queens[xside] |= BitMask.Mask[m.To];
                p = Piece.Queen;
            }
            AllPieces ^= BitMask.Mask[sq];
            Pieces[xside] ^= BitMask.Mask[sq];
            AllPiecesL45 ^= BitMask.Mask[RotatedL45Map[sq]];
            AllPiecesR45 ^= BitMask.Mask[RotatedR45Map[sq]];
            AllPiecesR90 ^= BitMask.Mask[Rotated90Map[sq]];
            HashKey ^= TranspositionTable.HashKeys[SideToMove,(int)p,sq];
        }

        private void UpdateBoards(Piece p, Move m, int sideToMove, ulong moveMask)
        {
            Pieces[sideToMove] ^= moveMask;
            if (p==Piece.King)
                King[sideToMove] ^= moveMask;
            if (p==Piece.Bishop)
                Queens[sideToMove] ^= moveMask;
            if (p==Piece.Bishop)
                Bishops[sideToMove] ^= moveMask;
            if (p==Piece.Knight)
                Knights[sideToMove] ^= moveMask;
            if (p==Piece.Rook)
                Rooks[sideToMove] ^= moveMask;
            if (p==Piece.Pawn)
                Pawns[sideToMove] ^= moveMask;

            HashKey ^= TranspositionTable.HashKeys[sideToMove,(int)p,m.From];
            HashKey ^= TranspositionTable.HashKeys[sideToMove,(int)p,m.To];

        }
    }
}