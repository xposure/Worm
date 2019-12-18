namespace Game.Framework
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Atma;
    using Atma.Entities;
    using Atma.Events;
    using Atma.Memory;
    using Atma.Systems;
    using SimpleInjector;

    public class GameExecutionEngine : UnmanagedDispose
    {
        private Type[] _systemTypes;
        private Type[] _systemProducers;

        private Container _container;

        private Assembly _assembly;

        private string _pluginFile;
        private DateTime _lastWrite;

        private SystemManager _systems;

        private EventManager _events;

        private EntityManager _entities;

        public GameExecutionEngine(Container container, string plugin)
        {
            _container = container;
            _pluginFile = plugin;
            if (!Path.IsPathRooted(_pluginFile))
                _pluginFile = Path.Combine(System.Environment.CurrentDirectory, _pluginFile);

        }

        private void WaitForAccess(string file)
        {
            while (true)
            {
                try
                {
                    using (var sr = File.OpenWrite(file))
                        return;
                }
                catch { System.Threading.Thread.Sleep(10); }
            }
        }

        private void BuildContainer()
        {
            //new system manager
            _container.RegisterSingleton<SystemManager>();

            foreach (var it in _systemTypes)
                _container.Collection.Append(typeof(ISystem), it, Lifestyle.Singleton);

            foreach (var it in _systemProducers)
                _container.Collection.Append(typeof(SystemProducer), it, Lifestyle.Singleton);

            _container.Verify();

            _entities = _container.GetInstance<EntityManager>();
            _events = _container.GetInstance<EventManager>();

            _systems = _container.GetInstance<SystemManager>();
            _systems.DefaultStage = nameof(UpdateStage);
            _systems.AddStage(nameof(UpdateStage));
            _systems.AddStage(nameof(RenderStage));

            foreach (var system in _container.GetAllInstances<ISystem>())
                _systems.Register(system);

            foreach (var system in _container.GetAllInstances<SystemProducer>())
                _systems.Register(system);


            _systems.Init();
        }

        private void CopyDirectory(string dir, string dst)
        {
            if (!dir.EndsWith('\\'))
                dir += "\\";

            var dirs = new Stack<string>();
            dirs.Push(dir);
            while (dirs.Count > 0)
            {
                var d = dirs.Pop();
                foreach (var it in Directory.GetDirectories(d))
                    dirs.Push(it);

                var relDirName = d.Substring(dir.Length);
                var dstDir = Path.Combine(dst, relDirName);
                Directory.CreateDirectory(dstDir);

                foreach (var it in Directory.GetFiles(d))
                    File.Copy(it, Path.Combine(dst, Path.GetRelativePath(dir, it)));
            }
        }

        //public IEnumerable<ISystem> Systems => container.GetAllInstances<ISystem>();

        public void Init()
        {
            WaitForAccess(_pluginFile);

            var folder = Path.GetRandomFileName();
            var temp = Path.GetTempPath();
            var path = Path.Combine(temp, folder);
            Directory.CreateDirectory(path);

            var pluginFolder = Path.GetDirectoryName(_pluginFile);
            CopyDirectory(pluginFolder, path);

            var dstFile = Path.Combine(path, Path.GetFileName(_pluginFile));

            _assembly = Assembly.LoadFile(dstFile);

            _systemTypes = (from type in _assembly.GetExportedTypes()
                            where typeof(ISystem).IsAssignableFrom(type)
                            select type).ToArray();

            _systemProducers = (from type in _assembly.GetExportedTypes()
                                where typeof(SystemProducer).IsAssignableFrom(type)
                                select type).ToArray();

            var fi = new FileInfo(_pluginFile);
            _lastWrite = fi.LastWriteTimeUtc;

            BuildContainer();
        }

        public bool CheckChange()
        {
            var fi = new FileInfo(_pluginFile);
            return _lastWrite != fi.LastWriteTimeUtc;
        }

        public virtual void Update(float dt)
        {
            _systems.Tick(nameof(UpdateStage));

            _events.Fire(nameof(Events.Tick), dt);
        }

        public virtual void Draw(float dt)
        {
            _systems.Tick(nameof(RenderStage));
        }

        protected override void OnManagedDispose()
        {
            _container.Dispose();
        }

        public void LoadScene()
        {
            _entities.ClearAll();
            _events.Fire(nameof(Events.LoadScene), 0f);
        }
    }
}