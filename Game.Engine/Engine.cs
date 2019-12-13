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
    using Atma.Systems;

    using System;
    using System.IO;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    using Worm.Systems;
    using Worm.Managers;
    using SimpleInjector;

    using Game.Framework;

    public class Engine : Game
    {

        public const int TILE_SIZE = 32;

        private static Engine _instance;
        public static Engine Instance => _instance;

        GraphicsDeviceManager graphics;

        private readonly ILoggerFactory _logFactory;
        private ILogger _logger;
        private readonly EntityManager _entities;
        public EntityManager Entities => _entities;

        //private SystemManager

        private readonly IAllocator _memory;
        public IAllocator Memory => _memory;

        private Stopwatch updateTimer = new Stopwatch();
        private Stopwatch renderTimer = new Stopwatch();

        private AvgValue _updateAvg = new AvgValue(0.9f, 1f);
        private AvgValue _renderAvg = new AvgValue(0.9f, 1f);

        private Task<GameExecutionEngine> _geeReloadTask;

        private GameExecutionEngine _gee;
        private bool _isRunning = true;

        public Engine()
        {
            _instance = this;
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _logFactory = LoggerFactory.Create(builder => { builder.AddConsole(configure => { }); });
            _logger = _logFactory.CreateLogger<Engine>();

            _memory = new HeapAllocator(_logFactory);
            _entities = new EntityManager(_logFactory, _memory);

        }

        private GameExecutionEngine CheckGEE()
        {
            while (_isRunning)
            {
                if (_gee == null || _gee.CheckChange())
                {
                    System.Console.WriteLine("Reloading");
                    var container = CreateDI();
                    var gee = new GameExecutionEngine(container, "Game.Logic\\bin\\Game.Logic.dll");
                    gee.Init();
                    return gee;
                }

                System.Threading.Thread.Sleep(100);
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
            container.RegisterInstance<IAllocator>(_memory);
            container.RegisterInstance<EntityManager>(_entities);

            //logging
            container.RegisterInstance<ILoggerFactory>(_logFactory);
            container.Register(typeof(ILogger<>), typeof(Logger<>));

            container.Collection.Append(typeof(ISystem), typeof(SpriteRenderer), Lifestyle.Singleton);

            return container;
        }

        protected override void Initialize()
        {
            _geeReloadTask = Task.Run(() => CheckGEE());
            base.Initialize();
        }

        private void Reload()
        {
            if (_geeReloadTask.IsCompletedSuccessfully)
            {
                _gee?.Dispose();
                _gee = _geeReloadTask.Result;
                GC.GetTotalMemory(true);
                _geeReloadTask = Task.Run(() => CheckGEE());
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Prefabs.Dispose();
                Animations.Dispose();
                Sprites.Dispose();

                _entities.Dispose();
                _memory.Dispose();
            }

            base.Dispose(disposing);
        }

        protected unsafe override void LoadContent()
        {
            base.LoadContent();
            //DI.RegisterInstance(Sprites);
            //DI.RegisterInstance(Animations);
            //DI.RegisterInstance(Prefabs);
            Sprites.Init();
            Animations.Init();
            Prefabs.Init();

            CreateWalls();
            //var player = CreatePlayer();
            //CreateCamera(player);
            CreateCamera(0);
        }

        private void CreateWalls()
        {
            using (var sr = File.OpenText(@"Assets\room.data"))
            {
                var wallSpec = EntitySpec.Create<Position, Sprite, Color, Solid, Scale>();

                var wall2 = _entities.Create(wallSpec);
                _entities.Replace(wall2, new Sprite(1, 100, 100) { OriginX = 0.5f, OriginY = 0.5f });
                _entities.Replace(wall2, new Scale(1, 1));
                _entities.Replace(wall2, new Position(0, 0));
                _entities.Replace(wall2, Color.Green);

                // string line = null;
                // var y = 0;
                // while ((line = sr.ReadLine()) != null)
                // {
                //     for (var x = 0; x < line.Length; x++)
                //     {
                //         if (line[x] == '1')
                //         {
                //             var wall = _entities.Create(wallSpec);
                //             _entities.Replace(wall, new Sprite(0, TILE_SIZE, TILE_SIZE) { OriginX = 0, OriginY = 0 });
                //             _entities.Replace(wall, new Position(x * TILE_SIZE, y * TILE_SIZE));
                //             _entities.Replace(wall, new Solid() { Bounds = AxisAlignedBox2.FromRect(float2.Zero, new float2(TILE_SIZE, TILE_SIZE)) });
                //         }
                //     }
                //     y++;
                // }
            }
        }

        private uint CreatePlayer()
        {
            var playerSpec = EntitySpec.Create<Position, Sprite, PlayerInput, TextureRegion, Collider, Move, Gravity, Input>();
            var player = _entities.Create(playerSpec);
            _entities.Replace(player, new PlayerInput());
            _entities.Replace(player, new Move() { Friction = new float2(0.85f), Acceleration = new float2(2000), Speed = float2.Zero });
            _entities.Replace(player, new Position(TILE_SIZE, 21 * TILE_SIZE));
            _entities.Replace(player, new Sprite(Sprites.Player, 32, 48) { OriginX = 0.5f, OriginY = 1f });
            _entities.Replace(player, new Collider() { Type = ColliderType.Player, Bounds = AxisAlignedBox2.FromRect(new float2(16, 48), new float2(32, 48)) });
            _entities.Replace(player, FromTexture(Sprites.Player, 16, 24, 0, 2));
            _entities.Replace(player, Gravity.Default);
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
            var cameraSpec = EntitySpec.Create<Camera, Position>();
            var camera = _entities.Create(cameraSpec);
            _entities.Replace(camera, new Camera() { EntityTrack = target, TrackSpeed = 10f });
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            Reload();

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            updateTimer.Restart();

            _gee?.Update(dt);

            updateTimer.Stop();
            _updateAvg += updateTimer;
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            GraphicsDevice.Clear(new Color(50, 50, 50, 255));

            renderTimer.Restart();

            _gee?.Draw(dt);

            renderTimer.Stop();
            _renderAvg += renderTimer;

            Window.Title = $"Update: {_updateAvg}, Render: {_renderAvg} {renderTimer.ElapsedMilliseconds}";
        }

    }
}
