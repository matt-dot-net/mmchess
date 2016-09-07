using System;
using System.Runtime.InteropServices;

namespace mmchess{
    [StructLayout(LayoutKind.Explicit,Size=8)]
    public class TranspositionTableEntry{

        public enum EntryType{
            PV=1,
            ALL=2,
            CUT=4
        }  ;

  
        public const int SizeOf = 8;

        [FieldOffset(0)]
        public UInt32 MoveValue;

        [FieldOffset(4)]
        public Byte Type;
        [FieldOffset(5)]
        public Byte DepthAge; //This field will use 2-bits for age and 6-bits for depth
        [FieldOffset(6)]
        public Int16 Score;
        
        [FieldOffset(0)]
        public ulong Value;

        [FieldOffset(8)]
        public uint Lock;

        public int Depth{get{
            return DepthAge & 63;
        }
        set{
            //preserve top two bits for age
            DepthAge &= 0xC0;   //1100 0000
            //set the bottom 6 bits as depth
            DepthAge |= (byte)(value&63); //0011 1111
        }}

        //we store the age in the top two bits of the DepthAge field
        public int Age {
            get{ return DepthAge>>6;}
            set {
                Depth &= 63; // clear current Age
                DepthAge |= (byte)((value & 3) << 6);// shift in the age 
            }
        }
    }
}