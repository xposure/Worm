namespace Worm.Features.Dummy
{
    using Atma.Systems;
    using Microsoft.Extensions.Logging;

    public class DummySystem : SystemEntityProcessor
    {
        public void Execute(ref Position position, in Velocity velocity)
        {
            position.X += velocity.X;
            position.Y += velocity.Y;
        }

    }
}