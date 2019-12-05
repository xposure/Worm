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

    public class Engine : Game
    {
        private static Engine _instance;
        public static Engine Instance => _instance;


        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D _white;

        private ILoggerFactory _logFactory;
        private ILogger _logger;
        private EntityManager _entities;
        public EntityManager Entities => _entities;

        private IAllocator _memory;
        public IAllocator Memory => _memory;
        private BetterSpriteBatch _spriteBatch;

        private AvgValue _updateAvg = new AvgValue(0.9f, 1f);
        private AvgValue _renderAvg = new AvgValue(0.9f, 1f);

        private Texture2D _circle;

        private List<ISystem> _systems = new List<ISystem>();

        private MTexture _test;
        private MTexture _test2;
        private MTexture _test3;
        private MTexture _test4;

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
            base.Initialize();
            // var atlasBuilder = new AtlasBuilder();
            // atlasBuilder.AddPath(@"P:\Games\Assets\Adventure_Pack_v3\Grass", "gtile*.png");

            // _test = MTexture.FromFile(@"p:\Games\Assets\Adventure_Pack_v3\Grass\gtile44.png");//  atlasBuilder.Build().Texture;
            // _test2 = MTexture.FromFile(@"p:\Games\Assets\Adventure_Pack_v3\Grass\grass_blur_orange.png");//  atlasBuilder.Build().Texture;
            // _test3 = MTexture.FromFile(@"p:\Games\Assets\Adventure_Pack_v3\Grass\grass_blur_blue.png");//  atlasBuilder.Build().Texture;
            _test4 = MTexture.FromFile(@"p:\Games\Worm\sprite.png");

            _systems.Add(new ColorLerpSystem());

            _memory = new HeapAllocator(_logFactory);
            _entities = new EntityManager(_logFactory, _memory);
            _spriteBatch = new BetterSpriteBatch(_memory, GraphicsDevice);

            var maxx = GraphicsDevice.PresentationParameters.BackBufferWidth;
            var maxy = GraphicsDevice.PresentationParameters.BackBufferHeight;


            var r = new Random();
            var spec = EntitySpec.Create<Position, Velocity, Color, Scale>();
            for (var i = 0; i < 0; i++)
            {
                //TODO: bulk insert API
                var entity = _entities.Create(spec);
                _entities.Replace(entity, new Position(r.Next(10, maxx - 10), r.Next(10, maxy - 10)));
                var scale = r.Next(5, 25);
                _entities.Replace(entity, new Scale(scale, scale));
                //_entities.Replace(entity, new Velocity(r.Next(-5000, 5000), r.Next(-5000, 5000)));
                //_entities.Replace(entity, new Color(r.Next(255), r.Next(255), r.Next(255), 255));
            }


        }

        protected override void LoadContent()
        {
            _white = new Texture2D(GraphicsDevice, 1, 1);
            _white.SetData(new[] { Color.White });

            var r = 8;
            var rsq = r * r;
            _circle = new Texture2D(GraphicsDevice, r * 2, r * 2);
            var circleData = new Color[_circle.Width * _circle.Height];
            for (var y = 0; y < _circle.Height; y++)
            {
                for (var x = 0; x < _circle.Width; x++)
                {
                    var xr = x - r;
                    var yr = y - r;

                    var psq = xr * xr + yr * yr;

                    var idx = _circle.Width * y + x;
                    circleData[idx] = Color.White;
                    var d = 1f - Math.Clamp((float)psq / rsq, 0, 1);
                    var p = (byte)(d * 255);

                    //circleData[idx] = p
                    circleData[idx] = new Color(p, p, p, (byte)64);
                }
            }
            _circle.SetData(circleData);

            spriteBatch = new SpriteBatch(GraphicsDevice);

            foreach (var it in _systems)
                it.Init();
        }

        private Stopwatch updateTimer0 = new Stopwatch();
        private Stopwatch renderTimer = new Stopwatch();
        private MouseState _lastMouse;
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            updateTimer0.Restart();
            var mouse = Microsoft.Xna.Framework.Input.Mouse.GetState();
            var mp = new Vector2(mouse.Position.X, mouse.Position.Y);

            if (mouse.LeftButton == ButtonState.Pressed)
            {
                var lastMp = new Vector2(_lastMouse.Position.X, _lastMouse.Position.Y) - mp;
                cx -= lastMp.X;
                cy -= lastMp.Y;
            }

            _lastMouse = mouse;
            updateTimer0.Stop();
            _updateAvg += updateTimer0;

            foreach (var it in _systems)
                it.Update(dt, _entities);

            base.Update(gameTime);
        }

        private float cx = 0, cy = 0;
        private float cameraSpeed = 100;
        private int ii = 0;
        protected override void Draw(GameTime gameTime)
        {
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            GraphicsDevice.Clear(new Color(50, 50, 50, 255));

            renderTimer.Restart();
            _spriteBatch.SetBlendState(BlendState.NonPremultiplied);
            //_spriteBatch.SetTexture(_test.Texture);
            // _entities.ForChunk((int length, ReadOnlySpan<uint> entities, Span<Position> positions, Span<Color> colors, Span<Scale> scales) =>
            // {
            //     var i = 0;
            //     var remainingSprites = length;
            //     while (remainingSprites > 0)
            //     {
            //         using var bulkSprites = _spriteBatch.TakeSprites(remainingSprites);
            //         remainingSprites -= bulkSprites.Sprites.Length;

            //         var sprites = bulkSprites.Sprites;
            //         for (var spriteIndex = 0; spriteIndex < sprites.Length; spriteIndex++, i++)
            //         {
            //             var position = positions[i];
            //             ref var scale = ref scales[i];

            //             position.X -= scale.Height / 2;
            //             position.Y -= scale.Width / 2;

            //             ref var color = ref colors[i];

            //             ref var sprite = ref sprites[spriteIndex];

            //             sprite.TL.Position = position;
            //             sprite.TL.Color = color;

            //             sprite.TR.Position.X = position.X + scale.Width;
            //             sprite.TR.Position.Y = position.Y;
            //             sprite.TR.Color = color;

            //             sprite.BR.Position.X = position.X + scale.Width;
            //             sprite.BR.Position.Y = position.Y + scale.Height;
            //             sprite.BR.Color = color;

            //             sprite.BL.Position.X = position.X;
            //             sprite.BL.Position.Y = position.Y + scale.Height;
            //             sprite.BL.Color = color;
            //         }
            //     }
            // });
            // rotation += dt;

            var keyboard = Microsoft.Xna.Framework.Input.Keyboard.GetState();
            if (keyboard.IsKeyDown(Keys.Right)) cx -= cameraSpeed * dt;
            if (keyboard.IsKeyDown(Keys.Left)) cx += cameraSpeed * dt;
            if (keyboard.IsKeyDown(Keys.Up)) cy += cameraSpeed * dt;
            if (keyboard.IsKeyDown(Keys.Down)) cy -= cameraSpeed * dt;

            _spriteBatch.SetSamplerState(SamplerState.PointClamp);
            _spriteBatch.SetCamera(Matrix.CreateTranslation((int)cx, (int)cy, 0));
            // var scale = 32;
            // for (var y = 0; y < 128; y++)
            //     for (var x = 0; x < 128; x++)
            //         _spriteBatch.AddSprite(_test.Texture, new Position(x * scale, y * scale), new Scale(scale, scale));
            // //_test.DrawCentered(_spriteBatch, new Vector2(100, 100));
            // //_spriteBatch.SetBlendState(BlendState.Additive);
            // var r = new Random(12345679);
            // for (var i = 0; i < 10; i++)
            // {
            //     var x = r.Next(0, 128 * scale);
            //     var y = r.Next(0, 128 * scale);
            //     var s = r.Next(1500, 2500);
            //     var ca = r.Next(64, 164);
            //     var cr = r.Next(192, 255);
            //     var cg = r.Next(192, 255);
            //     var cb = r.Next(192, 255);
            //     _spriteBatch.AddSprite(_test2.Texture,
            //                     new Position(x - _test2.Texture.Width / 2, y - _test2.Texture.Height / 2),
            //                     new Scale(s, s),
            //                     new Color(cr, cg, cb, ca));
            // }
            // for (var i = 0; i < 10; i++)
            // {
            //     var x = r.Next(0, 128 * scale);
            //     var y = r.Next(0, 128 * scale);
            //     var s = r.Next(500, 2000);
            //     var ca = r.Next(128, 164);
            //     var cr = r.Next(192, 255);
            //     var cg = r.Next(192, 255);
            //     var cb = r.Next(192, 255);
            //     _spriteBatch.AddSprite(_test3.Texture,
            //                     new Position(x - _test3.Texture.Width / 2, y - _test3.Texture.Height / 2),
            //                     new Scale(s, s),
            //                     new Color(cr, cg, cb, ca));
            // }
            var fps = 9;
            ii = (ii + 1) % (4 * fps);
            var mt = _test4.GetSubtexture((ii / fps * 16), 48, 16, 24);
            _spriteBatch.SetSamplerState(SamplerState.PointClamp);
            mt.DrawCentered(_spriteBatch, new Vector2(100, 100), Color.White, 4f);
            //_spriteBatch.AddSprite(_test4.GetSubtexture((ii * 16)), new Position(0, 0), new Scale(100, 100), Color.White);
            _spriteBatch.Render();

            foreach (var it in _systems)
                it.Draw(dt, _entities);

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
