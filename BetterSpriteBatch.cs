using System;
using Atma.Memory;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Worm
{
    public class BetterSpriteBatch
    {
        public struct VertexElement
        {
            public Vector2 Position;
            public Color Color;
            public Vector2 TextureCoord;

            public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
                new Microsoft.Xna.Framework.Graphics.VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                new Microsoft.Xna.Framework.Graphics.VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new Microsoft.Xna.Framework.Graphics.VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
            );
        }

        private DynamicVertexBuffer _vertexBuffer;
        private DynamicIndexBuffer _indexBuffer;

        private RenderCommandBuffer _renderCommands;
        private BasicEffect _basicEffect;
        private GraphicsDevice _device;
        private Texture2D _defaultTexture;
        private DepthStencilState _defaultDepth = DepthStencilState.Default;
        private BlendState _defaultBlend = BlendState.NonPremultiplied;
        private RasterizerState _defaultRasterizer = RasterizerState.CullNone;

        private BasicEffect Effect => _basicEffect;

        public BetterSpriteBatch(IAllocator allocator, GraphicsDevice device)
        {
            _renderCommands = new RenderCommandBuffer(allocator);
            _device = device;

            _basicEffect = new BasicEffect(device);
            _basicEffect.TextureEnabled = true;
            _basicEffect.VertexColorEnabled = true;

            // set up our matrix to match basic effect.
            Viewport viewport = device.Viewport;
            //

            var camera2DScrollPosition = new Vector3(0, 0, -1);
            var camera2DScrollLookAt = new Vector3(0, 0, 0);
            float camera2dRotationZ = 0f;
            _basicEffect.World = Matrix.Identity;
            Vector3 cameraUp = Vector3.Transform(new Vector3(0, -1, 0), Matrix.CreateRotationZ(camera2dRotationZ));
            _basicEffect.View = Matrix.CreateLookAt(camera2DScrollPosition, camera2DScrollLookAt, cameraUp);
            // We could set up the world maxtrix this way and get the expected rotation but its not really proper.
            //basicEffect.World = Matrix.Identity * Matrix.CreateRotationZ(camera2DrotationZ);
            //basicEffect.View = Matrix.CreateLookAt(camera2DScrollPosition, camera2DScrollLookAt, new Vector3(0, -1, 0));
            _basicEffect.Projection = Matrix.CreateScale(1, -1, 1) * Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, 0, 1);

            _defaultTexture = new Texture2D(device, 1, 1);
            _defaultTexture.SetData(new[] { Color.White });

            _vertexBuffer = new DynamicVertexBuffer(device, VertexElement.VertexDeclaration, 4096, BufferUsage.WriteOnly);
            _vertexBuffer.SetData(new VertexElement[] {
                new VertexElement() { Position = new Vector2(-100, -100), TextureCoord = new Vector2(0, 0), Color = Color.White},
                new VertexElement() { Position = new Vector2(100, 100), TextureCoord = new Vector2(1, 0), Color = Color.Red},
                new VertexElement() { Position = new Vector2(0, 0), TextureCoord = new Vector2(0, 1), Color = Color.Green}
            });
            _indexBuffer = new DynamicIndexBuffer(device, IndexElementSize.SixteenBits, 4096, BufferUsage.WriteOnly);
            _indexBuffer.SetData(new short[] { 0, 1, 2 });
        }

        public void Render()
        {
            _renderCommands.SetDepthState(_defaultDepth);
            _renderCommands.SetBlendState(_defaultBlend);
            _renderCommands.SetRasterizerState(_defaultRasterizer);
            _renderCommands.SetTexture(_defaultTexture);
            _renderCommands.SetEffect(_basicEffect);
            _renderCommands.SetIndexBuffer(_indexBuffer);
            _renderCommands.SetVertexBuffer(_vertexBuffer);
            _renderCommands.RenderOp(0, 1);
            _renderCommands.Render(_device);
        }



    }
}