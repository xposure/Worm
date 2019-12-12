namespace Game.Logic.Modules.Physics
{
    using Atma.Systems;

    public class Physics : SystemProducer
    {

        [Has(typeof(Sprite))]
        public void Execute(ref Position position)
        {
            //position.X += 0.1f;
            return;
        }

    }
}