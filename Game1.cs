namespace Worm
{
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
    public struct Position
    {
        public float x;
        public float y;

        public Position(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }

    public struct Velocity
    {
        public float x;
        public float y;
        public Velocity(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }

    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D _white;

        private ILoggerFactory _logFactory;
        private ILogger _logger;
        private EntityManager _entities;


        private IAllocator _memory;


        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _logFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole(configure =>
                {
                });
            });

            _logger = _logFactory.CreateLogger<Game1>();
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();

            _memory = new HeapAllocator(_logFactory);
            _entities = new EntityManager(_logFactory, _memory);

            var r = new Random();
            var spec = EntitySpec.Create<Position, Velocity, Color>();
            for (var i = 0; i < 100000; i++)
            {
                //TODO: bulk insert API
                var entity = _entities.Create(spec);
                _entities.Replace(entity, new Position(r.Next(0, 1024), r.Next(0, 1024)));
                _entities.Replace(entity, new Velocity(r.Next(-500, 500), r.Next(-500, 500)));
                _entities.Replace(entity, new Color(r.Next(255), r.Next(255), r.Next(255), 255));
            }
        }

        protected override void LoadContent()
        {
            _white = new Texture2D(GraphicsDevice, 1, 1);
            _white.SetData(new[] { Color.White });

            spriteBatch = new SpriteBatch(GraphicsDevice);

        }

        private Stopwatch updateTimer0 = new Stopwatch();
        private Stopwatch updateTimer1 = new Stopwatch();
        private Stopwatch renderTimer = new Stopwatch();
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            var maxx = GraphicsDevice.PresentationParameters.BackBufferWidth;
            var maxy = GraphicsDevice.PresentationParameters.BackBufferHeight;
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            updateTimer0.Restart();
            _entities.ForEach((uint entity, ref Position position, ref Velocity velocity) =>
            {
                position.x += velocity.x * dt;
                position.y += velocity.y * dt;

                velocity.x -= velocity.x * dt;
                velocity.y -= velocity.y * dt;

                if ((position.x > maxx && velocity.x > 0) || (position.x < 0 && velocity.x < 0))
                    velocity.x = -velocity.x;

                if ((position.y > maxy && velocity.y > 0) || (position.y < 0 && velocity.y < 0))
                    velocity.y = -velocity.y;

            });
            updateTimer0.Stop();
            ManualUpdate(dt, maxx, maxy);

            base.Update(gameTime);
        }

        private void ManualUpdate(float dt, float maxx, float maxy)
        {
            updateTimer1.Restart();
            Span<ComponentType> componentTypes = stackalloc ComponentType[] {
                ComponentType<Position>.Type,
                ComponentType<Velocity>.Type
            };

            var entityArrays = _entities.EntityArrays;
            for (var i = 0; i < entityArrays.Count; i++)
            {
                var array = entityArrays[i];
                if (array.Specification.HasAll(componentTypes))
                {
                    var t0i = -1;
                    var t1i = -1;

                    for (var k = 0; k < array.AllChunks.Count; k++)
                    {
                        var chunk = array.AllChunks[k];
                        var length = chunk.Count;
                        if (t0i == -1) t0i = chunk.PackedArray.GetComponentIndex(componentTypes[0]);
                        if (t1i == -1) t1i = chunk.PackedArray.GetComponentIndex(componentTypes[1]);

                        var t0 = chunk.PackedArray.GetComponentSpan<Position>(t0i, componentTypes[0]);
                        var t1 = chunk.PackedArray.GetComponentSpan<Velocity>(t1i, componentTypes[1]);
                        for (var j = 0; j < length; j++)
                        {
                            ref var position = ref t0[j];
                            ref var velocity = ref t1[j];
                            position.x += velocity.x * dt;
                            position.y += velocity.y * dt;

                            velocity.x -= velocity.x * dt;
                            velocity.y -= velocity.y * dt;

                            if ((position.x > maxx && velocity.x > 0) || (position.x < 0 && velocity.x < 0))
                                velocity.x = -velocity.x;

                            if ((position.y > maxy && velocity.y > 0) || (position.y < 0 && velocity.y < 0))
                                velocity.y = -velocity.y;
                        }
                    }
                }
            }
            updateTimer1.Stop();
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(50, 50, 50, 255));

            renderTimer.Restart();
            spriteBatch.Begin();
            _entities.ForEach((uint entity, ref Position position, ref Color color) =>
            {
                spriteBatch.Draw(_white, new Vector2(position.x, position.y), color);
            });
            spriteBatch.End();
            renderTimer.Stop();

            _logger.LogInformation($"update0: {updateTimer0.Elapsed.TotalMilliseconds},update1: {updateTimer1.Elapsed.TotalMilliseconds}, render: {renderTimer.Elapsed.TotalMilliseconds}");

            base.Draw(gameTime);
        }

        protected override void UnloadContent()
        {
            _entities.Dispose();
            _memory.Dispose();
        }
    }
}
