namespace mmchess{

    // public static class StaticExchange{
    //     public static int Eval(Board b, Move m)
    //     {
    //         ulong attackers, ulong[]xray_attackers;

    //         var sq = m.To;
    //         var attacks = MoveGenerator.Attacks(b,sq);
    //         var stm = b.SideToMove;

    //         attackers = attacks & b.Pieces[b.SideToMove];
    //         //we've established which squares have the attackers
    //         //to get xray attackers we remove these first attackers and compute again
    //         while(attackers>0){
    //             var attack_sq = attackers.BitScanForward();
    //             attackers ^= BitMask.Mask[attack_sq];

    //             //find which piece it is and remove it for the next iteration
    //             if((b.Pawns[stm] & attack_sq)>0)

                
    //         }


            
            
    //     }
    // }
}