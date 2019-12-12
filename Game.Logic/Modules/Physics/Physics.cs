namespace Game.Logic.Modules.Physics
{
    using Atma.Systems;

    public class Physics : SystemEntityProcessor
    {

        public void Execute(ref Position position)
        {
            position.X += 1;
            return;
        }

    }
}