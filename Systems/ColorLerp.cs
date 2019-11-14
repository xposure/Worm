using System;
using Atma;
using Atma.Entities;
using Microsoft.Xna.Framework;

public struct ColorLerp
{
    public Color Target;
    public float Speed;
}

public class ColorLerpSystem : ISystem
{
    public void Init()
    {
    }

    public void Update(float dt, EntityManager entityManager)
    {
        entityManager.ForEntity((uint entities, ref Color color, ref ColorLerp colorLerp) =>
        {
            color = Color.Lerp(color, colorLerp.Target, colorLerp.Speed);
        });
    }

    public void Draw(float dt, EntityManager entityManager)
    {
    }

}