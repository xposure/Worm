namespace Game.Engine.Managers
{
    using Atma;
    using Game.Framework.Managers;
    using Microsoft.Xna.Framework.Graphics;

    public class GraphicsBufferFactory : GraphicsBufferFactoryBase
    {
        public class IndexBuffer16Internal : UnmanagedDispose, IIndexBuffer16
        {
            private readonly GraphicsDevice _device;
            private IndexBuffer _buffer;
            private bool _isDynamic;

            public uint ID { get; private set; }

            public IndexBuffer16Internal(uint id, int size, GraphicsDevice device, bool isDynamic)
            {
                ID = id;
                _device = device;
                _isDynamic = isDynamic;


                if (isDynamic)
                    _buffer = new DynamicIndexBuffer(_device, IndexElementSize.SixteenBits, size, BufferUsage.WriteOnly);
                else
                    _buffer = new IndexBuffer(_device, IndexElementSize.SixteenBits, size, BufferUsage.None);
            }

            public void Bind()
            {
                _device.Indices = _buffer;
            }

            public void SetData(ushort[] data, int startIndex, int length)
            {
                if (_isDynamic)
                    ((DynamicIndexBuffer)_buffer).SetData(data, startIndex, length, SetDataOptions.Discard);
                else
                    _buffer.SetData(data, startIndex, length);
            }

            protected override void OnManagedDispose()
            {
                _buffer.Dispose();
            }
        }

        public class IndexBuffer32Internal : UnmanagedDispose, IIndexBuffer32
        {
            private readonly GraphicsDevice _device;
            private IndexBuffer _buffer;
            private bool _isDynamic;

            public uint ID { get; private set; }

            public IndexBuffer32Internal(uint id, int size, GraphicsDevice device, bool isDynamic)
            {
                ID = id;
                _device = device;
                _isDynamic = isDynamic;

                if (isDynamic)
                    _buffer = new DynamicIndexBuffer(_device, IndexElementSize.ThirtyTwoBits, size, BufferUsage.WriteOnly);
                else
                    _buffer = new IndexBuffer(_device, IndexElementSize.ThirtyTwoBits, size, BufferUsage.None);
            }

            public void Bind()
            {
                _device.Indices = _buffer;
            }

            public void SetData(uint[] data, int startIndex, int length)
            {
                if (_isDynamic)
                    ((DynamicIndexBuffer)_buffer).SetData(data, startIndex, length, SetDataOptions.Discard);
                else
                    _buffer.SetData(data, startIndex, length);
            }

            protected override void OnManagedDispose()
            {
                _buffer.Dispose();
            }
        }

        public class VertexBufferInternal<T> : UnmanagedDispose, IVertexBuffer<T>
            where T : unmanaged
        {
            public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
            );

            private readonly GraphicsDevice _device;
            private VertexBuffer _buffer;
            private bool _isDynamic = false;

            public uint ID { get; private set; }

            public VertexBufferInternal(uint id, int size, GraphicsDevice device, bool isDynamic)
            {
                ID = id;
                _device = device;
                _isDynamic = isDynamic;

                if (isDynamic)
                    _buffer = new DynamicVertexBuffer(_device, VertexDeclaration, size, BufferUsage.None);
                else
                    _buffer = new VertexBuffer(_device, VertexDeclaration, size, BufferUsage.WriteOnly);
            }

            public void Bind()
            {
                _device.SetVertexBuffer(_buffer);
            }

            public void SetData(T[] data, int startIndex, int length)
            {
                if (_isDynamic)
                    ((DynamicVertexBuffer)_buffer).SetData(data, startIndex, length, SetDataOptions.Discard);
                else
                    _buffer.SetData(data, startIndex, length);
            }

            protected override void OnManagedDispose()
            {
                _buffer.Dispose();
            }
        }

        private readonly GraphicsDevice _device;

        public GraphicsBufferFactory(GraphicsDevice device)
        {
            _device = device;
        }

        protected override IIndexBuffer16 CreateIndex16Internal(uint id, int count, bool isDynamic)
            => new IndexBuffer16Internal(id, count, _device, isDynamic);

        protected override IIndexBuffer32 CreateIndex32Internal(uint id, int count, bool isDynamic)
            => new IndexBuffer32Internal(id, count, _device, isDynamic);

        protected override IVertexBuffer<T> CreateVertexInternal<T>(uint id, int count, bool isDynamic)
            => new VertexBufferInternal<T>(id, count, _device, isDynamic);
    }
}