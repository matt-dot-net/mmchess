using System;
using System.Runtime.InteropServices;

namespace mmchess{
    [StructLayout(LayoutKind.Explicit,Size=8)]
    public class TranspositionTableEntry{

        public enum EntType{
            EXACT=1,
            LOWER=2,
            UPPER=4
        }  ;

  
        public const int SizeOf = 8;

        [FieldOffset(0)]
        public UInt32 MoveValue;

        [FieldOffset(4)]
        public Byte Depth;
        [FieldOffset(5)]
        public Byte Type;
        [FieldOffset(6)]
        public Byte Score;
        [FieldOffset(7)]
        public Byte Age;
        [FieldOffset(0)]
        public uint Value;

        [FieldOffset(8)]
        public uint Lock;
    }
}