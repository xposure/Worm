namespace Worm.Systems
{
    using Atma;
    using Atma.Entities;

    public class ColliderSystem : ISystem
    {
        public void Dispose()
        {
        }

        public void Draw(float dt)
        {
        }

        public void Init()
        {
        }

        public void Update(float dt)
        {
            var em = Engine.Instance.Entities;
            foreach (var array in em.EntityArrays.Filter(default))
                foreach (var chunk in array.AllChunks)
                    CollisionCheck(chunk);

        }

        private void CollisionCheck(EntityChunk chunk)
        {
            var em = Engine.Instance.Entities;


        }
    }
}