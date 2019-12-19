namespace Game.Framework.Services.Graphics
{
    using System;
    using System.Collections.Generic;
    using Atma;

    public interface ITexture : IDisposable
    {
        uint ID { get; }
        void Bind();
    }

    public interface ITexture2D : ITexture
    {
        int Width { get; }
        int Height { get; }
        void SetData<T>(T[] data) where T : struct;
        void GetData<T>(T[] data) where T : struct;
    }

    [GameService()]
    public interface ITextureFactory : IDisposable
    {
        ITexture2D LoadFromFile(IReadOnlyFile file);
        ITexture2D CreateTexture(string name, int width, int height);
        ITexture2D this[string name] { get; }
        ITexture2D this[uint textureId] { get; }

    }

    public abstract class TextureFactoryBase : UnmanagedDispose, ITextureFactory
    {
        private Dictionary<uint, ITexture2D> _textures = new Dictionary<uint, ITexture2D>();

        public ITexture2D this[string name] => this[GetId(name)];

        public ITexture2D this[uint textureId]
        {
            get
            {
                //TODO: create default texture with an init method
                if (!_textures.TryGetValue(textureId, out var texture))
                    return OnePixel;

                return texture;
            }
        }

        public abstract ITexture2D OnePixel { get; }

        public ITexture2D LoadFromFile(IReadOnlyFile file)
        {
            var id = GetId(file.Name);
            var texture = PlatformLoadFromFile(id, file);
            if (_textures.ContainsKey(id))
                _textures[id] = texture;
            else
                _textures.Add(id, texture);
            return texture;
        }

        public ITexture2D CreateTexture(string name, int width, int height)
        {
            var id = GetId(name);
            var texture = PlatformCreateTexture(id, width, height);
            if (_textures.ContainsKey(id))
                _textures[id] = texture;
            else
                _textures.Add(id, texture);
            return texture;
        }

        protected uint GetId(string name) => (uint)name.GetHashCode();

        protected abstract ITexture2D PlatformCreateTexture(uint id, int width, int height);

        protected abstract ITexture2D PlatformLoadFromFile(uint id, IReadOnlyFile file);

        protected override void OnManagedDispose()
        {
            foreach (var it in _textures.Values)
                it.Dispose();

            _textures.Clear();
        }
    }
}