using Atma.Math;
using Atma.Memory;
using Microsoft.Xna.Framework;

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
            MoveActors(dt);
        }

        public void MoveActors(float dt)
        {
            var em = Engine.Instance.Entities;
            // em.ForEntity((uint entity, ref Move move, ref Position position, ref Collider collider, ref Gravity gravity)
            //     => move.Speed.y += gravity.Force);

            em.ForEntity((uint entity, ref Move move, ref Position position, ref Collider collider)
                => MoveActor(dt, entity, ref move, ref position, ref collider));

        }

        // private readonly struct Collidable
        // {
        //     public readonly uint Entity;
        //     public readonly AxisAlignedBox2 AABB;

        //     public Collidable(uint entity, in Move move, in Position position, in Collider collider)
        //     {
        //         Entity = entity;
        //         Move = move;
        //         Position = position;
        //         Collider = collider;
        //         AABB = collider.Area;
        //         AABB.Center = new float2(position.X, position.Y);
        //     }
        // }

        private void MoveActor(float dt, uint entity, ref Move move, ref Position position, ref Collider collider)
        {
            var aabb = collider.Bounds;
            RenderingSystem.DebugDraw(aabb, Color.Green);
            var em = Engine.Instance.Entities;
            if (!collider.Disabled && !float2.ApproxEqual(move.Speed, float2.Zero))
            {
                using var entities = new NativeList<AxisAlignedBox2>(Engine.Instance.Memory);
                var broadaabb = collider.Bounds;
                broadaabb.Offset(position);
                RenderingSystem.DebugDraw(broadaabb, Color.Orange);

                // var amountX = move.SpeedX * dt;
                // var amountY = move.SpeedY * dt;

                var amount = move.Speed * dt;

                var targetaabb = broadaabb;
                targetaabb.Offset(amount);
                var targetPosition = position + amount;


                //targetaabb.Offset(targetPosition);
                broadaabb.Merge(targetaabb);
                RenderingSystem.DebugDraw(targetaabb, Color.Green);

                //gather all aabbs in our broadphase based on where the entity is moving to
                em.ForEntity((uint other, ref Position otherPosition, ref Solid solid) =>
                {
                    var solidArea = solid.Bounds;
                    solidArea.Offset(otherPosition);

                    if (solidArea.Intersects(broadaabb))
                        entities.Add(solidArea);
                });

                //nothing in our area to collide with
                if (entities.Length == 0)
                {
                    position = targetPosition;
                }
                else
                {
                    var dist = float2.Distance(targetPosition, position);
                    var step = amount / dist;
                    while (dist > 0)
                    {
                        MoveActorAxis(entities, 0, step.x, ref move, ref position, ref collider);
                        MoveActorAxis(entities, 1, step.y, ref move, ref position, ref collider);
                        dist -= 1;
                    }
                }
            }
        }

        private bool MoveActorAxis(NativeList<AxisAlignedBox2> broadsweep, int axis, float amount, ref Move move, ref Position position, ref Collider collider)
        {
            move.Remainder[axis] += amount;
            int m = (int)System.Math.Round(move.Remainder[axis]);
            if (m != 0)
            {
                move.Remainder[axis] -= m;
                int sign = System.Math.Sign(m);

                while (m != 0)
                {
                    var newPosition = position;
                    newPosition[axis] += sign;

                    var worldaabb = collider.Bounds;
                    worldaabb.Offset(newPosition);

                    if (worldaabb.Intersects(broadsweep.AsSpan(), out var index))
                    {
                        RenderingSystem.DebugDraw(broadsweep[index], Color.Red);
                        return false;
                    }

                    position[axis] += sign;
                    m -= sign;
                }
            }

            return true;
        }
    }
}