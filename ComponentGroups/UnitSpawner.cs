using Atma.Entities;
using Worm.Managers;

public struct UnitSpawner : IEntitySpecGroup
{
    public Prefab Prefab;

    public override int GetHashCode() => Prefab.PrefabID.GetHashCode();
}