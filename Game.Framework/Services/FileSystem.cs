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
    using System.Collections;

    [Flags]
    public enum FileType
    {
        UNKNOWN = 0,
        IMAGE = 1,
        JSON = 2,
    }

    public static class FileUtils
    {
        private static readonly KeyValuePair<string[], FileType>[] FileTypes = new[]
        {
            new KeyValuePair<string[], FileType>(new []{"png", "jpg", "jpeg"}, FileType.IMAGE),
            new KeyValuePair<string[], FileType>(new []{"json"}, FileType.JSON)
        };

        public unsafe static FileType GetFileType(ReadOnlySpan<char> path)
        {
            var ext = GetExtension(path);
            Span<char> lower = stackalloc char[ext.Length];
            for (var i = 0; i < ext.Length; i++)
            {
                var ch = ext[i];
                if (char.IsLetter(ch) && char.IsUpper(ch))
                    ch = char.ToLower(ch);

                lower[i] = ch;
            }

            for (var i = 0; i < FileTypes.Length; i++)
            {
                var fileType = FileTypes[i];
                for (var k = 0; k < fileType.Key.Length; k++)
                    if (lower.SequenceEqual(fileType.Key[k]))
                        return fileType.Value;
            }

            return FileType.UNKNOWN;
        }

        public static ReadOnlySpan<char> GetExtension(ReadOnlySpan<char> path)
        {
            var lastIndex = path.LastIndexOf('.');
            if (lastIndex == -1) return ReadOnlySpan<char>.Empty;

            return path.Slice(lastIndex + 1, path.Length - lastIndex - 1);
        }
    }

    [GameService]
    public interface IFileSystem : IDisposable
    {
        IReadOnlyFileSource FindAssets(string globPattern, FileType? fileTypes = null);
    }

    public interface IReadOnlyFileSource : IEnumerable<IReadOnlyFile>
    {
        string Pattern { get; }

        IDisposable Observe(Action<IReadOnlyFile> callback);
    }


    public interface IReadOnlyFile
    {
        int ID { get; }
        string Name { get; }

        FileType FileType { get; }

        Stream OpenRead();

        bool Exists { get; }
    }

    public class FileSystem : UnmanagedDispose, IFileSystem, IGameService
    {
        internal class FileSource : IReadOnlyFileSource
        {
            public string Pattern { get; }

            private Glob _glob;
            private AssetWatcher _watcher;

            private FileType? _fileTypes;

            public FileSource(AssetWatcher watcher, Glob glob, FileType? fileTypes)
            {
                _watcher = watcher;
                _glob = glob;
                _fileTypes = fileTypes;

                Pattern = glob.Pattern;
            }

            private IEnumerable<IReadOnlyFile> Files
            {
                get
                {
                    foreach (var it in _watcher.GetFiles(_glob, _fileTypes))
                        yield return it;
                }
            }

            public IDisposable Observe(Action<IReadOnlyFile> callback) => new AssetTrackerGlob(_glob, callback);

            public IEnumerator<IReadOnlyFile> GetEnumerator()
            {
                foreach (var it in Files)
                    yield return it;
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                foreach (var it in Files)
                    yield return it;
            }
        }
        internal struct AssetFile : IReadOnlyFile
        {
            public int ID { get; }

            public string Name { get; }

            public FileType FileType { get; }

            public readonly string Path;
            public readonly string RelativePath;

            public readonly DateTime LastModifiedUtc;

            public AssetFile(int id, string path, string relPath, FileType fileType, DateTime lastMod)
            {
                ID = id;
                Path = path;
                RelativePath = relPath;
                LastModifiedUtc = lastMod;
                Name = System.IO.Path.GetFileNameWithoutExtension(path);
                FileType = fileType;
            }

            public bool Exists => File.Exists(Path);

            public Stream OpenRead() => File.OpenRead(Path);

            public override string ToString() => $"ID: {ID}, Name: {Name}, Path: {RelativePath}, LastMod: {LastModifiedUtc}";
        }

        internal class AssetTrackerGlob : UnmanagedDispose, IObserver<IReadOnlyFile>
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
                if (_glob.IsMatch(value.Name))
                    _callback(value);
            }

            protected override void OnManagedDispose() => _unsubscriber?.Dispose();
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
                    foreach (var it in GetFiles(new Glob("*"), null))
                    {
                        if (!_files.TryGetValue(it.ID, out var file))
                        {
                            _logger.LogDebug($"Found file [{it}].");
                            _files.Add(it.ID, it);
                        }

                        if (it.LastModifiedUtc != file.LastModifiedUtc)
                        {
                            _files[it.ID] = it;
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

                foreach (var it in GetFiles(new Glob("*"), null))
                {
                    _files.Add(it.ID, it);
                    _logger.LogDebug($"Found file [{it}].");
                }

                _isRunning = true;
                _fileCheck = Task.Run(TaskCheck);
            }

            public void Stop()
            {
                Contract.EqualTo(_isRunning, true);
                _isRunning = false;
                _fileCheck.Wait();
            }

            public IEnumerable<AssetFile> GetFiles(Glob pattern, FileType? fileTypes)
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
                        var fileType = FileUtils.GetFileType(filePath);
                        if (!fileTypes.HasValue || (fileType & fileTypes.Value) == fileType)
                        {
                            var relFile = Path.GetRelativePath(_path, filePath);
                            if (pattern.IsMatch(relFile))
                            {
                                var hash = Atma.HashCode.Hash(relFile);
                                var fi = new FileInfo(filePath);
                                yield return new AssetFile(hash, filePath, relFile, fileType, fi.LastWriteTimeUtc);
                            }
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
            _assetWatcher.Start();
        }

        public IReadOnlyFileSource FindAssets(string globPattern, FileType? fileTypes = null)
        {
            return new FileSource(_assetWatcher, new Glob(globPattern, GlobOptions.Compiled | GlobOptions.CaseInsensitive), fileTypes);
        }

        public void Initialize()
        {
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