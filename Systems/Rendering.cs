namespace Worm.Systems
{
    using System;
    using System.Collections.Generic;
    using Atma;
    using Atma.Common;
    using Atma.Entities;
    using Atma.Memory;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Worm.Graphics;
    using Worm.Managers;

    public class RenderingSystem : UnmanagedDispose, ISystem
    {
        private BetterSpriteBatch _spriteBatch;
        //private IAllocator _allocator;
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

            var defaultScale = new Scale(1, 1);
            var defaultColor = Color.White;
            var defaultRegion = new TextureRegion(0, 0, 1, 1);

            foreach (var renderGroup in _renderOrder)
            {
                _spriteBatch.Reset();
                _spriteBatch.SetSamplerState(SamplerState.PointClamp);

                var currentTexture = Sprites.Missing;
                _spriteBatch.SetTexture(Sprites.Missing);

                var texelWidth = _spriteBatch.TexelWidth;
                var texelHeight = _spriteBatch.TexelHeight;
                var textureWidth = currentTexture.GpuTexture.Width;
                var textureHeight = currentTexture.GpuTexture.Height;

                //var renderLayer = renderGroup.Specification.GetGroupData<RenderLayer>();

                var spriteComponentIndex = renderGroup.Specification.GetComponentIndex<Sprite>();
                var positionComponentIndex = renderGroup.Specification.GetComponentIndex<Position>();
                var scaleComponentIndex = renderGroup.Specification.GetComponentIndex<Scale>();
                var colorComponentIndex = renderGroup.Specification.GetComponentIndex<Color>();
                var regionComponentIndex = renderGroup.Specification.GetComponentIndex<TextureRegion>();

                for (var k = 0; k < renderGroup.AllChunks.Count; k++)
                {
                    //using var grpSprites = _spriteBatch.TakeSprites(renderGroup.EntityCount);
                    var chunk = renderGroup[k];
                    if (chunk.Count > 0)
                    {
                        var sprites = chunk.GetComponentData<Sprite>(spriteComponentIndex);
                        var positions = chunk.GetComponentData<Position>(positionComponentIndex);
                        var scales = scaleComponentIndex > -1 ? chunk.GetComponentData<Scale>(scaleComponentIndex) : stackalloc[] { defaultScale };
                        var colors = colorComponentIndex > -1 ? chunk.GetComponentData<Color>(colorComponentIndex) : stackalloc[] { defaultColor };
                        var regions = regionComponentIndex > -1 ? chunk.GetComponentData<TextureRegion>(regionComponentIndex) : stackalloc[] { defaultRegion };

                        //var colors = 
                        //TODO: is it going to be faster to lookup for texture count and take sprites or to just call add sprite and let it do it?
                        for (var i = 0; i < chunk.Count; i++)
                        {
                            //ref var gpuSprite = ref grpSprites.Sprites[i];
                            ref var sprite = ref sprites[i];
                            ref var position = ref positions[i];
                            ref var scale = ref scales[scaleComponentIndex > -1 ? i : 0];
                            ref var color = ref colors[colorComponentIndex > -1 ? i : 0];
                            ref var region = ref regions[regionComponentIndex > -1 ? i : 0];

                            if (currentTexture != sprite.TextureID)
                            {
                                _spriteBatch.SetTexture(Sprites.GetTexture(sprite.TextureID));
                                texelWidth = _spriteBatch.TexelWidth;
                                texelHeight = _spriteBatch.TexelHeight;
                                textureWidth = currentTexture.GpuTexture.Width;
                                textureHeight = currentTexture.GpuTexture.Height;
                            }

                            //TODO: rotation
                            var size = new Scale(sprite.Width, sprite.Height);
                            size.Width *= scale.Width;
                            size.Height *= scale.Height;

                            var p = new Position(position.X, position.Y);
                            p.X += sprite.OriginX * size.Width;
                            p.Y += sprite.OriginY * size.Height;

                            if (sprite.FlipX)
                            {
                                p.X += size.Width;
                                size.Width = -size.Width;
                            }

                            if (sprite.FlipY)
                            {
                                p.Y += size.Height;
                                size.Height = -size.Height;
                            }

                            _spriteBatch.AddSprite(p, size, color, region);

                            // //positions
                            // if (scaleComponentIndex > -1)
                            // {
                            //     ref var scale = ref scales[i];

                            //     gpuSprite.TL.Position.X = position.X;
                            //     gpuSprite.TL.Position.Y = position.Y;

                            //     gpuSprite.TR.Position.X = position.X + sprite.Width * scale.Width;
                            //     gpuSprite.TR.Position.Y = position.Y;

                            //     gpuSprite.BR.Position.X = position.X + sprite.Width * scale.Width;
                            //     gpuSprite.BR.Position.Y = position.Y + sprite.Height * scale.Height;

                            //     gpuSprite.BL.Position.X = position.X;
                            //     gpuSprite.BL.Position.Y = position.Y + sprite.Height * scale.Height;
                            // }
                            // else
                            // {
                            //     gpuSprite.TL.Position.X = position.X;
                            //     gpuSprite.TL.Position.Y = position.Y;

                            //     gpuSprite.TR.Position.X = position.X + sprite.Width;
                            //     gpuSprite.TR.Position.Y = position.Y;

                            //     gpuSprite.BR.Position.X = position.X + sprite.Width;
                            //     gpuSprite.BR.Position.Y = position.Y + sprite.Height;

                            //     gpuSprite.BL.Position.X = position.X;
                            //     gpuSprite.BL.Position.Y = position.Y + sprite.Height;
                            // }

                            // //color
                            // gpuSprite.TL.Color = sprite.Color;
                            // gpuSprite.TR.Color = sprite.Color;
                            // gpuSprite.BR.Color = sprite.Color;
                            // gpuSprite.BL.Color = sprite.Color;


                            // //texture coords
                            // gpuSprite.TL.TextureCoord.X = sprite.UVX0;
                            // gpuSprite.TL.TextureCoord.Y = sprite.UVY0;

                            // gpuSprite.TR.TextureCoord.X = sprite.UVX1;
                            // gpuSprite.TR.TextureCoord.Y = sprite.UVY0;

                            // gpuSprite.BR.TextureCoord.X = sprite.UVX1;
                            // gpuSprite.BR.TextureCoord.Y = sprite.UVY1;

                            // gpuSprite.BL.TextureCoord.X = sprite.UVX0;
                            // gpuSprite.BL.TextureCoord.Y = sprite.UVY1;
                        }
                    }
                }

                _spriteBatch.Render();
            }
        }
        protected override void OnUnmanagedDispose()
        {
            _spriteBatch.Dispose();
            _renderOrder.Clear();
        }

    }
}