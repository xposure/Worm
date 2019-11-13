using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 0)]
public struct TextureRegion
{
    public float X0;
    public float Y0;

    public float X1;
    public float Y1;

    public TextureRegion(float x0, float y0, float x1, float y1)
    {
        X0 = x0;
        Y0 = y0;
        X1 = x1;
        Y1 = y1;
    }
}