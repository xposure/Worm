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

    public class Engine : Game
    {
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

            var player = Sprites.AddTexture(@"p:\Games\Worm\sprite.png");
            var playerAnimation = Animations.CreateAnimation(player, 16, 24, Enumerable.Range(0, 4).Select(x => new Point(x, 2)).ToArray());

            _systems.Add(new ColorLerpSystem());
            _systems.Add(new UnitSpawnerSystem());
            _systems.Add(new PlayerInputSystem());
            _systems.Add(new AnimationSystem());
            _systems.Add(new RenderingSystem());

            _memory = new HeapAllocator(_logFactory);
            _entities = new EntityManager(_logFactory, _memory);

            var unitSpawnerSpec = EntitySpec.Create<Position>(new UnitSpawner() { Prefab = Prefabs.Player });
            var p0 = Entities.Create(unitSpawnerSpec);
            Entities.Replace(p0, new Position(100, 100));

            var p1 = Entities.Create(unitSpawnerSpec);
            Entities.Replace(p1, new Position(200, 100));

            var p2 = Entities.Create(unitSpawnerSpec);
            Entities.Replace(p2, new Position(200, 200));


            // var spec = new EntitySpec(componentTypes);
            // for (var i = 0; i < 3; i++)
            // {
            //     //TODO: bulk insert API
            //     var entity = _entities.Create(spec);
            //     _entities.Replace(entity, new Position(r.Next(10, maxx - 10), r.Next(10, maxy - 10)));
            //     _entities.Replace(entity, new Sprite(player, 16, 24) { FlipX = (i % 2) == 0, FlipY = (i % 3) == 0 });
            //     var scale = r.Next(1, 3);
            //     _entities.Replace(entity, new Scale(scale, scale));
            //     _entities.Replace(entity, new SpriteAnimation(playerAnimation.AnimationID, fps: 1f / 10, enabled: true));
            //     _entities.Replace(entity, playerAnimation.Frames[0].Region);
            //     _entities.Replace(entity, Color.White);

            //     //_entities.Replace(entity, new Velocity(r.Next(-5000, 5000), r.Next(-5000, 5000)));
            //     //_entities.Replace(entity, new Color(r.Next(255), r.Next(255), r.Next(255), 255));
            // }

            foreach (var it in _systems)
                it.Init();
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
