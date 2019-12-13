namespace Worm.Systems
{
    using System;
    using System.Collections.Generic;
    using Atma;
    using Atma.Common;
    using Atma.Entities;
    using Atma.Math;
    using Atma.Memory;
    using Atma.Systems;
    using Game.Framework;
    using Game.Framework.Managers;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Worm.Managers;
    using Color = global::Color;

    [Stages(nameof(RenderStage))]
    public class SpriteRenderer : SystemBase
    {
        private struct DebugLine
        {
            public float2 Min, Max;
            public Color Color;
        }

        private DrawContext _spriteBatch;
        private ITextureManager _textures;
        //private IAllocator _allocator;
        private EntitySpec _renderSpec;

        private List<EntityChunkList> _renderOrder = new List<EntityChunkList>();
        private Comparison<EntityChunkList> _renderSort;

        // private static NativeList<DebugLine> _debugLines;

        // public static void DebugDraw(in AxisAlignedBox2 box) => DebugDraw(box, Color.White);
        // public static void DebugDraw(in AxisAlignedBox2 box, in Color color)
        // {
        //     var tl = box.Min;
        //     var br = box.Max;
        //     var tr = new float2(br.x, tl.y);
        //     var bl = new float2(tl.x, br.y);

        //     _debugLines.Add(new DebugLine() { Min = tl, Max = tr, Color = color });
        //     _debugLines.Add(new DebugLine() { Min = bl, Max = br, Color = color });
        //     _debugLines.Add(new DebugLine() { Min = tl, Max = bl, Color = color });
        //     _debugLines.Add(new DebugLine() { Min = tr, Max = br, Color = color });
        // }

        public SpriteRenderer(ITextureManager texture, DrawContextFactory drawContextFactory)
        {
            _textures = texture;
            _spriteBatch = drawContextFactory.CreateDrawContext();
        }

        protected override void OnUnmanagedDispose()
        {
            //_debugLines.Dispose();
            //_spriteBatch.Dispose();
            _renderOrder.Clear();
        }

        protected override void OnGatherDependencies(DependencyListConfig config)
        {
            config.Read<Position>();
            config.Read<Scale>();
            config.Read<Color>();
            config.Read<TextureRegion>();
            config.Read<Sprite>();
        }

        protected override void OnInit()
        {
            //_spriteBatch = new BetterSpriteBatch(Engine.Instance.Memory, Engine.Instance.GraphicsDevice);
            _renderSpec = EntitySpec.Create<Position, Sprite>();
            _renderSort = new Comparison<EntityChunkList>((x, y) =>
                            x.Specification.GetGroupData<RenderLayer>().Layer -
                            y.Specification.GetGroupData<RenderLayer>().Layer);
            //_debugLines = new NativeList<DebugLine>(Engine.Instance.Memory, 1024);
        }

        protected override void OnTick(SystemManager systemManager, EntityManager entityManager)
        {
            var em = entityManager;
            _renderOrder.Clear();
            _renderOrder.AddRange(em.EntityArrays.Filter(_renderSpec));
            _renderOrder.Sort(_renderSort);

            var defaultScale = new Scale(1, 1);
            var defaultColor = Color.White;
            var defaultRegion = new TextureRegion(0, 0, 1, 1);

            var width = Engine.Instance.GraphicsDevice.PresentationParameters.BackBufferWidth;
            var height = Engine.Instance.GraphicsDevice.PresentationParameters.BackBufferHeight;

            foreach (var renderGroup in _renderOrder)
            {
                em.ForEntity((uint cameraEntity, ref Camera camera, ref Position cameraPosition) =>
                {
                    _spriteBatch.Reset();
                    //_spriteBatch.SetSamplerState(SamplerState.PointClamp);

                    //_spriteBatch.SetCamera(Matrix.CreateTranslation(-cameraPosition.X + (width / 2), -cameraPosition.Y + (height / 2), 0));
                    _spriteBatch.SetCamera(float4x4.Translate(-cameraPosition.X + (width / 2), -cameraPosition.Y + (height / 2), 0));

                    var currentTexture = _textures["default"];
                    _spriteBatch.SetTexture(currentTexture);

                    var texelWidth = _spriteBatch.TexelWidth;
                    var texelHeight = _spriteBatch.TexelHeight;
                    var textureWidth = currentTexture.Width;
                    var textureHeight = currentTexture.Height;

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

                                if (currentTexture.ID != sprite.TextureID)
                                {
                                    _spriteBatch.SetTexture(_textures[sprite.TextureID]);
                                    texelWidth = _spriteBatch.TexelWidth;
                                    texelHeight = _spriteBatch.TexelHeight;
                                    textureWidth = currentTexture.Width;
                                    textureHeight = currentTexture.Height;
                                }

                                //TODO: rotation
                                var size = new Scale(sprite.Width, sprite.Height);
                                size.Width *= scale.Width;
                                size.Height *= scale.Height;

                                var p = new Position(position.X, position.Y);
                                p.X -= sprite.OriginX * size.Width;
                                p.Y -= sprite.OriginY * size.Height;

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
                });
            }

            // em.ForEntity((uint cameraEntity, ref Camera camera, ref Position cameraPosition) =>
            // {
            //     var region = new TextureRegion(0, 0, 1, 1);
            //     _spriteBatch.Reset();
            //     _spriteBatch.SetSamplerState(SamplerState.PointClamp);
            //     _spriteBatch.SetCamera(Matrix.CreateTranslation(-cameraPosition.X + (width / 2), -cameraPosition.Y + (height / 2), 0));

            //     for (var i = 0; i < _debugLines.Length; i++)
            //     {
            //         ref var line = ref _debugLines[i];
            //         // if (line.Min.y == line.Max.y)
            //         //     _spriteBatch.AddSprite(new float2(line.Min.x - 0.5f, line.Min.y), new Scale(line.Max.x - line.Min.x, 1), line.Color, region);
            //         // else if (line.Min.x == line.Max.x)
            //         //     _spriteBatch.AddSprite(new float2(line.Min.x, line.Min.y - 0.5f), new Scale(1, line.Max.y - line.Min.y), line.Color, region);
            //         if (line.Min.y == line.Max.y)
            //             _spriteBatch.AddSprite(line.Min, new Scale(line.Max.x - line.Min.x, 1), line.Color, region);
            //         else if (line.Min.x == line.Max.x)
            //             _spriteBatch.AddSprite(line.Min, new Scale(1, line.Max.y - line.Min.y), line.Color, region);
            //         else
            //             throw new NotImplementedException();
            //     }

            //     _spriteBatch.Render();
            // });
            // _debugLines.Reset();
        }
    }
}