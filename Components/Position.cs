
using System.Runtime.InteropServices;

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
}