namespace Game.Engine.Services
{
    using System.Collections.Concurrent;
    using System.IO;
    using Atma;
    using Game.Framework;
    using Game.Framework.Services;
    using Game.Framework.Services.Graphics;
    using Microsoft.Extensions.Logging;
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

        private ILogger _logger;
        private GraphicsDevice _device;
        private IFileSystem _fileSystem;

        private ITexture2D _onePixel;

        public override ITexture2D OnePixel => _onePixel;

        public TextureFactoryMG(ILoggerFactory logFactory, GraphicsDevice device, IFileSystem fileSystem)
        {
            _logger = logFactory.CreateLogger<TextureFactoryMG>();
            _device = device;
            _fileSystem = fileSystem;
        }

        protected override ITexture2D PlatformCreateTexture(uint id, int width, int height)
        {
            var texture = new Texture2D(_device, width, height);
            return new PlatformTexture2D(id, _device, texture);
        }

        protected override ITexture2D PlatformLoadFromFile(uint id, IReadOnlyFile file)
        {
            _logger.LogDebug($"Loading texture {id} [{file}]");
            using (var fs = file.OpenRead())
            {
                var texture = Texture2D.FromStream(_device, fs);
                return new PlatformTexture2D(id, _device, texture);
            }
        }

        private ConcurrentBag<IReadOnlyFile> _modifiedFiles = new ConcurrentBag<IReadOnlyFile>();

        public void Initialize()
        {
            _onePixel = new PlatformTexture2D(0, _device, new Texture2D(_device, 1, 1));
            _onePixel.SetData(new Color[1] { new Color(Microsoft.Xna.Framework.Color.Magenta.PackedValue) });

            var files = _fileSystem.FindAssets("textures/**/*", FileType.IMAGE);
            foreach (var it in files)
                LoadFromFile(it);

            Track(files.Observe(file => _modifiedFiles.Add(file)));
        }

        public void Tick(float dt)
        {
            while (_modifiedFiles.TryTake(out var file))
                LoadFromFile(file);
        }
    }
}