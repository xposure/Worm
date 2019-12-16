namespace Game.Framework
{
    using System;
    using Atma;

    public interface IGameService : IDisposable
    {
        //Type Interface { get; }
        void Initialize();

        void Tick();

    }

    public abstract class GameService<T> : UnmanagedDispose, IGameService
    {
        //public Type Interface => typeof(T);

        public void Initialize() => OnInitialize();

        public void Tick() => OnTick();

        protected virtual void OnInitialize() { }

        protected virtual void OnTick() { }
    }
}