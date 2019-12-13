namespace Game.Engine.Managers
{
    using System.IO;
    using Atma;
    using Game.Framework.Managers;
    using Microsoft.Extensions.Logging;
    using Microsoft.Xna.Framework.Graphics;

    public class TextureManager : TextureManagerBase
    {
        protected class Texture2DInternal : UnmanagedDispose, ITexture2D
        {
            private Texture2D _texture;

            public Texture2DInternal(uint id, Texture2D texture)
            {
                ID = id;
                _texture = texture;
            }

            public uint ID { get; private set; }

            public int Width => _texture.Width;

            public int Height => _texture.Height;

            public void GetData<T>(T[] data) where T : struct => _texture.GetData<T>(data);

            public void SetData<T>(T[] data) where T : struct => _texture.SetData<T>(data);

            protected override void OnManagedDispose()
            {
                _texture?.Dispose();
                _texture = null;
            }
        }

        private GraphicsDevice _device;

        public TextureManager(GraphicsDevice device)
        {
            _device = device;
        }

        protected override ITexture2D CreateTextureInternal(uint id, int width, int height)
        {
            var texture = new Texture2D(_device, width, height);
            return new Texture2DInternal(id, texture);
        }

        protected override ITexture2D LoadFromFileInternal(uint id, string loadFile)
        {
            using (var fs = File.OpenRead(loadFile))
            {
                var texture = Texture2D.FromStream(_device, fs);
                return new Texture2DInternal(id, texture);
            }
        }
    }
}