namespace Game.Framework
{

    public interface ISystemTick
    {
        void Tick(float dt);
    }

    public interface ISystemInit
    {
        void Initialize();
    }

    public interface ISystemStartup
    {
        void Start(bool isReload = false);
    }
}