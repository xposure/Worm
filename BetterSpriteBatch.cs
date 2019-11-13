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
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
            );
        }
        public struct SVertex
        {
            public Vector3 Position;
            public Color Color;
            public Vector2 TextureCoord;

        }

        private RenderCommandBuffer _renderCommands;
        private SpriteEffect _defaultEffect;
        //private BasicEffect _defaultEffect;
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
        private SVertex[] _vertices;
        private DynamicVertexBuffer _vertexBuffer;
        private ObjectPool<DynamicVertexBuffer> _bufferPool;
        private List<DynamicVertexBuffer> _usedBuffers = new List<DynamicVertexBuffer>();
        private List<VertexBuffer> _allBuffers = new List<VertexBuffer>();

        public BetterSpriteBatch(IAllocator allocator, GraphicsDevice device)
        {
            _bufferPool = new ObjectPool<DynamicVertexBuffer>(() =>
            {
                var buffer = new DynamicVertexBuffer(device, VertexPositionColorTexture.VertexDeclaration, MAX_VERTICES, BufferUsage.WriteOnly);
                _allBuffers.Add(buffer);
                return buffer;
            });
            _vertices = new SVertex[MAX_VERTICES];
            //_vertices = new SpriteVertex[MAX_VERTICES];
            _renderCommands = new RenderCommandBuffer(allocator);
            _device = device;

            _defaultEffect = new SpriteEffect(device);
            //_defaultEffect.TextureEnabled = true;
            //_defaultEffect.VertexColorEnabled = true;

            // set up our matrix to match basic effect.
            Viewport viewport = device.Viewport;
            //
            Matrix _projection = Matrix.Identity;
            var vp = _device.Viewport;
            if ((vp.Width != vp.Width) || (vp.Height != vp.Height))
            {
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

                //vp = vp;
            }

            var camera2DScrollPosition = new Vector3(0, 0, -1);
            var camera2DScrollLookAt = new Vector3(0, 0, 0);
            float camera2dRotationZ = 0f;
            // _defaultEffect.World = Matrix.Identity;
            // Vector3 cameraUp = Vector3.Transform(new Vector3(0, -1, 0), Matrix.CreateRotationZ(camera2dRotationZ));
            // _defaultEffect.View = Matrix.Identity;// Matrix.CreateLookAt(camera2DScrollPosition, camera2DScrollLookAt, cameraUp);
            // // We could set up the world maxtrix this way and get the expected rotation but its not really proper.
            // //basicEffect.World = Matrix.Identity * Matrix.CreateRotationZ(camera2DrotationZ);
            // //basicEffect.View = Matrix.CreateLookAt(camera2DScrollPosition, camera2DScrollLookAt, new Vector3(0, -1, 0));
            // _defaultEffect.Projection = _projection;// Matrix.CreateScale(1, -1, 1) * Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, 0, 1);
            _defaultEffect.Parameters["MatrixTransform"].SetValue(_projection);
            _defaultTexture = new Texture2D(device, 1, 1);
            _defaultTexture.SetData(new[] { Color.White });

            // _vertexBuffer = new DynamicVertexBuffer(device, VertexElement.VertexDeclaration, 4096, BufferUsage.WriteOnly);

            _indexBuffer = new IndexBuffer(device, IndexElementSize.SixteenBits, IndexData.Length, BufferUsage.None);

            _indexBuffer.SetData(IndexData);

            for (var i = 0; i < MAX_VERTICES;)
            {
                _vertices[i++].TextureCoord = new Vector2(0, 0);
                _vertices[i++].TextureCoord = new Vector2(1, 0);
                _vertices[i++].TextureCoord = new Vector2(1, 1);
                _vertices[i++].TextureCoord = new Vector2(0, 1);
            }
            Reset();
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

            tl.Position = new Vector3(position.X, position.Y, 0);
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
            {
                _vertexBuffer = _bufferPool.Take();
                _renderCommands.SetVertexBuffer(_vertexBuffer);
                _usedBuffers.Add(_vertexBuffer);
            }
        }

        public void Render(SpriteBatch batch)
        {
            CompleteBuffer(true);

            batch.Begin(SpriteSortMode.Immediate);

            _renderCommands.Render(_device);
            batch.End();
            Reset();
        }

        public void Reset()
        {
            _renderCommands.SetDepthState(_defaultDepth);
            _renderCommands.SetBlendState(_defaultBlend);
            _renderCommands.SetRasterizerState(_defaultRasterizer);
            _renderCommands.SetTexture(_defaultTexture);
            //_renderCommands.SetEffect(_defaultEffect);
            _renderCommands.SetIndexBuffer(_indexBuffer);
            _currentTexture = _defaultTexture;
            foreach (var it in _usedBuffers)
                _bufferPool.Return(it);

            _vertexBuffer = _bufferPool.Take();
            _usedBuffers.Add(_vertexBuffer);
            _renderCommands.SetVertexBuffer(_vertexBuffer);
            _vertexPosition = 0;
            _primitiveCount = 0;
        }

        protected override void OnUnmanagedDispose()
        {
            _allBuffers.DisposeAll();
            _defaultTexture.Dispose();
            _renderCommands.Dispose();
            _indexBuffer.Dispose();
            _defaultEffect.Dispose();
        }
    }
}