namespace Worm
{
    using System;
    using System.Collections.Generic;
    using Atma;
    using Atma.Entities;
    using Atma.Memory;
    using Atma.Systems;
    using SimpleInjector;

    public class GameExecutionEngine : UnmanagedDispose
    {
        private Container container;
        private IAllocator _memory;
        private EntityManager _entities;

        public IEnumerable<Type> SystemTypes { get; }

        public Container Container { get; }

        public bool IsModified => false;

        public GameExecutionEngine(IAllocator memory, EntityManager entityManager, string plugin)
        {

        }

        public IEnumerable<ISystem> Systems => container.GetAllInstances<ISystem>();

        protected override void OnManagedDispose()
        {
            container.Dispose();
        }
    }
}