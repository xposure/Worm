namespace Game.Framework
{
    using System;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = true)]
    public class AutoRegisterAttribute : Attribute
    {
        public readonly bool Singleton;
        public AutoRegisterAttribute(bool singleton)
        {
            Singleton = singleton;
        }

    }
}