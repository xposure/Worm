using System;
using Atma;
using Atma.Entities;
using Microsoft.Xna.Framework;
using Worm;

public struct ColorLerp
{
    public Color Target;
    public float Speed;
}

public class ColorLerpSystem : UnmanagedDispose, ISystem
{
    public void Init()
    {
    }

    public void Update(float dt)
    {
        Engine.Instance.Entities.ForEntity((uint entities, ref Color color, ref ColorLerp colorLerp) =>
        {
            color = Color.Lerp(color, colorLerp.Target, colorLerp.Speed);
        });
    }

    public void Draw(float dt)
    {
    }

}