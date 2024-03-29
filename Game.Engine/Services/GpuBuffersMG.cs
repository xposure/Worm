namespace Game.Engine.Services
{
    using System;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Atma;
    using Game.Framework;
    using Game.Framework.Services.Graphics;
    using Microsoft.Xna.Framework.Graphics;

    public class GraphicsBufferFactoryMG : GraphicsBufferFactoryBase
    {
        public class PlatformIndexBuffer16 : UnmanagedDispose, IIndexBuffer16
        {
            private readonly GraphicsDevice _device;
            private IndexBuffer _buffer;
            private bool _isDynamic;

            public uint ID { get; private set; }

            public PlatformIndexBuffer16(uint id, int size, GraphicsDevice device, bool isDynamic)
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
                _buffer?.Dispose();
                _buffer = null;
            }
        }

        public class PlatformIndexBuffer32 : UnmanagedDispose, IIndexBuffer32
        {
            private readonly GraphicsDevice _device;
            private IndexBuffer _buffer;
            private bool _isDynamic;

            public uint ID { get; private set; }

            public PlatformIndexBuffer32(uint id, int size, GraphicsDevice device, bool isDynamic)
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
                _buffer?.Dispose();
                _buffer = null;
            }
        }

        public class PlatformVertexBuffer<T> : UnmanagedDispose, IVertexBuffer<T>
            where T : unmanaged
        {
            private static bool IsValid => VertexDeclaration != null;
            private static readonly Exception Exception;

            public static readonly VertexDeclaration VertexDeclaration;
            public static readonly Type VertexType;

            static PlatformVertexBuffer()
            {
                try
                {
                    var type = typeof(T);
                    var vertexGroup = type.GetCustomAttribute<VertexGroupAttribute>();
                    if (vertexGroup != null)
                        type = vertexGroup.VertexType;

                    VertexType = type;

                    var fields = type.GetFields();
                    var elements = new VertexElement[fields.Length];
                    for (var i = 0; i < elements.Length; i++)
                    {
                        var vertAttr = fields[i].GetCustomAttribute<VertexElementAttribute>();
                        if (vertAttr == null)
                            throw new Exception($"Missing VertexElementAttribute on {fields[i].Name}");

                        var offset = (int)Marshal.OffsetOf(type, fields[i].Name);
                        elements[i] = Convert(offset, vertAttr.VertexSchema);
                    }

                    VertexDeclaration = new VertexDeclaration(elements);
                }
                catch (Exception ex)
                {
                    Exception = ex;
                }
            }

            private static VertexElement Convert(int offset, in VertexSchema schema)
               => new VertexElement(
                        offset, //(int)Marshal.OffsetOf(type, fields[i].Name),
                        Convert(schema.ElementType),
                        Convert(schema.Semantic),
                        schema.UsageIndex);

            private static VertexElementFormat Convert(VertexElementType elementType)
            {
                switch (elementType)
                {
                    case VertexElementType.Color: return VertexElementFormat.Color;
                    case VertexElementType.Float2: return VertexElementFormat.Vector2;
                    case VertexElementType.Float3: return VertexElementFormat.Vector3;
                    default:
                        throw new Exception($"Unsupported element type: {elementType}");
                }
            }

            private static VertexElementUsage Convert(VertexSemantic semantic)
            {
                switch (semantic)
                {
                    case VertexSemantic.Color: return VertexElementUsage.Color;
                    case VertexSemantic.Texture: return VertexElementUsage.TextureCoordinate;
                    case VertexSemantic.Position: return VertexElementUsage.Position;
                    default:
                        throw new Exception($"Unsupported semantic: {semantic}");
                }
            }

            private readonly GraphicsDevice _device;
            private VertexBuffer _buffer;
            private bool _isDynamic = false;

            public uint ID { get; private set; }

            public PlatformVertexBuffer(uint id, int size, GraphicsDevice device, bool isDynamic)
            {
                if (!IsValid)
                    throw Exception;

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
                _buffer?.Dispose();
                _buffer = null;
            }
        }

        private readonly GraphicsDevice _device;

        public GraphicsBufferFactoryMG(GraphicsDevice device)
        {
            _device = device;
        }

        protected override IIndexBuffer16 PlatformCreateIndex16(uint id, int count, bool isDynamic)
            => new PlatformIndexBuffer16(id, count, _device, isDynamic);

        protected override IIndexBuffer32 PlatformCreateIndex32(uint id, int count, bool isDynamic)
            => new PlatformIndexBuffer32(id, count, _device, isDynamic);

        protected override IVertexBuffer<T> PlatformCreateVertex<T>(uint id, int count, bool isDynamic)
            => new PlatformVertexBuffer<T>(id, count, _device, isDynamic);
    }
}