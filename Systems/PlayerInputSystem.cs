namespace Worm.Systems
{
    public class PlayerInputSystem : ISystem
    {
        public void Dispose()
        {
        }

        public void Draw(float dt)
        {
        }

        public void Init()
        {
        }

        public void Update(float dt)
        {
            var em = Engine.Instance.Entities;
            em.ForEntity((uint entity, ref Position position, ref PlayerInput playerInput) =>
            {
                playerInput.LastMouse = playerInput.Mouse;
                playerInput.Mouse = Microsoft.Xna.Framework.Input.Mouse.GetState();

                playerInput.LastKeyboard = playerInput.Keyboard;
                playerInput.Keyboard = Microsoft.Xna.Framework.Input.Keyboard.GetState();

                var keyboard = playerInput.Keyboard;
                if (keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.A))
                    position.X -= playerInput.Speed * dt;
                else if (keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D))
                    position.X += playerInput.Speed * dt;

            });
        }
    }
}