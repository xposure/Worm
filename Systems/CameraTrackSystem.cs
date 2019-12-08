namespace Worm.Systems
{
    using Atma;

    public class CameraTrackSystem : ISystem
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

            em.ForEntity((uint entity, ref Camera camera, ref Position position) =>
            {
                if (camera.EntityTrack != 0)
                {
                    var target = em.Get<Position>(camera.EntityTrack);
                    var diffX = position.X - target.X;
                    var diffY = position.Y - target.Y;

                    position.X -= diffX * dt * camera.TrackSpeed;
                    position.Y -= diffY * dt * camera.TrackSpeed;
                }
            });
        }
    }
}