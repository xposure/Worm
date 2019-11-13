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

    // public struct Mass
    // {
    //     public float Amount;
    // }

    // public struct Force
    // {
    //     public float Amount;
    // }

    // public struct Acceleration
    // {
    //     public float Amount;
    // }

    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D _white;

        private ILoggerFactory _logFactory;
        private ILogger _logger;
        private EntityManager _entities;

        private IAllocator _memory;
        private BetterSpriteBatch _spriteBatch;

        private AvgValue _updateAvg = new AvgValue(0.9f, 1f);
        private AvgValue _renderAvg = new AvgValue(0.9f, 1f);

        private Texture2D _circle;

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
            base.Initialize();

            _memory = new HeapAllocator(_logFactory);
            _entities = new EntityManager(_logFactory, _memory);
            _spriteBatch = new BetterSpriteBatch(_memory, GraphicsDevice);

            var maxx = GraphicsDevice.PresentationParameters.BackBufferWidth;
            var maxy = GraphicsDevice.PresentationParameters.BackBufferHeight;


            var r = new Random();
            var spec = EntitySpec.Create<Position, Velocity, Color>();
            for (var i = 0; i < 100000; i++)
            {
                //TODO: bulk insert API
                var entity = _entities.Create(spec);
                _entities.Replace(entity, new Position(r.Next(10, maxx - 10), r.Next(10, maxy - 10)));
                //_entities.Replace(entity, new Velocity(r.Next(-5000, 5000), r.Next(-5000, 5000)));
                //_entities.Replace(entity, new Color(r.Next(255), r.Next(255), r.Next(255), 255));
            }
        }

        protected override void LoadContent()
        {
            _white = new Texture2D(GraphicsDevice, 1, 1);
            _white.SetData(new[] { Color.White });
            spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        private Stopwatch updateTimer0 = new Stopwatch();
        private Stopwatch renderTimer = new Stopwatch();
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            var maxx = GraphicsDevice.PresentationParameters.BackBufferWidth;
            var maxy = GraphicsDevice.PresentationParameters.BackBufferHeight;
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            updateTimer0.Restart();
            var mouse = Microsoft.Xna.Framework.Input.Mouse.GetState();
            var mp = new Vector2(mouse.Position.X, mouse.Position.Y);
            var explosion = mouse.LeftButton == ButtonState.Pressed;
            var mousePower = (mouse.LeftButton == ButtonState.Pressed) ? 100000 : 100;

            _entities.ForChunk((int length, ReadOnlySpan<uint> entities, Span<Position> positions, Span<Velocity> velocities, Span<Color> colors) =>
            {
                for (var i = 0; i < length; i++)
                {
                    ref var position = ref positions[i];
                    ref var velocity = ref velocities[i];

                    var vp = new Vector2(position.X, position.Y);

                    var dstSqr = Vector2.DistanceSquared(vp, mp);
                    var force = 200f / dstSqr;
                    if (force < 1)
                    {
                        var dir = Vector2.Normalize(vp - mp);
                        velocity.X += force * mousePower * dir.X;
                        velocity.Y += force * mousePower * dir.Y;
                    }

                    colors[i] = Color.Lerp(Color.Red, Color.Green, 1000f / (velocity.X * velocity.X + velocity.Y * velocity.Y));

                    if (velocity.X != 0)
                    {
                        position.X += velocity.X * dt;
                        velocity.X -= velocity.X * dt;
                        if (velocity.X > 0.0001 && velocity.X < 0.0001) velocity.X = 0;
                        if ((position.X > maxx - 10 && velocity.X > 0))
                        {
                            velocity.X = -velocity.X;
                            position.X = maxx - 10;
                        }
                        if ((position.X < 10 && velocity.X < 0))
                        {
                            velocity.X = -velocity.X;
                            position.X = 10;
                        }
                    }


                    if (velocity.Y != 0)
                    {
                        position.Y += velocity.Y * dt;
                        velocity.Y -= velocity.Y * dt;
                        if (velocity.Y > 0.0001 && velocity.Y < 0.0001) velocity.Y = 0;
                        if ((position.Y > maxy - 10 && velocity.Y > 0))
                        {
                            velocity.Y = -velocity.Y;
                            position.Y = maxy - 10;
                        }
                        if ((position.Y < 10 && velocity.Y < 0))
                        {
                            velocity.Y = -velocity.Y;
                            position.Y = 10;
                        }
                    }
                }
            });
            updateTimer0.Stop();
            _updateAvg += updateTimer0;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(50, 50, 50, 255));

            renderTimer.Restart();
            var scale = new Scale(5, 5);
            _entities.ForChunk((int length, ReadOnlySpan<uint> entities, Span<Position> positions, Span<Color> colors) =>
            {
                var i = 0;
                var remainingSprites = length;
                while (remainingSprites > 0)
                {
                    using var bulkSprites = _spriteBatch.TakeSprites(remainingSprites);
                    remainingSprites -= bulkSprites.Sprites.Length;

                    var sprites = bulkSprites.Sprites;
                    for (var spriteIndex = 0; spriteIndex < sprites.Length; spriteIndex++, i++)
                    {
                        ref var position = ref positions[i];
                        ref var color = ref colors[i];

                        ref var sprite = ref sprites[spriteIndex];

                        sprite.TL.Position = position;
                        sprite.TL.Color = color;

                        sprite.TR.Position.X = position.X + scale.Width;
                        sprite.TR.Position.Y = position.Y;
                        sprite.TR.Color = color;

                        sprite.BR.Position.X = position.X + scale.Width;
                        sprite.BR.Position.Y = position.Y + scale.Height;
                        sprite.BR.Color = color;

                        sprite.BL.Position.X = position.X;
                        sprite.BL.Position.Y = position.Y + scale.Height;
                        sprite.BL.Color = color;
                    }
                }
            });
            _spriteBatch.Render(spriteBatch);
            renderTimer.Stop();
            _renderAvg += renderTimer;

            Window.Title = $"Update: {_updateAvg}, Render: {_renderAvg} {renderTimer.ElapsedMilliseconds}, Commands: {_spriteBatch.Commands}, Triangles: {_spriteBatch.Triangles}";

            base.Draw(gameTime);
        }

        protected override void UnloadContent()
        {
            _entities.Dispose();
            _memory.Dispose();
        }
    }
}
