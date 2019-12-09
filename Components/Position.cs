
using System;
using System.Runtime.InteropServices;
using Atma;
using Atma.Math;

[StructLayout(LayoutKind.Sequential, Pack = 0)]
public struct Position
{
    public float X;
    public float Y;

    public Position(float x, float y)
    {
        this.X = x;
        this.Y = y;
    }

    public float this[int axis]
    {
        get
        {
            if (axis == 0)
                return X;
            else if (axis == 1)
                return Y;

            throw new ArgumentOutOfRangeException(nameof(axis));
        }
        set
        {
            if (axis == 0)
                X = value;
            else if (axis == 1)
                Y = value;
            else
                throw new ArgumentOutOfRangeException(nameof(axis));
        }
    }

    public static implicit operator float2(Position it) => new float2(it.X, it.Y);
    public static implicit operator Position(float2 it) => new Position(it.x, it.y);
}