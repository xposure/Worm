namespace Worm
{
    //https://mikemac2d.itch.io/adventure-tile-pack
    //https://sharpscript.net/lisp/unity
    //https://0x72.itch.io/pixeldudesmaker

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;

    using Atma;
    using Atma.Memory;
    using Atma.Entities;
    using Atma.Math;

    using System;
    using System.IO;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using System.Threading;
    using Microsoft.Extensions.Logging;

    using SimpleInjector;

    using Game.Framework;

    public class Engine : Game
    {
        public const int TILE_SIZE = 4;

        private ManualResetEvent _requestReload = new ManualResetEvent(true);

        private readonly ILoggerFactory _logFactory;
        private ILogger _logger;
        private GameServiceManager _services;

        private Container _serviceContainer = new Container();
        private GameExecutionEngine _gee;
        private GraphicsDeviceManager graphics;

        private Stopwatch updateTimer = new Stopwatch();
        private Stopwatch renderTimer = new Stopwatch();

        private AvgValue _updateAvg = new AvgValue(0.9f, 1f);
        private AvgValue _renderAvg = new AvgValue(0.9f, 1f);

        private Task<GameExecutionEngine> _geeReloadTask;

        private bool _isRunning = true;

        public Engine()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _logFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddConsole(configure => configure.DisableColors = false);
            });

            _logger = _logFactory.CreateLogger<Engine>();
        }

        private GameExecutionEngine CheckGEE()
        {
            System.Threading.Thread.CurrentThread.Name = "Logic Hot Reload";
            while (_isRunning)
            {
                if (_gee == null || _gee.CheckChange())
                    _requestReload.Set();

                System.Threading.Thread.Sleep(10);
            }
            return null;
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            _isRunning = false;
        }
        private Container CreateDI()
        {
            var container = new Container();

            //singletons
            container.RegisterInstance(this);
            container.RegisterInstance(GraphicsDevice);
            container.RegisterInstance(graphics);
            container.RegisterInstance<IAllocator>(_serviceContainer.GetInstance<IAllocator>());
            container.RegisterInstance<EntityManager>(_serviceContainer.GetInstance<EntityManager>());

            //logging
            container.RegisterInstance<ILoggerFactory>(_logFactory);
            container.Register(typeof(ILogger<>), typeof(Logger<>));

            _services.RegisterPlatformInstances(_serviceContainer, container);

            return container;
        }

        protected override void Initialize()
        {
            _logger.LogInformation("Init");

            _serviceContainer.RegisterInstance(this);
            _serviceContainer.RegisterInstance(GraphicsDevice);
            _serviceContainer.RegisterInstance(graphics);

            _serviceContainer.RegisterInstance<ILoggerFactory>(_logFactory);
            _serviceContainer.Register(typeof(ILogger<>), typeof(Logger<>));
            _serviceContainer.RegisterSingleton(typeof(IAllocator), typeof(HeapAllocator));
            _serviceContainer.RegisterSingleton(typeof(EntityManager), typeof(EntityManager));

            _services = new GameServiceManager(typeof(Engine).Assembly, _serviceContainer, _logFactory);

            _serviceContainer.Verify();

            _services.Initialize();

            _geeReloadTask = Task.Run(() => CheckGEE());
            base.Initialize();
        }

        private void Reload()
        {
            if (_requestReload.WaitOne(0))
            {
                _logger.LogDebug("Reloading logic modules.");

                _gee?.Dispose();
                GC.GetTotalMemory(true);

                var container = CreateDI();
                _gee = new GameExecutionEngine(container, "Game.Logic\\bin\\Game.Logic.dll");
                _gee.Init();

                _requestReload.Reset();
            }
        }

        protected override void Dispose(bool disposing)
        {
            _gee.Dispose();
            _serviceContainer.Dispose();

            base.Dispose(disposing);
        }

        protected unsafe override void LoadContent()
        {
            base.LoadContent();

            CreateWalls();
            CreateCamera(0);
        }

        private void CreateWalls()
        {
            using (var sr = File.OpenText(@"Assets\room.data"))
            {
                var wallSpec = EntitySpec.Create<Position, Sprite, Color, Solid, Scale>();
                var em = _serviceContainer.GetInstance<EntityManager>();

                var wall2 = em.Create(wallSpec);
                em.Replace(wall2, new Sprite(0, 100, 100) { OriginX = 0.5f, OriginY = 0.5f });
                em.Replace(wall2, new Scale(1, 1));
                em.Replace(wall2, new Position(0, 0));
                em.Replace(wall2, Color.Green);

                string line = null;
                var y = 0;
                while ((line = sr.ReadLine()) != null)
                {
                    for (var x = 0; x < line.Length; x++)
                    {
                        if (line[x] == '1')
                        {
                            var wall = em.Create(wallSpec);
                            em.Replace(wall, new Sprite(0, TILE_SIZE, TILE_SIZE) { OriginX = 0, OriginY = 0 });
                            em.Replace(wall, new Position(x * TILE_SIZE, y * TILE_SIZE));
                            em.Replace(wall, new Scale(1, 1));
                            em.Replace(wall, Color.White);
                            em.Replace(wall, new Solid() { Bounds = AxisAlignedBox2.FromRect(float2.Zero, new float2(TILE_SIZE, TILE_SIZE)) });
                        }
                    }
                    y++;
                }
            }
        }

        private uint CreatePlayer()
        {
            var em = _serviceContainer.GetInstance<EntityManager>();

            var playerSpec = EntitySpec.Create<Position, Sprite, PlayerInput, TextureRegion, Collider, Move, Gravity, Input>();
            var player = em.Create(playerSpec);
            em.Replace(player, new PlayerInput());
            em.Replace(player, new Move() { Friction = new float2(0.85f), Acceleration = new float2(2000), Speed = float2.Zero });
            em.Replace(player, new Position(TILE_SIZE, 21 * TILE_SIZE));
            //_entities.Replace(player, new Sprite(Sprites.Player, 32, 48) { OriginX = 0.5f, OriginY = 1f });
            em.Replace(player, new Collider() { Type = ColliderType.Player, Bounds = AxisAlignedBox2.FromRect(new float2(16, 48), new float2(32, 48)) });
            //_entities.Replace(player, FromTexture(Sprites.Player, 16, 24, 0, 2));
            em.Replace(player, Gravity.Default);
            return player;
        }
        public static TextureRegion FromTexture(Texture2D texture, int tileWidth, int tileHeight, int tileX, int tileY)
        {
            var tilesX = texture.Width / tileWidth;
            var tilesY = texture.Height / tileHeight;

            Assert.EqualTo(tilesX * tileWidth, texture.Width);
            Assert.EqualTo(tilesY * tileHeight, texture.Height);

            var tileW = 1f / tilesX;
            var tileH = 1f / tilesY;

            return TextureRegion.FromSize(tileX * tileW, tileY * tileW, tileW, tileH);
        }
        private void CreateCamera(uint target)
        {
            var em = _serviceContainer.GetInstance<EntityManager>();
            var cameraSpec = EntitySpec.Create<Camera, Position>();
            var camera = em.Create(cameraSpec);
            em.Replace(camera, new Camera()
            {
                EntityTrack = target,
                TrackSpeed = 10f,
                Width = graphics.PreferredBackBufferWidth,
                Height = graphics.PreferredBackBufferHeight
            });
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            Reload();

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            updateTimer.Restart();

            _services.Tick(dt);

            _gee?.Update(dt);

            updateTimer.Stop();
            _updateAvg += updateTimer;
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            renderTimer.Restart();

            if (_gee != null)
            {
                GraphicsDevice.Clear(new Color(50, 50, 50, 255));
                _gee.Draw(dt);
            }
            else
            {
                GraphicsDevice.Clear(new Color(255, 50, 50, 255));
            }

            renderTimer.Stop();
            _renderAvg += renderTimer;

            Window.Title = $"Update: {_updateAvg}, Render: {_renderAvg} {renderTimer.ElapsedMilliseconds}";
        }

    }
}
