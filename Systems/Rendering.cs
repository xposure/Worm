namespace Worm.Systems
{
    using System;
    using Atma;
    using Atma.Common;
    using Atma.Entities;
    using Atma.Memory;
    using Microsoft.Xna.Framework;

    public class RenderingSystem : ISystem
    {
        private BetterSpriteBatch _spriteBatch;
        private IAllocator _allocator;

        public void Init()
        {
            _spriteBatch = new BetterSpriteBatch(Engine.Instance.Memory, Engine.Instance.GraphicsDevice);
        }

        public void Update(float dt)
        {
        }

        public void Draw(float dt)
        {
            var em = Engine.Instance.Entities;
            em.ForChunk((int length, ReadOnlySpan<EntityRef> entities, Span<Position> positions, Span<Color> colors) =>
            {

            });
        }
    }
}