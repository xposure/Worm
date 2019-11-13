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
    public class BetterSpriteBatch : UnmanagedDispose
    {
        private const int MAX_PRIMITIVES = 10922;
        private const int MAX_VERTICES = MAX_PRIMITIVES * 4;
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

        private RenderCommandBuffer _renderCommands;
        private BasicEffect _defaultEffect;
        private GraphicsDevice _device;
        private Texture2D _defaultTexture;
        private DepthStencilState _defaultDepth = DepthStencilState.Default;
        private BlendState _defaultBlend = BlendState.NonPremultiplied;
        private RasterizerState _defaultRasterizer = RasterizerState.CullNone;

        private Effect Effect => _defaultEffect;

        private Texture _currentTexture = null;

        private IndexBuffer _indexBuffer;

        private int _primitiveCount = 0;
        private int _vertexPosition = 0;
        private SpriteVertex[] _vertices;
        private DynamicVertexBuffer _vertexBuffer;
        private ObjectPool<DynamicVertexBuffer> _bufferPool;
        private List<DynamicVertexBuffer> _usedBuffers0 = new List<DynamicVertexBuffer>();
        private List<DynamicVertexBuffer> _usedBuffers1 = new List<DynamicVertexBuffer>();
        private List<VertexBuffer> _allBuffers = new List<VertexBuffer>();

        private Viewport _lastViewport = new Viewport();

        public BetterSpriteBatch(IAllocator allocator, GraphicsDevice device)
        {
            _bufferPool = new ObjectPool<DynamicVertexBuffer>(() =>
            {
                var buffer = new DynamicVertexBuffer(device, SpriteVertex.VertexDeclaration, MAX_VERTICES, BufferUsage.WriteOnly);
                _allBuffers.Add(buffer);
                return buffer;
            });
            _vertices = new SpriteVertex[MAX_VERTICES];
            _renderCommands = new RenderCommandBuffer(allocator);
            _device = device;

            _defaultEffect = new BasicEffect(device);
            _defaultEffect.TextureEnabled = true;
            _defaultEffect.VertexColorEnabled = true;

            _defaultTexture = new Texture2D(device, 1, 1);
            _defaultTexture.SetData(new[] { Color.White });

            _indexBuffer = new IndexBuffer(device, IndexElementSize.SixteenBits, IndexData.Length, BufferUsage.None);
            _indexBuffer.SetData(IndexData);

            for (var i = 0; i < MAX_VERTICES;)
            {
                _vertices[i++].TextureCoord = new TextureCoord(0, 0);
                _vertices[i++].TextureCoord = new TextureCoord(1, 0);
                _vertices[i++].TextureCoord = new TextureCoord(1, 1);
                _vertices[i++].TextureCoord = new TextureCoord(0, 1);
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

        public void AddSprite(Texture texture, in Position position, in Scale scale, in Color color)
        {
            if (_currentTexture != texture)
            {
                FlushRender();
                _renderCommands.SetTexture(texture);
                _currentTexture = texture;
            }

            ref var tl = ref _vertices[_vertexPosition++];
            ref var tr = ref _vertices[_vertexPosition++];
            ref var bl = ref _vertices[_vertexPosition++];
            ref var br = ref _vertices[_vertexPosition++];

            tl.Position = position;
            tl.Color = color;

            tr.Position.X = position.X + scale.Width;
            tr.Position.Y = position.Y;
            tr.Color = color;

            br.Position.X = position.X + scale.Width;
            br.Position.Y = position.Y + scale.Height;
            br.Color = color;

            bl.Position.X = position.X;
            bl.Position.Y = position.Y + scale.Height;
            bl.Color = color;

            _primitiveCount += 2;

            if (_vertexPosition == MAX_VERTICES)
                CompleteBuffer();
        }

        private void FlushRender()
        {
            if (_primitiveCount > 0)
            {
                _renderCommands.RenderOp(_vertexPosition - _primitiveCount * 2, _primitiveCount);
                _primitiveCount = 0;
            }
        }
        private void CompleteBuffer(bool lastRender = false)
        {
            FlushRender();

            _vertexBuffer.SetData(_vertices);//, 0, _vertexPosition, SetDataOptions.Discard);
            _vertexPosition = 0;

            if (!lastRender)
                UpdateNextVertexBuffer();
        }

        public void Render(SpriteBatch batch)
        {
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
            _vertexPosition = 0;
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