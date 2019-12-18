namespace Game.Framework.Services
{
    using Atma;
    using Atma.Common;
    using GlobExpressions;
    using System;
    using System.Collections.Generic;
    using System.IO;

    [GameService]
    public interface IFileSystem : IDisposable
    {
        IReadOnlyFile FindAsset(string asset);
        IObservable<IReadOnlyFile> AssetTracker { get; }

    }

    public interface IReadOnlyFile
    {
        int ID { get; }
        string Name { get; }

        Stream OpenRead();

        bool Exists { get; }


    }

    public interface IFile : IReadOnlyFile
    {

    }

    public interface IAssetTrackerFactory
    {

    }

    public class AssetTrackerGlob : IObserver<IReadOnlyFile>
    {
        private IDisposable unsubscriber;
        private Glob _glob;

        public AssetTrackerGlob(Glob _globPattern)
        {
            _glob = _globPattern;
        }

        public virtual void Subscribe(IObservable<IReadOnlyFile> provider)
        {
            if (provider != null)
                unsubscriber = provider.Subscribe(this);
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(IReadOnlyFile value)
        {
            throw new NotImplementedException();
        }
    }


    public class FileSystemBase : UnmanagedDispose, IFileSystem
    {
        internal class FileTracker : IObservable<IReadOnlyFile>
        {
            private List<IObserver<IReadOnlyFile>> observers = new List<IObserver<IReadOnlyFile>>();
            public IDisposable Subscribe(IObserver<IReadOnlyFile> observer)
            {
                if (!observers.Contains(observer))
                    observers.Add(observer);
                return new Unsubscriber(observers, observer);
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

        internal class ReadOnlyFile : IReadOnlyFile
        {
            private FileSystemBase _fileSystem;
            public int ID { get; private set; }
            public string Name { get; private set; }

            internal DateTime LastModifiedUtc { get; private set; }

            public ReadOnlyFile(FileSystemBase fileSystem, string name)
            {
                _fileSystem = fileSystem;
                Name = name;
                ID = Atma.HashCode.Hash(name);
            }

            public Stream OpenRead() => File.OpenRead(Name);

            public bool Exists => File.Exists(Name);
        }

        private IObservable<IReadOnlyFile> _assetTracker = new FileTracker();
        public IObservable<IReadOnlyFile> AssetTracker => _assetTracker;

        public IReadOnlyFile FindAsset(string asset) => new ReadOnlyFile(this, asset);

        //public virtual void 


    }
}