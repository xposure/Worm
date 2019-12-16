namespace Game.Framework.Managers
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
    public interface ITextureManager : IDisposable
    {
        ITexture2D LoadFromFile(string name, string loadFile);
        ITexture2D CreateTexture(string name, int width, int height);
        ITexture2D this[string name] { get; }
        ITexture2D this[uint textureId] { get; }

    }

    public abstract class TextureManagerBase : UnmanagedDispose, ITextureManager
    {
        private Dictionary<uint, ITexture2D> _textures = new Dictionary<uint, ITexture2D>();

        public ITexture2D this[string name] => this[GetId(name)];

        public ITexture2D this[uint textureId]
        {
            get
            {
                //TODO: create default texture with an init method
                if (!_textures.TryGetValue(textureId, out var texture))
                    return _textures[GetId("default")];

                return texture;
            }
        }

        public TextureManagerBase()
        {

        }

        public ITexture2D LoadFromFile(string name, string loadFile)
        {
            var id = GetId(name);
            var texture = PlatformLoadFromFile(id, loadFile);
            _textures.Add(id, texture);
            return texture;
        }

        public ITexture2D CreateTexture(string name, int width, int height)
        {
            var id = GetId(name);
            var texture = PlatformCreateTexture(id, width, height);
            _textures.Add(id, texture);
            return texture;
        }

        protected uint GetId(string name) => (uint)name.GetHashCode();

        protected abstract ITexture2D PlatformCreateTexture(uint id, int width, int height);

        protected abstract ITexture2D PlatformLoadFromFile(uint id, string loadFile);

        protected override void OnManagedDispose()
        {
            foreach (var it in _textures.Values)
                it.Dispose();

            _textures.Clear();
        }
    }
}