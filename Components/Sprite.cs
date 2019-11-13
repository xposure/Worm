public struct Sprite
{
    public uint TextureID;
    public uint Layer;

    public Sprite(uint textureId, uint layer)
    {
        TextureID = textureId;
        Layer = layer;
    }
}