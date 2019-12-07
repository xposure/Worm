using Microsoft.Xna.Framework;

public struct Sprite
{
    public uint TextureID;

    public int Width, Height;

    //public float UVX0, UVY0, UVX1, UVY1;

    //public Color Color;
    //public float Rotation;
    public float OriginX;
    public float OriginY;

    public bool FlipX, FlipY;

    public Sprite(uint textureId, int width, int height)
    {
        TextureID = textureId;
        //Color = color;
        Width = width;
        Height = height;
        //Rotation = 0;
        OriginX = 0.5f;
        OriginY = 0.5f;
        FlipX = false;
        FlipY = false;
        // UVX0 = 0;
        // UVY0 = 0;
        // UVX1 = 1;
        // UVY1 = 1;

    }

    // public Sprite(uint textureId, Color color, int width, int height, float uvx0, float uvy0, float uvx1, float uvy1)
    // {
    //     TextureID = textureId;
    //     Color = color;
    //     Width = width;
    //     Height = height;
    //     UVX0 = uvx0;
    //     UVY0 = uvy0;
    //     UVX1 = uvx1;
    //     UVY1 = uvy1;

    // }
}