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
        public UInt16 Score;
        [FieldOffset(0)]

        public uint Value;

        [FieldOffset(8)]
        public uint Lock;

        public int Depth{get{

            return (DepthAge >> 6) & 63;
        }}

        public int Age {
            get{ return DepthAge>>6;}
        }
    }
}