public struct Color
{
    public uint PackedValue;

    public Color(uint packedValue)
    {
        PackedValue = packedValue;
    }

    public static readonly Color White = new Color() { PackedValue = 0xffffffff };
}