using Atma.Math;

public enum ColliderType
{
    Player,
    Enemy
}

public struct Collider
{
    public ColliderType Type;

    public AxisAlignedBox2 Bounds;

    public bool Disabled;
}