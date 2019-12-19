namespace Game.Framework.Services
{
    using Atma;
    using Atma.Common;
    using GlobExpressions;
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    [GameService]
    public interface IFileSystem : IDisposable
    {
        IEnumerable<IReadOnlyFile> FindAssets(Glob pattern);
        IObservable<IReadOnlyFile> AssetTracker { get; }

        IObserver<IReadOnlyFile> Observe(Glob pattern, Action<IReadOnlyFile> callback);
    }

    public interface IReadOnlyFile
    {
        int ID { get; }
        string Name { get; }

        Stream OpenRead();

        bool Exists { get; }
    }

    public class FileSystem : UnmanagedDispose, IFileSystem, IGameService
    {
        internal struct AssetFile : IReadOnlyFile
        {
            public int ID { get; }

            public string Name { get; }

            public readonly string Path;
            public readonly string RelativePath;

            public readonly DateTime LastModifiedUtc;

            public AssetFile(int id, string path, string relPath, DateTime lastMod)
            {
                ID = id;
                Path = path;
                RelativePath = relPath;
                LastModifiedUtc = lastMod;
                Name = System.IO.Path.GetFileNameWithoutExtension(path);
            }

            public bool Exists => File.Exists(Path);

            public Stream OpenRead() => File.OpenRead(Path);

            public override string ToString() => $"ID: {ID}, Name: {Name}, Path: {Path}";
        }

        internal class AssetTrackerGlob : IObserver<IReadOnlyFile>
        {
            private IDisposable _unsubscriber;
            private Glob _glob;
            private Action<IReadOnlyFile> _callback;

            public AssetTrackerGlob(Glob _globPattern, Action<IReadOnlyFile> callback)
            {
                _glob = _globPattern;
                _callback = callback;
            }

            public virtual void Subscribe(IObservable<IReadOnlyFile> provider)
            {
                if (provider != null)
                    _unsubscriber = provider.Subscribe(this);
            }

            public void OnCompleted()
            {
                _unsubscriber.Dispose();
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(IReadOnlyFile value)
            {
                _callback(value);
            }
        }

        internal class AssetWatcher : UnmanagedDispose, IObservable<IReadOnlyFile>
        {
            private ILogger _logger;
            private List<IObserver<IReadOnlyFile>> _observers = new List<IObserver<IReadOnlyFile>>();
            private Dictionary<int, AssetFile> _files = new Dictionary<int, AssetFile>();
            //private ConcurrentBag<AssetFile> _filesChanged = new ConcurrentBag<AssetFile>();
            private string _path;
            private Task _fileCheck;
            private bool _isRunning = false;

            public AssetWatcher(ILoggerFactory logFactory, string path)
            {
                _logger = logFactory.CreateLogger<AssetWatcher>();
                _path = path;
                if (!Path.IsPathRooted(path))
                    _path = Path.Combine(System.Environment.CurrentDirectory, _path);
            }

            private void TaskCheck()
            {
                System.Threading.Thread.CurrentThread.Name = "File Asset Tracker";
                while (_isRunning)
                {
                    foreach (var it in Files)
                    {
                        if (!_files.TryGetValue(it.ID, out var file))
                            _files.Add(it.ID, it);

                        if (it.LastModifiedUtc != file.LastModifiedUtc)
                        {
                            _files[it.ID] = file;
                            _logger.LogDebug($"File changed [{it.RelativePath}]");
                            foreach (var obs in _observers)
                                obs.OnNext(file);
                            //_filesChanged.Add(file);
                        }
                    }

                    System.Threading.Thread.Sleep(100);
                }
            }

            public void Start()
            {
                Contract.EqualTo(_isRunning, false);

                foreach (var it in Files)
                    _files.Add(it.ID, it);

                _isRunning = true;
                _fileCheck = Task.Run(TaskCheck);
            }

            public void Stop()
            {
                Contract.EqualTo(_isRunning, true);
                _isRunning = false;
                _fileCheck.Wait();
            }

            public IEnumerable<AssetFile> Files
            {
                get
                {
                    var dirs = new Stack<string>();
                    dirs.Push(_path);

                    while (dirs.Count > 0)
                    {
                        var dir = dirs.Pop();
                        var files = Directory.GetFiles(dir);
                        foreach (var it in Directory.GetDirectories(dir))
                            dirs.Push(it);

                        foreach (var filePath in files)
                        {
                            var relFile = Path.GetRelativePath(_path, filePath);
                            var hash = Atma.HashCode.Hash(relFile);
                            var fi = new FileInfo(filePath);
                            yield return new AssetFile(hash, filePath, relFile, fi.LastWriteTimeUtc);
                        }
                    }
                }
            }

            protected override void OnManagedDispose()
            {
                _isRunning = false;
                _fileCheck.Wait();
            }

            public IDisposable Subscribe(IObserver<IReadOnlyFile> observer)
            {
                if (!_observers.Contains(observer))
                    _observers.Add(observer);
                return new Unsubscriber(_observers, observer);
            }

            private class Unsubscriber : IDisposable
            {
                private List<IObserver<IReadOnlyFile>> _observers;
                private IObserver<IReadOnlyFile> _observer;

                public Unsubscriber(List<IObserver<IReadOnlyFile>> observers, IObserver<IReadOnlyFile> observer)
                {
                    this._observers = observers;
                    this._observer = observer;
                }

                public void Dispose()
                {
                    if (_observer != null && _observers.Contains(_observer))
                        _observers.Remove(_observer);
                }
            }
        }

        private AssetWatcher _assetWatcher;
        public IObservable<IReadOnlyFile> AssetTracker => _assetWatcher;

        public FileSystem(ILoggerFactory logFactory)
        {
            _assetWatcher = new AssetWatcher(logFactory, "Assets");
        }

        public IEnumerable<IReadOnlyFile> FindAssets(Glob pattern)
        {
            foreach (var it in _assetWatcher.Files)
                if (pattern.IsMatch(it.Path))
                    yield return it;
        }

        public IObserver<IReadOnlyFile> Observe(Glob pattern, Action<IReadOnlyFile> callback)
        {
            return new AssetTrackerGlob(pattern, callback);
        }

        public void Initialize()
        {
            //find all files
            _assetWatcher.Start();
        }

        public void Tick(float dt)
        {
        }

        protected override void OnManagedDispose()
        {
            _assetWatcher.Stop();
        }
    }
}