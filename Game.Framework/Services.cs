namespace Game.Framework
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Atma;
    using Microsoft.Extensions.Logging;
    using SimpleInjector;

    public interface IGameService //: IDisposable
    {
        //Type Interface { get; }
        void Initialize();

        void Tick(float dt);
    }

    // public abstract class GameService<T> : UnmanagedDispose, IGameService
    // {

    //     public void Initialize() => OnInitialize();

    //     public void Tick() => OnTick();

    //     protected virtual void OnInitialize() { }

    //     protected virtual void OnTick() { }
    // }

    public class GameServiceManager : UnmanagedDispose
    {
        private ILogger _logger;
        private HashSet<Type> _services = new HashSet<Type>();
        private List<IGameService> _gameServices = new List<IGameService>();

        private Container _serviceContainer;
        private Assembly _platformAssembly;

        public GameServiceManager(Assembly platformAsm, Container serviceContainer, ILoggerFactory logFactory)
        {
            _platformAssembly = platformAsm;
            _serviceContainer = serviceContainer;
            _logger = logFactory.CreateLogger<GameServiceManager>();

            var ourAssembly = typeof(GameServiceManager).Assembly;
            var ourTypes = ourAssembly.GetExportedTypes();

            foreach (var type in ourTypes)
            {
                if (type.IsInterface)
                {
                    var gameService = type.GetCustomAttribute<GameServiceAttribute>();
                    if (gameService != null)
                    {
                        _logger.LogDebug($"Found service interface [{type.FullName}].");
                        _services.Add(type);
                    }
                }
            }

            if (_services.Count == 0)
                _logger.LogError("No services found!");

            RegisterPlatformServices(_platformAssembly, _serviceContainer);
        }

        public void Initialize()
        {
            var gameServiceType = typeof(IGameService);
            foreach (var it in _services)
            {
                var service = _serviceContainer.GetInstance(it);
                var serviceType = service.GetType();
                if (gameServiceType.IsAssignableFrom(serviceType))
                    _gameServices.Add((IGameService)service);

            }

            foreach (var it in _gameServices)
                it.Initialize();
        }

        private bool IsValidServiceInterface(Type it) => it != typeof(IDisposable) && _services.Contains(it);

        private void RegisterPlatformServices(Assembly platformAsm, Container container)
        {
            var matchedServices = new HashSet<Type>();
            var platformServicesQuery = from serviceType in platformAsm.GetExportedTypes()
                                        where serviceType.IsClass && !serviceType.IsAbstract
                                        from serviceInterface in serviceType.GetInterfaces()
                                        where IsValidServiceInterface(serviceInterface)
                                        select new { serviceType, serviceInterface };

            var platformServices = platformServicesQuery.ToArray();
            foreach (var it in platformServices)
            {
                if (!matchedServices.Add(it.serviceInterface))
                    _logger.LogError($"Duplicate service implementation [{it.serviceType.FullName}] for service [{it.serviceInterface.FullName}].");
                else
                    container.RegisterSingleton(it.serviceInterface, it.serviceType);
            }

            var frameworkServicesQuery = from serviceType in typeof(GameServiceManager).Assembly.GetExportedTypes()
                                         where serviceType.IsClass && !serviceType.IsAbstract
                                         where !matchedServices.Contains(serviceType)
                                         from serviceInterface in serviceType.GetInterfaces()
                                         where IsValidServiceInterface(serviceInterface)
                                         select new { serviceType, serviceInterface };

            var framworkServices = frameworkServicesQuery.ToArray();
            foreach (var it in framworkServices)
                if (matchedServices.Add(it.serviceInterface))
                    container.RegisterSingleton(it.serviceInterface, it.serviceType);

            if (matchedServices.Count != _services.Count)
            {
                var sb = new StringBuilder();
                foreach (var it in _services)
                {
                    if (!matchedServices.Contains(it))
                    {
                        _logger.LogError($"Missing service implementation of [{it.FullName}].");
                        sb.AppendLine(it.FullName);
                    }
                }

                throw new Exception("The following services were missing in the platform.\n" + sb.ToString());
            }
        }

        public void RegisterPlatformInstances(Container services, Container target)
        {
            foreach (var it in _services)
            {
                var service = services.GetInstance(it);
                target.RegisterInstance(it, service);

            }
        }

        public void Tick(float dt)
        {
            foreach (var it in _gameServices)
                it.Tick(dt);
        }

        protected override void OnManagedDispose()
        {
            _gameServices.Clear();
        }
    }
}