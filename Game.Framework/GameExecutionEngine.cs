namespace Worm
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Atma;
    using Atma.Entities;
    using Atma.Memory;
    using Atma.Systems;
    using SimpleInjector;

    public class GameExecutionEngine : UnmanagedDispose
    {

        private Type[] _systemTypes;

        private Container _container;


        public bool IsModified => false;

        private Assembly _assembly;

        private string _pluginFile;
        private DateTime _lastWrite;

        private SystemManager _systems;

        public GameExecutionEngine(Container container, string plugin)
        {
            _container = container;
            _pluginFile = plugin;
            //_memory = memory;
            //_entities = entityManager;

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

            var systems = _container.GetAllInstances<ISystem>().ToArray();
            _systems = _container.GetInstance<SystemManager>();
            foreach (var system in systems)
                _systems.Add(system);

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
                    dirs.Push(d);

                var relDirName = d.Substring(dir.Length);
                var dstDir = Path.Combine(dst, relDirName);
                Directory.CreateDirectory(dstDir);

                foreach (var it in Directory.GetFiles(d))
                    File.Copy(it, Path.Combine(dst, Path.GetRelativePath(dir, it)));
            }
        }

        //public IEnumerable<ISystem> Systems => container.GetAllInstances<ISystem>();

        public virtual void InitOnce()
        {

        }

        public virtual void Init()
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
            _systems.Tick();
        }

        public virtual void Draw(float dt)
        {

        }

        protected override void OnManagedDispose()
        {
            _container.Dispose();
        }
    }
}