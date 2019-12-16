namespace Game.Framework.Managers
{

    [AutoRegister(true)]
    public interface IDrawContextFactory
    {
        DrawContext CreateDrawContext();
    }


    [AutoRegister(true)]
    public interface IRenderCommandFactory
    {
        IRenderCommandBuffer Create();
    }

}