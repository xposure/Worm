using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Atma;
using Atma.Common;
using Atma.Memory;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Worm
{
    [StructLayout(LayoutKind.Sequential, Pack = 0)]

    public struct SpriteVertex
    {
        public Position Position;
        public Color Color;
        public TextureCoord TextureCoord;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
            new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
            new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
        );
    }


    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct Sprite
    {
        public SpriteVertex TL;
        public SpriteVertex TR;
        public SpriteVertex BL;
        public SpriteVertex BR;
    }

    public readonly ref struct BulkSrpiteOperation
    {
        private readonly BetterSpriteBatch _batch;
        public readonly Span<Sprite> Sprites;

        public BulkSrpiteOperation(BetterSpriteBatch batch, Span<Sprite> sprites)
        {
            _batch = batch;
            Sprites = sprites;
        }

        public void Dispose()
        {
            _batch.CompleteBulkOperation(this);
        }
    }

    public class BetterSpriteBatch : UnmanagedDispose
    {

        private const int MAX_PRIMITIVES = 10922;
        private const int MAX_SPRITES = MAX_PRIMITIVES * 2;
        //private const int MAX_VERTICES = MAX_PRIMITIVES * 4;
        private const int MAX_INDICIES = MAX_PRIMITIVES * 6;

        private readonly static ushort[] IndexData;

        static BetterSpriteBatch()
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


        private RenderCommandBuffer _renderCommands;
        private BasicEffect _defaultEffect;
        private GraphicsDevice _device;
        private Texture2D _defaultTexture;
        private DepthStencilState _defaultDepth = DepthStencilState.Default;
        private BlendState _defaultBlend = BlendState.NonPremultiplied;
        private RasterizerState _defaultRasterizer = RasterizerState.CullNone;
        private SamplerState _defaultSampler = SamplerState.PointClamp;

        private Effect Effect => _defaultEffect;

        private Texture _currentTexture = null;

        private IndexBuffer _indexBuffer;

        private int _primitiveCount = 0;
        //private int _vertexPosition = 0;
        private Sprite[] _sprites;
        private int _spriteIndex = 0;
        private DynamicVertexBuffer _vertexBuffer;
        private ObjectPool<DynamicVertexBuffer> _bufferPool;
        private List<DynamicVertexBuffer> _usedBuffers0 = new List<DynamicVertexBuffer>();
        private List<DynamicVertexBuffer> _usedBuffers1 = new List<DynamicVertexBuffer>();
        private List<VertexBuffer> _allBuffers = new List<VertexBuffer>();
        private bool _isInBulkOperation = false;

        private Viewport _lastViewport = new Viewport();

        public int Triangles => _renderCommands.Triangles;
        public int Commands => _renderCommands.Commands;

        public BetterSpriteBatch(IAllocator allocator, GraphicsDevice device)
        {
            _bufferPool = new ObjectPool<DynamicVertexBuffer>(() =>
            {
                var buffer = new DynamicVertexBuffer(device, SpriteVertex.VertexDeclaration, MAX_SPRITES * 4, BufferUsage.WriteOnly);
                _allBuffers.Add(buffer);
                return buffer;
            });
            _sprites = new Sprite[MAX_SPRITES];
            _renderCommands = new RenderCommandBuffer(allocator);
            _device = device;

            _defaultEffect = new BasicEffect(device);
            _defaultEffect.TextureEnabled = true;
            _defaultEffect.VertexColorEnabled = true;

            _defaultTexture = new Texture2D(device, 1, 1);
            _defaultTexture.SetData(new[] { Color.White });

            _indexBuffer = new IndexBuffer(device, IndexElementSize.SixteenBits, IndexData.Length, BufferUsage.None);
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

        private void UpdateProjection()
        {
            // set up our matrix to match basic effect.
            Viewport viewport = _device.Viewport;
            //
            var vp = _device.Viewport;
            if ((_lastViewport.Width != vp.Width) || (_lastViewport.Height != vp.Height))
            {
                Matrix _projection = Matrix.Identity;
                // Normal 3D cameras look into the -z direction (z = 1 is in front of z = 0). The
                // sprite batch layer depth is the opposite (z = 0 is in front of z = 1).
                // --> We get the correct matrix with near plane 0 and far plane -1.
                Matrix.CreateOrthographicOffCenter(0, vp.Width, vp.Height, 0, 0, -1, out _projection);

                // Some platforms require a half pixel offset to match DX.
                //if (NeedsHalfPixelOffset)
                {
                    _projection.M41 += -0.5f * _projection.M11;
                    _projection.M42 += -0.5f * _projection.M22;
                }

                _defaultEffect.World = Matrix.Identity;
                _defaultEffect.Projection = _projection;
                _defaultEffect.View = Matrix.Identity;

                _lastViewport = vp;
            }
        }

        public void SetTexture(Texture texture)
        {
            Assert.EqualTo(_isInBulkOperation, false);
            if (_currentTexture != texture)
            {
                FlushRender();
                _renderCommands.SetTexture(texture);
                _currentTexture = texture;
            }
        }

        public void SetBlendState(BlendState blendState)
        {
            Assert.EqualTo(_isInBulkOperation, false);
            FlushRender();
            _renderCommands.SetBlendState(blendState);
        }
        public void SetDepthState(DepthStencilState depthState)
        {
            Assert.EqualTo(_isInBulkOperation, false);
            FlushRender();
            _renderCommands.SetDepthState(depthState);
        }
        public void SetRasterizerState(RasterizerState rasitizerState)
        {
            Assert.EqualTo(_isInBulkOperation, false);
            FlushRender();
            _renderCommands.SetRasterizerState(rasitizerState);
        }
        public void SetSamplerState(SamplerState samplerState)
        {
            Assert.EqualTo(_isInBulkOperation, false);
            FlushRender();
            _renderCommands.SetSamplerState(samplerState);
        }
        public void SetEffect(Effect effect)
        {
            Assert.EqualTo(_isInBulkOperation, false);
            FlushRender();
            _renderCommands.SetEffect(effect);
        }

        public void AddSprite(Texture texture, in Position position, in Scale scale, in Color color, in TextureRegion texCoord)
        {
            Assert.EqualTo(_isInBulkOperation, false);
            SetTexture(texture);

            ref var sprite = ref _sprites[_spriteIndex++];

            sprite.TL.Position = position;
            sprite.TL.Color = color;
            sprite.TL.TextureCoord.X = texCoord.X0;
            sprite.TL.TextureCoord.Y = texCoord.Y0;

            sprite.TR.Position.X = position.X + scale.Width;
            sprite.TR.Position.Y = position.Y;
            sprite.TR.Color = color;
            sprite.TR.TextureCoord.X = texCoord.X1;
            sprite.TR.TextureCoord.Y = texCoord.Y0;

            sprite.BR.Position.X = position.X + scale.Width;
            sprite.BR.Position.Y = position.Y + scale.Height;
            sprite.BR.Color = color;
            sprite.BR.TextureCoord.X = texCoord.X1;
            sprite.BR.TextureCoord.Y = texCoord.Y1;

            sprite.BL.Position.X = position.X;
            sprite.BL.Position.Y = position.Y + scale.Height;
            sprite.BL.Color = color;
            sprite.BL.TextureCoord.X = texCoord.X0;
            sprite.BL.TextureCoord.Y = texCoord.Y1;

            _primitiveCount += 2;

            if (_spriteIndex == MAX_SPRITES)
                CompleteBuffer();
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
                _renderCommands.RenderOp(_spriteIndex * 4 - _primitiveCount * 2, _primitiveCount);
                _primitiveCount = 0;
            }
        }
        private void CompleteBuffer(bool lastRender = false)
        {
            FlushRender();


            _vertexBuffer.SetData(_sprites, 0, _spriteIndex, SetDataOptions.Discard);//, 0, _vertexPosition, SetDataOptions.Discard);
            _spriteIndex = 0;

            if (!lastRender)
                UpdateNextVertexBuffer();
        }

        public void Render(SpriteBatch batch)
        {
            Assert.EqualTo(_isInBulkOperation, false);
            UpdateProjection();
            CompleteBuffer(true);
            _renderCommands.Render(_device);
            Reset();
        }

        public void Reset()
        {
            //TODO: renderCommand should support updating effect params (like WVP)
            _renderCommands.SetDepthState(_defaultDepth);
            _renderCommands.SetBlendState(_defaultBlend);
            _renderCommands.SetRasterizerState(_defaultRasterizer);
            _renderCommands.SetSamplerState(_defaultSampler);
            _renderCommands.SetTexture(_defaultTexture);
            _renderCommands.SetEffect(_defaultEffect);
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
            _defaultEffect.Dispose();
        }
    }
}