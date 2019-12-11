namespace Worm.Managers
{
    using System.Collections.Generic;
    using Atma;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Atma.Memory;
    using HashCode = Atma.HashCode;
    using System.Linq;

    public readonly struct SpriteKeyFrame : IEquatable<SpriteKeyFrame>
    {
        public readonly TextureRegion Region;

        public bool Equals(SpriteKeyFrame other) => Region.X0 == other.Region.X0 && Region.Y0 == other.Region.Y0 && Region.X1 == other.Region.X1 && Region.Y1 == other.Region.Y1;

        public unsafe override int GetHashCode()
        {
            var hasher = stackalloc HashCode[1];
            hasher->Add(Region.X0);
            hasher->Add(Region.Y0);
            hasher->Add(Region.X1);
            hasher->Add(Region.Y1);
            return hasher->ToHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return Equals((SpriteKeyFrame)obj);
        }

        public SpriteKeyFrame(float uvx0, float uvy0, float uvx1, float uvy1)
        {
            Region = new TextureRegion(uvx0, uvy0, uvx1, uvy1);
        }

        public SpriteKeyFrame(in TextureRegion region)
        {
            Region = region;
        }

        public static SpriteKeyFrame FromSize(float uvx, float uvy, float width, float height) => new SpriteKeyFrame(uvx, uvy, uvx + width, uvy + height);

        public static bool operator ==(SpriteKeyFrame a, SpriteKeyFrame b) => a.Equals(b);
        public static bool operator !=(SpriteKeyFrame a, SpriteKeyFrame b) => !a.Equals(b);
    }

    public class Animation : IEquatable<Animation>
    {
        public readonly uint AnimationID;
        public readonly SpriteKeyFrame[] Frames;
        public Animation(uint animationID, SpriteKeyFrame[] frames)
        {
            AnimationID = animationID;
            Frames = frames;
        }

        public bool Equals([AllowNull] Animation other)
        {
            if (AnimationID != other.AnimationID)
                return false;

            if (Frames.Length != other.Frames.Length)
                return false;

            for (var i = 0; i < Frames.Length; i++)
                if (Frames[i] != other.Frames[i])
                    return false;

            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            return Equals((Animation)obj);
        }

        public unsafe override int GetHashCode()
        {
            var hasher = stackalloc HashCode[1];
            hasher->Add(AnimationID);
            for (var i = 0; i < Frames.Length; i++)
                hasher->Add(Frames[i].GetHashCode());

            return hasher->ToHashCode();
        }

        public static bool operator ==(Animation a, Animation b) => a.Equals(b);
        public static bool operator !=(Animation a, Animation b) => !a.Equals(b);
    }

    public static class Animations
    {
        private static uint _animationId = 0;
        public static Animation None;
        public static Animation Player;

        private static Dictionary<uint, Animation> _animations = new Dictionary<uint, Animation>();

        public static void Init()
        {
            None = AddAnimation(new SpriteKeyFrame(0, 0, 1, 1));
            Player = Animations.CreateAnimation(Sprites.Player, 16, 24, Enumerable.Range(0, 4).Select(x => new Point(x, 2)).ToArray());
        }

        public static Animation AddAnimation(params SpriteKeyFrame[] frames)
        {
            var animation = new Animation(_animationId++, frames);
            _animations.Add(animation.AnimationID, animation);
            return animation;
        }

        public static Animation GetAnimation(uint id) => _animations[id];

        public unsafe static Animation CreateAnimation(SpriteTexture texture, int tileWidth, int tileHeight, params Point[] tiles)
        {
            var tilesX = texture.GpuTexture.Width / tileWidth;
            var tilesY = texture.GpuTexture.Height / tileHeight;

            Assert.EqualTo(tilesX * tileWidth, texture.GpuTexture.Width);
            Assert.EqualTo(tilesY * tileHeight, texture.GpuTexture.Height);

            var tileW = 1f / tilesX;
            var tileH = 1f / tilesY;

            var frames = new SpriteKeyFrame[tiles.Length];
            for (var i = 0; i < tiles.Length; i++)
                frames[i] = SpriteKeyFrame.FromSize(tiles[i].X * tileW, tiles[i].Y * tileW, tileW, tileH);

            return AddAnimation(frames);
        }

        public static void Dispose()
        {
            _animations.Clear();
        }
    }
}