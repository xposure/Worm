using System.Runtime.InteropServices;
using Atma;
using Microsoft.Xna.Framework.Graphics;

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

    public static TextureRegion FromTexture(Texture2D texture, int tileWidth, int tileHeight, int tileX, int tileY)
    {
        var tilesX = texture.Width / tileWidth;
        var tilesY = texture.Height / tileHeight;

        Assert.EqualTo(tilesX * tileWidth, texture.Width);
        Assert.EqualTo(tilesY * tileHeight, texture.Height);

        var tileW = 1f / tilesX;
        var tileH = 1f / tilesY;

        return FromSize(tileX * tileW, tileY * tileW, tileW, tileH);
    }

    public static TextureRegion FromSize(float uvx, float uvy, float width, float height) => new TextureRegion(uvx, uvy, uvx + width, uvy + height);

}