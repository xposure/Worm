namespace Game.Logic.Modules.Physics
{
    using Atma.Systems;

    public class Physics : SystemProducer
    {

        private float sin = 0f;


        [Has(typeof(Sprite))]
        public void Execute(in Position position, ref Scale scale)
        {
            var t = (sin % 2) - 1;
            //scale.Width = t * t * 2 + 1;
            //scale.Height = t * t * t * 2 + 1;


            //position.X -= 0.1f;
            return;
        }

        [Has(typeof(Sprite))]
        public void Execute(ref Position position)
        {
            sin += 0.016f;

            //position.X = (float)System.Math.Cos(sin) * 100;
            //position.Y = (float)System.Math.Sin(sin) * 100;


            //position.X -= 0.1f;
            return;
        }

    }
}