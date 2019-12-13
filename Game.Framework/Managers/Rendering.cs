namespace Game.Framework.Managers
{
    using System;
    using System.Runtime.InteropServices;
    using Atma.Math;

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct SpriteVertex
    {
        [VertexElement(VertexElementType.Float2, VertexSemantic.Position)]
        public Position Position;

        [VertexElement(VertexElementType.Color, VertexSemantic.Color)]
        public uint Color;

        [VertexElement(VertexElementType.Float2, VertexSemantic.Texture)]
        public TextureCoord TextureCoord;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    [VertexGroup(typeof(SpriteVertex))]
    public struct GpuSprite
    {
        public SpriteVertex TL;
        public SpriteVertex TR;
        public SpriteVertex BL;
        public SpriteVertex BR;

    }

    public enum RenderCameraType
    {
        World,
        Projection,
        View
    }



    public interface IRenderCommandBuffer : IDisposable
    {

        int Triangles { get; }
        int Commands { get; }

        void SetTexture(ITexture texture);
        void SetCamera(RenderCameraType type, in float4x4 matrix);

        void RenderPrimitives(int startIndex, int primitiveCount);

        void UpdateProjection();

        void ResetState();
        void Render();
        void SetIndexBuffer(IIndexBuffer buffer);
        void SetVertexBuffer(IVertexBuffer buffer);
    }

}