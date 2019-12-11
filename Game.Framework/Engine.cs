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
    using SimpleInjector;
    using System.Reflection;
    using Atma.Systems;

    public class Engine : Game
    {

        private readonly static Container DI = new Container();

        public const int TILE_SIZE = 32;

        private static Engine _instance;
        public static Engine Instance => _instance;

        GraphicsDeviceManager graphics;

        private ILoggerFactory _logFactory;
        private ILogger _logger;
        private EntityManager _entities;
        public EntityManager Entities => _entities;

        //private SystemManager

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

        protected override void Initialize()
        {
            DI.RegisterInstance(this);
            DI.RegisterInstance(GraphicsDevice);
            DI.RegisterInstance(graphics);
            DI.RegisterSingleton<IAllocator, HeapAllocator>();
            DI.RegisterInstance<ILoggerFactory>(_logFactory);
            DI.Register(typeof(ILogger<>), typeof(Logger<>));

            DI.RegisterSingleton<EntityManager>();
            DI.RegisterSingleton<SystemManager>();


            var asm = Assembly.GetExecutingAssembly();
            var systemTypes =
                from type in asm.GetExportedTypes()
                where typeof(ISystem).IsAssignableFrom(type)
                select type;

            foreach (var it in systemTypes)
                DI.Collection.Append(typeof(ISystem), it, Lifestyle.Singleton);

            var systems = DI.GetAllInstances<ISystem>().ToArray();
            var sm = DI.GetInstance<SystemManager>();
            foreach (var system in systems)
            {
                _logger.LogDebug(system.Name);
                sm.Add(system);
            }

            systems = DI.GetAllInstances<ISystem>().ToArray();
            sm = DI.GetInstance<SystemManager>();
            foreach (var system in systems)
            {
                _logger.LogDebug(system.Name);
                //sm.Add(system);
            }

            sm.Init();

            _memory = DI.GetInstance<IAllocator>();
            _entities = DI.GetInstance<EntityManager>();

            base.Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                DI?.Dispose();

            base.Dispose(disposing);
        }

        protected unsafe override void LoadContent()
        {
            //DI.RegisterInstance(Sprites);
            //DI.RegisterInstance(Animations);
            //DI.RegisterInstance(Prefabs);
            Sprites.Init();
            Animations.Init();
            Prefabs.Init();

            // var player = Sprites.AddTexture(@"p:\Games\Worm\sprite.png");
            // var playerAnimation = Animations.CreateAnimation(player, 16, 24, Enumerable.Range(0, 4).Select(x => new Point(x, 2)).ToArray());
            //var container = new Container();

            // _systems.Add(new ColorLerpSystem());
            // _systems.Add(new UnitSpawnerSystem());
            // _systems.Add(new CameraTrackSystem());
            // _systems.Add(new PlayerInputSystem());
            // _systems.Add(new MoveSystem());
            // _systems.Add(new PlayerUnitSelectSystem());
            // _systems.Add(new ColliderSystem());
            // _systems.Add(new AnimationSystem());
            // _systems.Add(new RenderingSystem());

            //_entities = new EntityManager(_logFactory, _memory);

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
            using (var sr = File.OpenText(@"Assets\room.data"))
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
            _entities.Replace(player, new PlayerInput());
            _entities.Replace(player, new Move() { Friction = new float2(0.85f), Acceleration = new float2(2000), Speed = float2.Zero });
            _entities.Replace(player, new Position(TILE_SIZE, 21 * TILE_SIZE));
            _entities.Replace(player, new Sprite(Sprites.Player, 32, 48) { OriginX = 0.5f, OriginY = 1f });
            _entities.Replace(player, new Collider() { Type = ColliderType.Player, Bounds = AxisAlignedBox2.FromRect(new float2(16, 48), new float2(32, 48)) });
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


            var sm = DI.GetInstance<SystemManager>();
            var em = DI.GetInstance<EntityManager>();
            foreach (var it in _systems)
                it.Tick(sm, em);
            //it.Update(dt);

            base.Update(gameTime);
            updateTimer.Stop();
            _updateAvg += updateTimer;
        }

        protected override void Draw(GameTime gameTime)
        {
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            GraphicsDevice.Clear(new Color(50, 50, 50, 255));

            renderTimer.Restart();
            // foreach (var it in _systems)
            //     it.Draw(dt);

            base.Draw(gameTime);
            renderTimer.Stop();
            _renderAvg += renderTimer;

            Window.Title = $"Update: {_updateAvg}, Render: {_renderAvg} {renderTimer.ElapsedMilliseconds}";
        }

        protected override void UnloadContent()
        {
            Prefabs.Dispose();
            Animations.Dispose();
            Sprites.Dispose();
        }
    }
}
