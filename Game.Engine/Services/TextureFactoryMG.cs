namespace Game.Engine.Services
{
    using System.IO;
    using Atma;
    using Game.Framework;
    using Game.Framework.Services.Graphics;
    using Microsoft.Xna.Framework.Graphics;

    public class TextureFactoryMG : TextureFactoryBase, IGameService
    {
        protected class PlatformTexture2D : UnmanagedDispose, ITexture2D
        {
            private GraphicsDevice _device;
            private Texture2D _texture;

            public PlatformTexture2D(uint id, GraphicsDevice device, Texture2D texture)
            {
                ID = id;
                _device = device;
                _texture = texture;
            }

            public uint ID { get; private set; }

            public int Width => _texture.Width;

            public int Height => _texture.Height;

            public void GetData<T>(T[] data) where T : struct => _texture.GetData<T>(data);

            public void SetData<T>(T[] data) where T : struct => _texture.SetData<T>(data);

            public void Bind()
            {
                _device.Textures[0] = _texture;
            }

            protected override void OnManagedDispose()
            {
                _texture?.Dispose();
                _texture = null;
            }
        }

        private GraphicsDevice _device;

        public TextureFactoryMG(GraphicsDevice device)
        {
            _device = device;
        }

        protected override ITexture2D PlatformCreateTexture(uint id, int width, int height)
        {
            var texture = new Texture2D(_device, width, height);
            return new PlatformTexture2D(id, _device, texture);
        }

        protected override ITexture2D PlatformLoadFromFile(uint id, string loadFile)
        {
            using (var fs = File.OpenRead(loadFile))
            {
                var texture = Texture2D.FromStream(_device, fs);
                return new PlatformTexture2D(id, _device, texture);
            }
        }

        public void Initialize()
        {
            var texture = CreateTexture("default", 1, 1);
            texture.SetData(new Color[1] { new Color(Microsoft.Xna.Framework.Color.Magenta.PackedValue) });
        }

        public void Tick(float dt)
        {
        }
    }
}