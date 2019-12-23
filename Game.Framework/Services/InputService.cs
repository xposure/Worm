using Atma.Math;

namespace Game.Framework.Services
{

    public enum MouseButton
    {
        Left,
        Right
    }


    [GameService]
    public interface IInputManager
    {
        int2 MousePosition { get; }
        bool IsMouseDown(MouseButton button);

    }
}