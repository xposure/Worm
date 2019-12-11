namespace Worm.Managers
{
    using System.Collections.Generic;
    using Atma;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Atma.Memory;
    using HashCode = Atma.HashCode;
    using System.Linq;

    public class MemoryManager
    {
        public IAllocator allocator;

        private StackAllocator _temp;
        private HeapAllocator _persistent;
        private HeapAllocator _short;

    }

}