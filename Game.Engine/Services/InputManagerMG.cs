namespace Game.Engine.Services
{
    using Atma.Math;
    using Game.Framework;
    using Game.Framework.Services;
    using Input = Microsoft.Xna.Framework.Input;

    public class InputManager : IInputManager, IGameService
    {

        //private Input.MouseState _mouseState;

        private bool _mouseLeft = false;
        private bool _mouseRight = false;

        public int2 MousePosition { get; private set; }

        public void Initialize()
        {
        }

        public void Tick(float dt)
        {
            var mouse = Input.Mouse.GetState();
            MousePosition = new int2(mouse.X, mouse.Y);
            _mouseLeft = mouse.LeftButton == Input.ButtonState.Pressed;
            _mouseRight = mouse.RightButton == Input.ButtonState.Pressed;
        }

        public bool IsMouseDown(MouseButton button)
        {
            switch (button)
            {
                case MouseButton.Left: return _mouseLeft;
                case MouseButton.Right: return _mouseRight;
            }

            return false;
        }
    }
}