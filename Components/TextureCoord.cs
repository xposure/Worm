using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 0)]
public struct TextureCoord
{
    public float X;
    public float Y;

    public TextureCoord(float x, float y)
    {
        this.X = x;
        this.Y = y;
    }
}