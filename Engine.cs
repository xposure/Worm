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
    using Atma.Common;
    using Atma.Entities;
    using System;
    using System.Diagnostics;
    using Microsoft.Extensions.Logging;
    using System.Collections.Generic;
    using Worm.Graphics;
    using Worm.Systems;
    using System.Linq;
    using Worm.Managers;
    using System.IO;
    using Atma.Math;

    public class Engine : Game
    {
        public const int TILE_SIZE = 32;

        private static Engine _instance;
        public static Engine Instance => _instance;

        GraphicsDeviceManager graphics;

        private ILoggerFactory _logFactory;
        private ILogger _logger;
        private EntityManager _entities;
        public EntityManager Entities => _entities;

        private IAllocator _memory;
        public IAllocator Memory => _memory;

        private Stopwatch updateTimer = new Stopwatch();
        private Stopwatch renderTimer = new Stopwatch();

        private AvgValue _updateAvg = new AvgValue(0.9f, 1f);
        private AvgValue _renderAvg = new AvgValue(0.9f, 1f);

        private List<ISystem> _systems = new List<ISystem>();

        public Engine()
        {
            _instance = this;
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _logFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole(configure =>
                {
                });
            });

            _logger = _logFactory.CreateLogger<Engine>();
        }

        protected unsafe override void LoadContent()
        {
            Sprites.Init();
            Animations.Init();
            Prefabs.Init();

            // var player = Sprites.AddTexture(@"p:\Games\Worm\sprite.png");
            // var playerAnimation = Animations.CreateAnimation(player, 16, 24, Enumerable.Range(0, 4).Select(x => new Point(x, 2)).ToArray());

            _systems.Add(new ColorLerpSystem());
            _systems.Add(new UnitSpawnerSystem());
            _systems.Add(new CameraTrackSystem());
            _systems.Add(new PlayerInputSystem());
            _systems.Add(new MoveSystem());
            _systems.Add(new PlayerUnitSelectSystem());
            _systems.Add(new ColliderSystem());
            _systems.Add(new AnimationSystem());
            _systems.Add(new RenderingSystem());

            _memory = new HeapAllocator(_logFactory);
            _entities = new EntityManager(_logFactory, _memory);

            // var unitSpawnerSpec = EntitySpec.Create<Position>(new UnitSpawner() { Prefab = Prefabs.Player });
            // var p0 = Entities.Create(unitSpawnerSpec);
            // Entities.Replace(p0, new Position(100, 100));

            // var p1 = Entities.Create(unitSpawnerSpec);
            // Entities.Replace(p1, new Position(200, 100));

            // var p2 = Entities.Create(unitSpawnerSpec);
            // Entities.Replace(p2, new Position(200, 200));

            CreateWalls();
            var player = CreatePlayer();
            CreateCamera(player);

            foreach (var it in _systems)
                it.Init();
        }

        private void CreateWalls()
        {
            using (var sr = File.OpenText(@"P:\Games\Worm\room.data"))
            {
                var wallSpec = EntitySpec.Create<Position, Sprite, Solid>();
                string line = null;
                var y = 0;
                while ((line = sr.ReadLine()) != null)
                {
                    for (var x = 0; x < line.Length; x++)
                    {
                        if (line[x] == '1')
                        {
                            var wall = _entities.Create(wallSpec);
                            _entities.Replace(wall, new Sprite(0, TILE_SIZE, TILE_SIZE) { OriginX = 0, OriginY = 0 });
                            _entities.Replace(wall, new Position(x * TILE_SIZE, y * TILE_SIZE));
                            _entities.Replace(wall, new Solid() { Bounds = AxisAlignedBox2.FromRect(float2.Zero, new float2(TILE_SIZE, TILE_SIZE)) });
                        }
                    }
                    y++;
                }
            }
        }

        private uint CreatePlayer()
        {
            var playerSpec = EntitySpec.Create<Position, Sprite, PlayerInput, TextureRegion, Collider, Move, Gravity, Input>();
            var player = _entities.Create(playerSpec);
            _entities.Replace(player, new PlayerInput() { Speed = 100 });
            _entities.Replace(player, new Position(TILE_SIZE, 21 * TILE_SIZE));
            _entities.Replace(player, new Sprite(Sprites.Player, 32, 48) { OriginY = 1f });
            _entities.Replace(player, new Collider() { Type = ColliderType.Player, Bounds = AxisAlignedBox2.FromDimensions(float2.Zero, new float2(32, 48)) });
            _entities.Replace(player, TextureRegion.FromTexture(Sprites.Player, 16, 24, 0, 2));
            _entities.Replace(player, Gravity.Default);
            return player;
        }

        private void CreateCamera(uint target)
        {
            var cameraSpec = EntitySpec.Create<Camera, Position>();
            var camera = _entities.Create(cameraSpec);
            _entities.Replace(camera, new Camera() { EntityTrack = target, TrackSpeed = 10f });
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            updateTimer.Restart();

            foreach (var it in _systems)
                it.Update(dt);

            base.Update(gameTime);
            updateTimer.Stop();
            _updateAvg += updateTimer;
        }

        protected override void Draw(GameTime gameTime)
        {
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            GraphicsDevice.Clear(new Color(50, 50, 50, 255));

            renderTimer.Restart();
            foreach (var it in _systems)
                it.Draw(dt);

            base.Draw(gameTime);
            renderTimer.Stop();
            _renderAvg += renderTimer;

            Window.Title = $"Update: {_updateAvg}, Render: {_renderAvg} {renderTimer.ElapsedMilliseconds}";
        }

        protected override void UnloadContent()
        {
            _entities.Dispose();
            Prefabs.Dispose();
            Animations.Dispose();
            Sprites.Dispose();
            _memory.Dispose();
        }
    }
}
