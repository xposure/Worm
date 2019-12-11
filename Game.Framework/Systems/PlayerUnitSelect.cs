using Microsoft.Xna.Framework;

namespace Worm.Systems
{
    public class PlayerUnitSelectSystem : ISystem
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
            em.ForEntity((uint entity, ref PlayerUnitSelect selected, ref PlayerInput playerInput, ref Position position) =>
            {
                var mouse = playerInput.Mouse;
                var mp = new Vector2(mouse.X, mouse.Y);
                var p = new Vector2(position.X, position.Y);
                selected.IsSelected = Vector2.DistanceSquared(mp, p) < 5;
            });
        }
    }
}