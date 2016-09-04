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
        public Byte Age;
        [FieldOffset(6)]
        public UInt16 Score;
        [FieldOffset(0)]

        public uint Value;

        [FieldOffset(8)]
        public uint Lock;
    }
}