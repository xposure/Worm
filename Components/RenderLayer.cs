using Atma.Entities;
public struct RenderLayer : IEntitySpecGroup
{
    public readonly int Layer;
    public RenderLayer(int layer)
    {
        Layer = layer;
    }

    public override int GetHashCode() => Layer;
}