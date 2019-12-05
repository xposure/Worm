public struct Sprite
{
    public uint TextureID;

    public uint Layer;

    public int TileX, TileY;

    public int Width, Height;

    public Sprite(uint textureId, uint layer, int tileX, int tileY, int width, int height)
    {
        TextureID = textureId;
        Layer = layer;
        TileX = tileX;
        TileY = tileY;
        Width = width;
        Height = height;
    }
}