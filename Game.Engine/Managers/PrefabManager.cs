namespace Worm.Managers
{
    using System.Collections.Generic;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using HashCode = Atma.HashCode;
    using Atma.Entities;
    using Microsoft.Xna.Framework;

    public interface IPrefab
    {
        void Create(in EntityCommandBuffer buffer);
    }

    public class Prefab : IEquatable<Prefab>
    {
        public readonly uint PrefabID;
        public readonly EntitySpec Specification;
        public readonly IPrefab Creator;

        public Prefab(uint prefabID, EntitySpec spec, IPrefab creator)
        {
            PrefabID = prefabID;
            Specification = spec;
            Creator = creator;
        }

        public bool Equals([AllowNull] Prefab other)
        {
            if (PrefabID != other.PrefabID)
                return false;

            if (Specification.ID != other.Specification.ID)
                return false;

            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            return Equals((Prefab)obj);
        }

        public unsafe override int GetHashCode()
        {
            var hasher = stackalloc HashCode[1];
            hasher->Add(PrefabID);
            hasher->Add(Specification.ID);
            return hasher->ToHashCode();
        }

        public void Create(in EntityCommandBuffer buffer)
        {
            buffer.Create(Specification);
            Creator.Create(buffer);
        }
        public static bool operator ==(Prefab a, Prefab b) => a.Equals(b);
        public static bool operator !=(Prefab a, Prefab b) => !a.Equals(b);
    }

    public static class Prefabs
    {
        private static uint _PrefabId = 0;

        public static Prefab Player;

        private static Dictionary<uint, Prefab> _prefabs = new Dictionary<uint, Prefab>();

        public static void Init()
        {
            //Player = InitPlayer();
        }

        // private unsafe static Prefab InitPlayer()
        // {
        //     Span<ComponentType> componentTypes = stackalloc[] {
        //         ComponentType<Position>.Type,
        //         ComponentType<Velocity>.Type,
        //         ComponentType<Sprite>.Type,
        //         ComponentType<Scale>.Type,
        //         ComponentType<SpriteAnimation>.Type,
        //         ComponentType<Color>.Type,
        //         ComponentType<TextureRegion>.Type,
        //         ComponentType<PlayerInput>.Type,
        //         ComponentType<PlayerUnitSelect>.Type,
        //     };
        //     return AddPrefab<PlayerPrefab>(new EntitySpec(componentTypes));
        // }

        public static Prefab AddPrefab<T>(EntitySpec spec)
            where T : IPrefab, new()
        {
            var t = new T();
            var prefab = new Prefab(_PrefabId++, spec, t);
            _prefabs.Add(prefab.PrefabID, prefab);
            return prefab;
        }

        public static Prefab GetPrefab(uint id) => _prefabs[id];

        public static void Dispose()
        {
            _prefabs.Clear();
        }
    }
}