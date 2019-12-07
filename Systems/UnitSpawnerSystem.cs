namespace Worm.Systems
{
    public class UnitSpawnerSystem : ISystem
    {

        public void Dispose()
        {
        }

        public void Draw(float dt)
        {
        }

        public unsafe void Init()
        {

        }

        public void Update(float dt)
        {
            var em = Engine.Instance.Entities;
            using var buffer = em.CreateCommandBuffer();
            em.ForEntityGroup((uint entity, UnitSpawner groupSpawner, ref Position position) =>
            {
                groupSpawner.Prefab.Create(buffer);
                if (groupSpawner.Prefab.Specification.Has<Position>())
                    buffer.Replace(position);
                buffer.Delete(entity);
            });
            buffer.Execute();
        }
    }
}