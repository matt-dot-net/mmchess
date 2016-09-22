using System;

namespace mmchess
{

    public static class StaticExchange
    {
        //evaluates exchanges on a square initiated by stm
        public static int Eval(Board b, Move m, int stm)
        {

            ulong[] attackers=new ulong[2];
            int score=0;
            var sq = m.To;
            var pieceVal = Evaluator.PieceValueOnSquare(b,sq);
            var attacks = MoveGenerator.Attacks(b, sq);
            var mystm = stm ^ 1;
            attackers[mystm] = attacks & b.Pieces[mystm];
            attackers[stm] = attacks & b.Pieces[stm];


            //if there is a promotion, then stm is already up material
            if(m.Promotion>0)
                score= Evaluator.PieceValues[m.Promotion];

            //add in any material taken on the first capture
            if((m.Bits & (byte)MoveBits.Capture)>0)
                score+=Evaluator.PieceValueOnSquare(b,m.To);

            //change the pieceVal to be the new piece
            pieceVal = Evaluator.PieceValues[(int)Move.GetPiece((MoveBits)m.Bits)];

            //remove the first Move
            attackers[stm] ^= BitMask.Mask[m.From];

            //make a pass over the attackers to find the lowest valued piece
            while (attackers[mystm] > 0)
            {
                var tempAttackers = attackers[mystm];
                var leastValuable = int.MaxValue;
                int leastValSq = -1;
                while (tempAttackers > 0)
                {
                    var attack_sq = tempAttackers.BitScanForward();
                    tempAttackers ^= BitMask.Mask[attack_sq];
                    var attackerVal = Evaluator.PieceValueOnSquare(b, attack_sq);
                    if (attackerVal < leastValuable)
                    {
                        leastValSq = attack_sq;
                        leastValuable = attackerVal;
                    }
                }

                //now we have the least valuable attacker
                //remove whatever value is on the square
                if(mystm==stm)
                    score+=pieceVal;
                else
                    score-=pieceVal;
                
                //update the attacked piece
                pieceVal = leastValuable;

                //remove the attacker
                attackers[mystm] ^= BitMask.Mask[leastValSq];

                //now we check for x-ray attacks 
                //the idea here is that if any rooks, queens, or bishops attack the temporary
                //square from the same direction that the temporary square is attacking the target
                //then we have an xray attacker
                var direction = MoveGenerator.Directions[leastValSq,sq];
                ulong xrayAttacks=0;
                switch(Math.Abs(direction)){
                    case 1:
                    case 8:
                        if(((b.Rooks[mystm]|b.Queens[mystm]) & Board.FileMask[sq.File()])>0)
                            xrayAttacks = MoveGenerator.RookAttacks(b,leastValSq) & (b.Rooks[mystm]|b.Queens[mystm]) & attackers[mystm];
                        break;
                    case 7:
                    case 9:
                            xrayAttacks = MoveGenerator.BishopAttacks(b,leastValSq) & (b.Bishops[mystm]|b.Queens[mystm]) & attackers[mystm];
                        break;
                }

                while(xrayAttacks >0){
                    var xraySq =xrayAttacks.BitScanForward();
                    xrayAttacks ^= BitMask.Mask[xraySq];
                    if(MoveGenerator.Directions[xraySq,sq]==direction){
                        //put this piece in main attackers bitboard
                        attackers[mystm] |= BitMask.Mask[xraySq];
                    }
                }
                //switch stm
                mystm ^= 1;
            }

            return score;
        }
    }
}