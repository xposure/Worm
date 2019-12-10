using Atma.Math;

public struct Scale
{
    public float Width;
    public float Height;

    public Scale(float width, float height)
    {
        Width = width;
        Height = height;
    }

    public static implicit operator float2(Scale scale) => new float2(scale.Width, scale.Height);
}