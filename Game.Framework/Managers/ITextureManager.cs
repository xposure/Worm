namespace Game.Framework.Managers
{
    using System;
    using System.Collections.Generic;
    using Atma;

    public interface ITexture2D : IDisposable
    {
        uint ID { get; }
        int Width { get; }
        int Height { get; }
        void SetData<T>(T[] data) where T : struct;
        void GetData<T>(T[] data) where T : struct;
    }

    [AutoRegister(true)]
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

        public ITexture2D this[string name] => throw new NotImplementedException();

        public ITexture2D this[uint textureId] => throw new NotImplementedException();

        public ITexture2D LoadFromFile(string name, string loadFile)
        {
            var id = GetId(name);
            var texture = LoadFromFileInternal(id, loadFile);
            _textures.Add(id, texture);
            return texture;
        }

        public ITexture2D CreateTexture(string name, int width, int height)
        {
            var id = GetId(name);
            var texture = CreateTextureInternal(id, width, height);
            _textures.Add(id, texture);
            return texture;
        }

        protected uint GetId(string name) => (uint)name.GetHashCode();

        protected abstract ITexture2D CreateTextureInternal(uint id, int width, int height);

        protected abstract ITexture2D LoadFromFileInternal(uint id, string loadFile);

        protected override void OnManagedDispose()
        {
            foreach (var it in _textures.Values)
                it.Dispose();

            _textures.Clear();
        }
    }
}