namespace Game.Framework.Services.Graphics
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Atma;
    using Atma.Common;
    using Atma.Math;

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct SpriteVertex
    {
        [VertexElement(VertexElementType.Float2, VertexSemantic.Position)]
        public Position Position;

        [VertexElement(VertexElementType.Color, VertexSemantic.Color)]
        public uint Color;

        [VertexElement(VertexElementType.Float2, VertexSemantic.Texture)]
        public TextureCoord TextureCoord;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    [VertexGroup(typeof(SpriteVertex))]
    public struct GpuSprite
    {
        public SpriteVertex TL;
        public SpriteVertex TR;
        public SpriteVertex BL;
        public SpriteVertex BR;

    }

    [GameService]
    public interface IDrawContextFactory
    {
        DrawContext CreateDrawContext();
    }

    public class DrawContextFactory : IDrawContextFactory
    {
        private readonly ITextureManager _textures;
        private readonly IGraphicsBufferFactory _bufferFactory;
        private readonly IRenderCommandFactory _renderCommandFactory;
        public DrawContextFactory(ITextureManager textures, IGraphicsBufferFactory bufferFactory, IRenderCommandFactory renderCommandFactory)
        {
            _textures = textures;
            _bufferFactory = bufferFactory;
            _renderCommandFactory = renderCommandFactory;
        }

        public DrawContext CreateDrawContext() => new DrawContext(_textures, _bufferFactory, _renderCommandFactory.Create());
    }

    public readonly ref struct BulkSrpiteOperation
    {
        private readonly DrawContext _batch;
        public readonly Span<GpuSprite> Sprites;

        public BulkSrpiteOperation(DrawContext batch, Span<GpuSprite> sprites)
        {
            _batch = batch;
            Sprites = sprites;
        }

        public void Dispose()
        {
            _batch.CompleteBulkOperation(this);
        }
    }


    public class DrawContext : UnmanagedDispose
    {
        private const int MAX_PRIMITIVES = 10922;
        private const int MAX_SPRITES = MAX_PRIMITIVES * 2;
        //private const int MAX_VERTICES = MAX_PRIMITIVES * 4;
        private const int MAX_INDICIES = MAX_PRIMITIVES * 6;

        private readonly static ushort[] IndexData;

        static DrawContext()
        {
            IndexData = new ushort[MAX_INDICIES];
            var verts = 0;
            for (int i = 0; i < IndexData.Length;)
            {
                IndexData[i++] = (ushort)(verts + 0);
                IndexData[i++] = (ushort)(verts + 1);
                IndexData[i++] = (ushort)(verts + 2);
                IndexData[i++] = (ushort)(verts + 1);
                IndexData[i++] = (ushort)(verts + 3);
                IndexData[i++] = (ushort)(verts + 2);
                verts += 4;
            }
        }

        private ITextureManager _textures;
        private IGraphicsBufferFactory _bufferFactory;

        private IRenderCommandBuffer _renderCommands;
        private ITexture2D _defaultTexture;
        private ITexture2D _currentTexture = null;
        private IIndexBuffer16 _indexBuffer;

        private int _primitiveCount = 0;
        //private int _vertexPosition = 0;
        private GpuSprite[] _sprites;
        private int _spriteIndex = 0;
        private IVertexBuffer<GpuSprite> _vertexBuffer;
        private ObjectPool<IVertexBuffer<GpuSprite>> _bufferPool;
        private List<IVertexBuffer<GpuSprite>> _usedBuffers0 = new List<IVertexBuffer<GpuSprite>>();
        private List<IVertexBuffer<GpuSprite>> _usedBuffers1 = new List<IVertexBuffer<GpuSprite>>();
        private List<IVertexBuffer<GpuSprite>> _allBuffers = new List<IVertexBuffer<GpuSprite>>();
        private bool _isInBulkOperation = false;


        public int Triangles => _renderCommands.Triangles;
        public int Commands => _renderCommands.Commands;

        public DrawContext(ITextureManager textures, IGraphicsBufferFactory bufferFactory, IRenderCommandBuffer renderCommands)
        {
            _textures = textures;
            _bufferFactory = bufferFactory;
            _renderCommands = renderCommands;

            _bufferPool = new ObjectPool<IVertexBuffer<GpuSprite>>(() =>
            {
                var buffer = _bufferFactory.CreateVertex<GpuSprite>(MAX_SPRITES * 4, true);
                _allBuffers.Add(buffer);
                return buffer;
            });
            _sprites = new GpuSprite[MAX_SPRITES];

            _defaultTexture = _textures["default"];//  new Texture2D(_device, 1, 1);
            //_defaultTexture.SetData(new[] { Color.White });

            _indexBuffer = _bufferFactory.CreateIndex16(IndexData.Length, false);
            _indexBuffer.SetData(IndexData);

            for (var i = 0; i < MAX_SPRITES; ++i)
            {
                ref var sprite = ref _sprites[i];
                sprite.TL.TextureCoord = new TextureCoord(0, 0);
                sprite.TR.TextureCoord = new TextureCoord(1, 0);
                sprite.BR.TextureCoord = new TextureCoord(1, 1);
                sprite.BL.TextureCoord = new TextureCoord(0, 1);
            }
            Reset();
        }



        public float TexelWidth => 1f / _currentTexture.Width;
        public float TexelHeight => 1f / _currentTexture.Height;

        public void SetTexture(ITexture2D texture)
        {
            Assert.EqualTo(_isInBulkOperation, false);
            if (_currentTexture != texture)
            {
                FlushRender();
                _renderCommands.SetTexture(texture);
                _currentTexture = texture;
            }
        }

        // public void SetBlendState(BlendState blendState)
        // {
        //     Assert.EqualTo(_isInBulkOperation, false);
        //     FlushRender();
        //     _renderCommands.SetBlendState(blendState);
        // }
        // public void SetDepthState(DepthStencilState depthState)
        // {
        //     Assert.EqualTo(_isInBulkOperation, false);
        //     FlushRender();
        //     _renderCommands.SetDepthState(depthState);
        // }
        // public void SetRasterizerState(RasterizerState rasitizerState)
        // {
        //     Assert.EqualTo(_isInBulkOperation, false);
        //     FlushRender();
        //     _renderCommands.SetRasterizerState(rasitizerState);
        // }

        public void SetCamera(in float4x4 matrix)
        {
            Assert.EqualTo(_isInBulkOperation, false);
            FlushRender();
            _renderCommands.SetCamera(RenderCameraType.World, matrix);
        }

        // public void SetSamplerState(SamplerState samplerState)
        // {
        //     Assert.EqualTo(_isInBulkOperation, false);
        //     FlushRender();
        //     _renderCommands.SetSamplerState(samplerState);
        // }
        // public void SetEffect(Effect effect)
        // {
        //     Assert.EqualTo(_isInBulkOperation, false);
        //     FlushRender();
        //     _renderCommands.SetEffect(effect);
        // }

        // //MonoGame draw method
        // public void MonoGameDraw(Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
        //     => MonoGameDraw(texture, position, sourceRectangle, color, rotation, origin, new Vector2(scale, scale), effects, layerDepth);

        // public void MonoGameDraw(Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth)
        // {
        //     SetTexture(texture);

        //     origin = origin * scale;
        //     TextureRegion _texCoord = new TextureRegion(0, 0, 1, 1);


        //     var texelWidth = 1f / texture.Width;
        //     var texelHeight = 1f / texture.Height;
        //     float w, h;
        //     if (sourceRectangle.HasValue)
        //     {
        //         var srcRect = sourceRectangle.GetValueOrDefault();
        //         w = srcRect.Width * scale.X;
        //         h = srcRect.Height * scale.Y;
        //         _texCoord.X0 = srcRect.X * texelWidth;
        //         _texCoord.Y0 = srcRect.Y * texelHeight;
        //         _texCoord.X1 = (srcRect.X + srcRect.Width) * texelWidth;
        //         _texCoord.Y1 = (srcRect.Y + srcRect.Height) * texelHeight;
        //     }
        //     else
        //     {
        //         w = texture.Width * scale.X;
        //         h = texture.Height * scale.Y;
        //         // _texCoord.X0 = 0;
        //         // _texCoord.Y0 = 0;
        //         // _texCoord.X1 = 1;
        //         // _texCoord.Y1 = 1;
        //     }

        //     if ((effects & SpriteEffects.FlipVertically) != 0)
        //     {
        //         var temp = _texCoord.Y1;
        //         _texCoord.Y1 = _texCoord.Y0;
        //         _texCoord.Y0 = temp;
        //     }
        //     if ((effects & SpriteEffects.FlipHorizontally) != 0)
        //     {
        //         var temp = _texCoord.X1;
        //         _texCoord.X1 = _texCoord.X0;
        //         _texCoord.X0 = temp;
        //     }

        //     if (rotation == 0f)
        //     {
        //         var p = new Position(position.X - origin.X, position.Y - origin.Y);
        //         var s = new Scale(w, h);
        //         AddSprite(texture, p, s, color, _texCoord);
        //     }
        //     else
        //     {
        //         MonoGameAddSprite(texture,
        //                 position.X,
        //                 position.Y,
        //                 -origin.X,
        //                 -origin.Y,
        //                 w,
        //                 h,
        //                 (float)Math.Sin(rotation),
        //                 (float)Math.Cos(rotation),
        //                 color,
        //                 new Vector2(_texCoord.X0, _texCoord.Y0),
        //                 new Vector2(_texCoord.X1, _texCoord.Y1),
        //                 layerDepth);
        //     }

        // }

        // public void MonoGameAddSprite(Texture2D texture, float x, float y, float dx, float dy, float w, float h, float sin, float cos, Color color, Vector2 texCoordTL, Vector2 texCoordBR, float depth)
        // {
        //     Assert.EqualTo(_isInBulkOperation, false);
        //     SetTexture(texture);

        //     ref var sprite = ref _sprites[_spriteIndex++];

        //     sprite.TL.Position.X = x + dx * cos - dy * sin;
        //     sprite.TL.Position.Y = y + dx * sin + dy * cos;
        //     sprite.TL.Color = color;
        //     sprite.TL.TextureCoord.X = texCoordTL.X;
        //     sprite.TL.TextureCoord.Y = texCoordTL.Y;

        //     sprite.TR.Position.X = x + (dx + w) * cos - dy * sin;
        //     sprite.TR.Position.Y = y + (dx + w) * sin + dy * cos;
        //     sprite.TR.Color = color;
        //     sprite.TR.TextureCoord.X = texCoordBR.X;
        //     sprite.TR.TextureCoord.Y = texCoordTL.Y;

        //     sprite.BL.Position.X = x + dx * cos - (dy + h) * sin;
        //     sprite.BL.Position.Y = y + dx * sin + (dy + h) * cos;
        //     sprite.BL.Color = color;
        //     sprite.BL.TextureCoord.X = texCoordTL.X;
        //     sprite.BL.TextureCoord.Y = texCoordBR.Y;

        //     sprite.BR.Position.X = x + (dx + w) * cos - (dy + h) * sin;
        //     sprite.BR.Position.Y = y + (dx + w) * sin + (dy + h) * cos;
        //     sprite.BR.Color = color;
        //     sprite.BR.TextureCoord.X = texCoordBR.X;
        //     sprite.BR.TextureCoord.Y = texCoordBR.Y;

        //     _primitiveCount += 2;

        //     if (_spriteIndex == MAX_SPRITES)
        //         CompleteBuffer();
        // }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddSprite(in Position position, in Scale scale, in Color color, in TextureRegion texCoord)
        {
            ref var sprite = ref _sprites[_spriteIndex++];

            var packedColor = color.PackedValue;
            sprite.TL.Position = position;
            sprite.TR.Position.X = position.X + scale.Width;
            sprite.TR.Position.Y = position.Y;
            sprite.BR.Position.X = position.X + scale.Width;
            sprite.BR.Position.Y = position.Y + scale.Height;
            sprite.BL.Position.X = position.X;
            sprite.BL.Position.Y = position.Y + scale.Height;

            sprite.TL.Color = packedColor;
            sprite.TR.Color = packedColor;
            sprite.BR.Color = packedColor;
            sprite.BL.Color = packedColor;

            sprite.TL.TextureCoord.X = texCoord.X0;
            sprite.TL.TextureCoord.Y = texCoord.Y0;
            sprite.TR.TextureCoord.X = texCoord.X1;
            sprite.TR.TextureCoord.Y = texCoord.Y0;
            sprite.BR.TextureCoord.X = texCoord.X1;
            sprite.BR.TextureCoord.Y = texCoord.Y1;
            sprite.BL.TextureCoord.X = texCoord.X0;
            sprite.BL.TextureCoord.Y = texCoord.Y1;

            _primitiveCount += 2;

            if (_spriteIndex == MAX_SPRITES)
                CompleteBuffer();
        }

        public void AddSprite(ITexture2D texture, in Position position, in Scale scale) => AddSprite(texture, position, scale, Color.White, new TextureRegion(0, 0, 1, 1));
        public void AddSprite(ITexture2D texture, in Position position, in Scale scale, in Color color) => AddSprite(texture, position, scale, color, new TextureRegion(0, 0, 1, 1));
        public void AddSprite(ITexture2D texture, in Position position, in Scale scale, in Color color, in TextureRegion texCoord)
        {
            Assert.EqualTo(_isInBulkOperation, false);
            SetTexture(texture);
            AddSprite(position, scale, color, texCoord);
        }

        public BulkSrpiteOperation TakeSprites(int requestCount)
        {
            Assert.EqualTo(_isInBulkOperation, false);
            _isInBulkOperation = true;
            var remainingSprites = MAX_SPRITES - _spriteIndex;
            if (remainingSprites > requestCount)
                return new BulkSrpiteOperation(this, _sprites.AsSpan().Slice(_spriteIndex, requestCount));

            return new BulkSrpiteOperation(this, _sprites.AsSpan().Slice(_spriteIndex, remainingSprites));
        }

        public void CompleteBulkOperation(BulkSrpiteOperation op)
        {
            Assert.EqualTo(_isInBulkOperation, true);
            _isInBulkOperation = false;

            _spriteIndex += op.Sprites.Length;
            _primitiveCount += op.Sprites.Length * 2;

            if (_spriteIndex == MAX_SPRITES)
                CompleteBuffer();
        }

        private void FlushRender()
        {
            if (_primitiveCount > 0)
            {
                var startIndex = _spriteIndex * 4 - _primitiveCount * 2;
                _renderCommands.RenderPrimitives(startIndex, _primitiveCount);
                _primitiveCount = 0;
            }
        }
        private void CompleteBuffer(bool lastRender = false)
        {
            FlushRender();

            if (_spriteIndex > 0)
            {
                _vertexBuffer.SetData(_sprites, 0, _spriteIndex);//, 0, _vertexPosition, SetDataOptions.Discard);
                _spriteIndex = 0;
            }

            if (!lastRender)
                UpdateNextVertexBuffer();
        }

        public void Render()
        {
            Assert.EqualTo(_isInBulkOperation, false);
            _renderCommands.UpdateProjection();
            CompleteBuffer(true);
            _renderCommands.Render();
            Reset();
        }

        public void Reset()
        {
            //TODO: renderCommand should support updating effect params (like WVP)
            _renderCommands.ResetState();

            _renderCommands.SetTexture(_defaultTexture);
            _renderCommands.SetIndexBuffer(_indexBuffer);

            _currentTexture = _defaultTexture;

            ReturnUsedBufers();
            UpdateNextVertexBuffer();
        }

        private void ReturnUsedBufers()
        {
            //we want to double buffer these buffers
            //i don't know if this is needed any more, but in the past
            //you could get gpu stalling when buffers were still being wrote to the gpu async
            var buffers = _usedBuffers1;
            _usedBuffers1 = _usedBuffers0;
            _usedBuffers0 = buffers;

            foreach (var it in buffers)
                _bufferPool.Return(it);

            buffers.Clear();
        }

        private void UpdateNextVertexBuffer()
        {
            _vertexBuffer = _bufferPool.Take();
            _usedBuffers0.Add(_vertexBuffer);

            _renderCommands.SetVertexBuffer(_vertexBuffer);
            _spriteIndex = 0;
            _primitiveCount = 0;
        }

        protected override void OnUnmanagedDispose()
        {
            _vertexBuffer = null;
            _usedBuffers0.Clear();
            _usedBuffers1.Clear();
            _allBuffers.DisposeAll();
            _defaultTexture.Dispose();
            _renderCommands.Dispose();
            _indexBuffer.Dispose();
        }
    }
}