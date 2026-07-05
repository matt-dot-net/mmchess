using System;

namespace mmchess;

public static class SquareExtensions
{
    public static int File(this int square)
    {
        return square & 7;
    }
    public static byte File(this byte square){
        return (byte)(square & 7);
    }

    public static int Rank(this int square)
    {
        return square >> 3;
    }
    public static byte Rank(this byte square){
        return (byte)(square >> 3);
    }
    public static int FileDistance(int a, int b)
    {
        return Math.Abs(b.File() - a.File());
    }

    //a8 (square 0) is a light square; color alternates with rank+file parity
    public static bool IsLightSquare(this int square)
    {
        return ((square.Rank() + square.File()) & 1) == 0;
    }

    //Chebyshev distance - the number of king moves between two squares
    public static int KingDistance(int a, int b)
    {
        return Math.Max(Math.Abs(b.File() - a.File()), Math.Abs(b.Rank() - a.Rank()));
    }
}