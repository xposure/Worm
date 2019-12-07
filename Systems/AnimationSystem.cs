namespace Worm.Systems
{
    using Atma;
    using Worm.Graphics;
    using Worm.Managers;

    public class AnimationSystem : UnmanagedDispose, ISystem
    {

        public void Init()
        {
        }

        public void Update(float dt)
        {
            var em = Engine.Instance.Entities;

            em.ForEntity((uint entity, ref SpriteAnimation spriteAnimation, ref TextureRegion region) =>
            {
                if (spriteAnimation.Enabled)
                {
                    spriteAnimation.Timer += dt;
                    if (spriteAnimation.Timer > spriteAnimation.FPS)
                    {
                        spriteAnimation.Timer -= spriteAnimation.FPS;
                        var animation = Animations.GetAnimation(spriteAnimation.AnimationID);
                        spriteAnimation.AnimationIndex = (spriteAnimation.AnimationIndex + 1) % animation.Frames.Length;
                        ref var frame = ref animation.Frames[spriteAnimation.AnimationIndex];
                        region.X0 = frame.Region.X0;
                        region.Y0 = frame.Region.Y0;
                        region.X1 = frame.Region.X1;
                        region.Y1 = frame.Region.Y1;
                    }
                }
            });
        }

        public void Draw(float dt)
        {

        }


    }
}