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

    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D _white;

        private EntityManager _entities;
        private IAllocator _memory = new HeapAllocator();

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

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();

            _entities = new EntityManager(_memory);

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

        private Stopwatch updateTimer = new Stopwatch();
        private Stopwatch renderTimer = new Stopwatch();
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            var maxx = GraphicsDevice.PresentationParameters.BackBufferWidth;
            var maxy = GraphicsDevice.PresentationParameters.BackBufferHeight;
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            updateTimer.Restart();
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
            updateTimer.Stop();

            base.Update(gameTime);
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

            Console.WriteLine($"update: {updateTimer.Elapsed.TotalMilliseconds}, render: {renderTimer.Elapsed.TotalMilliseconds}");
            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }

        protected override void UnloadContent()
        {
            _entities.Dispose();
            _memory.Dispose();
        }
    }
}
