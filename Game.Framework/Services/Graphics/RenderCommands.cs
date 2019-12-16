namespace Game.Framework.Services.Graphics
{
    using System;
    using Atma.Math;

    public enum RenderCameraType
    {
        World,
        Projection,
        View
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
    }
}