// namespace mmchess{

//     public static class StaticExchange{
//         //evaluates exchanges on a square initiated by stm
//         public static int Eval(Board b, Move m, int stm)
//         {
//             ulong attackers, ulong[]xray_attackers;

//             var sq = m.To;
//             var attacks = MoveGenerator.Attacks(b,sq); 
//             var mystm = stm ^ 1; 
//             attackers = attacks & b.Pieces[mystm];

//             //we've established which squares have the attackers
//             //to get xray attackers we remove these first attackers and compute again
//             while(attackers>0){
//                 var attack_sq = attackers.BitScanForward();
//                 attackers ^= BitMask.Mask[attack_sq];

//                 //find which piece it is and remove it for the next iteration
//                 //only rooks, bishops, queens can be xray sliders
//                 if(b.Queens[mystm])

                
//             }


            
            
//         }
//     }
// }