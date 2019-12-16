namespace Game.Framework.Managers
{

    [GameService]
    public interface IDrawContextFactory
    {
        DrawContext CreateDrawContext();
    }


    [GameService]
    public interface IRenderCommandFactory
    {
        IRenderCommandBuffer Create();
    }

}