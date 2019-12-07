namespace Worm.Managers
{
    using System.Collections.Generic;
    using Atma;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Atma.Memory;
    using System.IO;

    public class SpriteTexture : UnmanagedDispose, IEquatable<SpriteTexture>
    {
        public readonly uint TextureID;
        public readonly Texture2D GpuTexture;

        public readonly NativeArray<SpriteKeyFrame> KeyFrames;


        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            return Equals(obj as SpriteTexture);
        }

        public override int GetHashCode() => TextureID.GetHashCode();

        public bool Equals([AllowNull] SpriteTexture other) => TextureID == other.TextureID;

        public SpriteTexture(uint textureId, Texture2D texture, NativeArray<SpriteKeyFrame> keyFrames)
        {
            TextureID = textureId;
            GpuTexture = texture;
            KeyFrames = keyFrames;
        }

        public SpriteTexture(uint textureId, Texture2D texture)
        {
            TextureID = textureId;
            GpuTexture = texture;
            KeyFrames = NativeArray<SpriteKeyFrame>.Empty;
        }

        public static implicit operator Texture2D(SpriteTexture it) => it.GpuTexture;
        public static implicit operator uint(SpriteTexture it) => it.TextureID;

        protected override void OnUnmanagedDispose()
        {
            KeyFrames.Dispose();
            GpuTexture.Dispose();
        }
    }

    public static class Sprites
    {
        public static SpriteTexture Missing, Circle, Square;

        public static SpriteTexture Player;

        private static uint _textureId = 0;

        private static Dictionary<uint, SpriteTexture> _textures = new Dictionary<uint, SpriteTexture>();

        public static SpriteTexture GetTexture(uint textureId)
        {
            if (_textures.TryGetValue(textureId, out var texture))
                return texture;

            return Missing;
        }

        public static void Init()
        {
            Missing = InitMissingTexture();
            Circle = InitCircleTexture();
            Square = InitSquareTexture();
            Player = AddTexture(@"p:\Games\Worm\sprite.png");
        }

        public static void Dispose()
        {
            foreach (var texture in _textures.Values)
                texture.Dispose();

            _textures.Clear();
        }

        public static SpriteTexture AddTexture(string file)
        {
            using (var fs = File.OpenRead(file))
                return AddTexture(Texture2D.FromStream(Engine.Instance.GraphicsDevice, fs));
        }

        public static SpriteTexture AddTexture(Texture2D texture)
        {
            var spriteTexture = new SpriteTexture(_textureId++, texture);
            _textures.Add(spriteTexture.TextureID, spriteTexture);
            return spriteTexture;
        }

        private static SpriteTexture InitMissingTexture()
        {
            var texture = new Texture2D(Engine.Instance.GraphicsDevice, 1, 1);
            texture.SetData(new Color[] { Color.Magenta });
            return AddTexture(texture);
        }

        private static SpriteTexture InitCircleTexture()
        {
            var r = 8;
            var rsq = r * r;
            var texture = new Texture2D(Engine.Instance.GraphicsDevice, r * 2, r * 2);
            var textureData = new Color[texture.Width * texture.Height];
            for (var y = 0; y < texture.Height; y++)
            {
                for (var x = 0; x < texture.Width; x++)
                {
                    var xr = x - r;
                    var yr = y - r;

                    var psq = xr * xr + yr * yr;

                    var idx = texture.Width * y + x;
                    textureData[idx] = Color.White;
                    var d = 1f - Math.Clamp((float)psq / rsq, 0, 1);
                    var p = (byte)(d * 255);
                    textureData[idx] = new Color(p, p, p, (byte)64);
                }
            }
            texture.SetData(textureData);
            return AddTexture(texture);
        }

        private static SpriteTexture InitSquareTexture()
        {
            var r = 8;
            var texture = new Texture2D(Engine.Instance.GraphicsDevice, r * 2, r * 2);
            var textureData = new Color[texture.Width * texture.Height];
            for (var y = 0; y < texture.Height; y++)
            {
                for (var x = 0; x < texture.Width; x++)
                {
                    var xr = x - r;
                    var yr = y - r;
                    var t = Math.Max(xr, yr);

                    var idx = texture.Width * y + x;
                    textureData[idx] = Color.White;
                    var d = 1f - Math.Clamp((float)t / t, 0, 1);
                    var p = (byte)(d * 255);
                    textureData[idx] = new Color(p, p, p, (byte)64);
                }
            }
            texture.SetData(textureData);
            return AddTexture(texture);
        }
    }
}