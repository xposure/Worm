namespace Worm.Systems
{
    using System;
    using System.Collections.Generic;
    using Atma;
    using Atma.Common;
    using Atma.Entities;
    using Atma.Memory;
    using Microsoft.Xna.Framework;

    public class RenderingSystem : ISystem
    {
        private BetterSpriteBatch _spriteBatch;
        private IAllocator _allocator;
        private EntitySpec _renderSpec;

        private List<EntityChunkList> _renderOrder = new List<EntityChunkList>();
        private Comparison<EntityChunkList> _renderSort;

        public void Init()
        {
            _spriteBatch = new BetterSpriteBatch(Engine.Instance.Memory, Engine.Instance.GraphicsDevice);
            _renderSpec = EntitySpec.Create<Position, Sprite>();
            _renderSort = new Comparison<EntityChunkList>((x, y) =>
                            x.Specification.GetGroupData<RenderLayer>().Layer -
                            y.Specification.GetGroupData<RenderLayer>().Layer);
        }

        public void Update(float dt)
        {

        }

        public void Draw(float dt)
        {
            var em = Engine.Instance.Entities;
            _renderOrder.Clear();
            _renderOrder.AddRange(em.EntityArrays.Filter(_renderSpec));
            _renderOrder.Sort(_renderSort);


            foreach (var renderGroup in _renderOrder)
            {
                _spriteBatch.Reset();
                //TODO: add the ability to sort entities? this could move a lot of data, its better to copy only data needed and sort that

                //renderGroup.

            }

            // em.ForChunk((int length, ReadOnlySpan<EntityRef> entities, Span<Position> positions, Span<Color> colors) =>
            // {

            // });
        }
    }
}