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
    using Atma.Events;

    public class Engine : Game
    {

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
            container.RegisterInstance<IEventManager>(_serviceContainer.GetInstance<IEventManager>());
            container.RegisterInstance<IAutoEventManager>(_serviceContainer.GetInstance<IAutoEventManager>());

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
            _serviceContainer.RegisterSingleton(typeof(IEventManager), typeof(EventManager));
            _serviceContainer.RegisterSingleton(typeof(IAutoEventManager), typeof(AutoEventManager));

            _services = new GameServiceManager(typeof(Engine).Assembly, _serviceContainer, _logFactory);

            _serviceContainer.Verify();

            _services.Initialize();

            Reload();

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

            _gee.LoadScene();
        }


        private bool _shouldReload = false;
        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            Reload();

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (Keyboard.GetState().IsKeyDown(Keys.F12))
                _shouldReload = true;

            if (_shouldReload && Keyboard.GetState().IsKeyUp(Keys.F12))
            {
                _shouldReload = false;
                _logger.LogDebug("LoadScene");
                _gee?.LoadScene();
            }

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
                GraphicsDevice.Clear(new Color(255, 255, 255, 255));
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
