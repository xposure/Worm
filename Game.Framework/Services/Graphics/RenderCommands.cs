namespace Game.Framework.Services.Graphics
{
    using System;
    using Atma.Math;


    public static class RenderCommandExtensions
    {
        public static void SetBlendMode(this IRenderCommandBuffer it, BlendFunction blendRgba, Blend srcRgba, Blend dstRgba) => it.SetBlendMode(blendRgba, blendRgba, srcRgba, srcRgba, dstRgba, dstRgba);
        public static void SetAlphaBlend(this IRenderCommandBuffer it) => it.SetBlendMode(BlendFunction.Add, Blend.SourceAlpha, Blend.InverseSourceAlpha);
    }

    [GameService]
    public interface IRenderCommandFactory
    {
        IRenderCommandBuffer Create();
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
        void SetBlendMode(BlendFunction blendRgb, BlendFunction blendA, Blend srcRgb, Blend srcA, Blend dstRgb, Blend dstA);
    }
}