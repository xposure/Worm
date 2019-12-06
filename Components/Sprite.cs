public struct Sprite
{
    public uint TextureID;

    public int TileX, TileY;

    public int Width, Height;

    public Sprite(uint textureId, int tileX, int tileY, int width, int height)
    {
        TextureID = textureId;
        TileX = tileX;
        TileY = tileY;
        Width = width;
        Height = height;
    }
}