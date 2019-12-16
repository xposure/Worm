namespace Game.Framework.Managers
{
    using System;
    using Atma;

    public interface IGraphicsBuffer : IDisposable
    {
        uint ID { get; }
        void Bind();
    }

    public interface IVertexBuffer : IGraphicsBuffer
    {

    }

    public interface IVertexBuffer<T> : IVertexBuffer
        where T : unmanaged
    {
        void SetData(T[] data) => SetData(data, 0, data.Length);
        void SetData(T[] data, int startIndex, int length);

    }

    public interface IIndexBuffer : IGraphicsBuffer
    {
    }

    public interface IIndexBuffer16 : IIndexBuffer
    {
        void SetData(ushort[] data) => SetData(data, 0, data.Length);
        void SetData(ushort[] data, int startIndex, int length);
    }

    public interface IIndexBuffer32 : IIndexBuffer
    {
        void SetData(uint[] data) => SetData(data, 0, data.Length);
        void SetData(uint[] data, int startIndex, int length);
    }

    [GameService()]
    public interface IGraphicsBufferFactory : IDisposable
    {
        IVertexBuffer<T> CreateVertex<T>(int count, bool isDynamic = false) where T : unmanaged;
        IIndexBuffer16 CreateIndex16(int count, bool isDynamic = false);
        IIndexBuffer32 CreateIndex32(int count, bool isDynamic = false);
    }

    public abstract class GraphicsBufferFactoryBase : UnmanagedDispose, IGraphicsBufferFactory
    {
        private uint _nextId;

        public IVertexBuffer<T> CreateVertex<T>(int count, bool isDynamic = false)
            where T : unmanaged
        {
            return CreateVertexPlatform<T>(++_nextId, count, isDynamic);
        }

        protected abstract IVertexBuffer<T> CreateVertexPlatform<T>(uint id, int count, bool isDynamic)
            where T : unmanaged;

        public IIndexBuffer16 CreateIndex16(int count, bool isDynamic = false)
        {
            return CreateIndex16Platform(++_nextId, count, isDynamic);
        }
        protected abstract IIndexBuffer16 CreateIndex16Platform(uint id, int count, bool isDynamic);

        public IIndexBuffer32 CreateIndex32(int count, bool isDynamic = false)
        {
            return CreateIndex32Platform(++_nextId, count, isDynamic);
        }

        protected abstract IIndexBuffer32 CreateIndex32Platform(uint id, int count, bool isDynamic);
    }
}