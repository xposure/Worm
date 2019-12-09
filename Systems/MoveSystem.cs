namespace Worm.Systems
{
    //https://mattmakesgames.tumblr.com/post/127890619821/towerfall-physics
    public class MoveSystem : ISystem
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
            em.ForEntity((uint entity, ref Input input, ref Position position) =>
            {
                // em.ForEntity((uint solid, ref Tile tile, ) =>
                // {

                // });
            });
        }
    }
}