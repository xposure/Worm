namespace Game.Framework.Services.Graphics
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Atma;
    using Atma.Common;
    using Atma.Math;
    using Atma.Memory;


    // A single vertex (20 bytes by default, override layout with IMGUI_OVERRIDE_DRAWVERT_STRUCT_LAYOUT)
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct ImDrawVert
    {
        [VertexElement(VertexElementType.Float2, VertexSemantic.Position)]
        public float2 pos;

        [VertexElement(VertexElementType.Float2, VertexSemantic.Texture)]
        public float2 uv;

        [VertexElement(VertexElementType.Color, VertexSemantic.Color)]
        public uint col;

        public override string ToString() => $"pos: {pos}, uv: {uv}, col: {col}";
    }


    [GameService]
    public interface IGeometryContextFactory
    {
        GeometryContext CreateGeometryContext();
    }

    public class GeometryContextFactory : IGeometryContextFactory
    {
        private readonly IAllocator _allocator;
        private readonly ITextureFactory _textures;
        private readonly IGraphicsBufferFactory _bufferFactory;
        private readonly IRenderCommandFactory _renderCommandFactory;
        public GeometryContextFactory(IAllocator allocator, ITextureFactory textures, IGraphicsBufferFactory bufferFactory, IRenderCommandFactory renderCommandFactory)
        {
            _allocator = allocator;
            _textures = textures;
            _bufferFactory = bufferFactory;
            _renderCommandFactory = renderCommandFactory;
        }

        public GeometryContext CreateGeometryContext() => new GeometryContext(_allocator, _textures, _bufferFactory, _renderCommandFactory.Create());
    }

    public readonly ref struct GeometrySegment
    {
        public readonly Span<ImDrawVert> Vertices;
        public readonly Span<ushort> Indices;

        public GeometrySegment(Span<ImDrawVert> vertices, Span<ushort> indices)
        {
            Vertices = vertices;
            Indices = indices;
        }
    }

    public class GeometryContext : UnmanagedDispose
    {
        public float2 FontTexUvWhitePixel => new float2(0.5f);
        public bool AntiAliasedLines => true;
        public bool AntiAliasedShapes => true;
        public int CurveTessellationTol = 10;


        private const int MAX_INDICIES = 65532;
        private const int MAX_PRIMITIVES = MAX_INDICIES / 3;

        private ITextureFactory _textures;
        private IGraphicsBufferFactory _bufferFactory;

        private IRenderCommandBuffer _renderCommands;
        private ITexture2D _defaultTexture;
        private ITexture2D _currentTexture = null;
        private IIndexBuffer16 _indexBuffer;

        private int _primitiveCount = 0;
        //private int _vertexPosition = 0;
        private ImDrawVert[] _vertices;
        private ushort[] _indices;
        private int _vertexPosition = 0;
        private int _indexPosition = 0;

        private IVertexBuffer<ImDrawVert> _vertexBuffer;
        private ObjectPool<IVertexBuffer<ImDrawVert>> _vertexBufferPool;
        private ObjectPool<IIndexBuffer16> _indexBufferPool;
        private List<IVertexBuffer<ImDrawVert>> _usedVertexBuffers0 = new List<IVertexBuffer<ImDrawVert>>();
        private List<IVertexBuffer<ImDrawVert>> _usedVertexBuffers1 = new List<IVertexBuffer<ImDrawVert>>();
        private List<IIndexBuffer16> _usedIndexBuffers0 = new List<IIndexBuffer16>();
        private List<IIndexBuffer16> _usedIndexBuffers1 = new List<IIndexBuffer16>();
        private List<IGraphicsBuffer> _allBuffers = new List<IGraphicsBuffer>();

        private NativeList<float2> _Path;

        public int Triangles => _vertexPosition * 3;
        public int Commands => _renderCommands.Commands;

        public GeometryContext(IAllocator allocator, ITextureFactory textures, IGraphicsBufferFactory bufferFactory, IRenderCommandBuffer renderCommands)
        {
            _Path = new NativeList<float2>(allocator);
            _textures = textures;
            _bufferFactory = bufferFactory;
            _renderCommands = renderCommands;

            _vertexBufferPool = new ObjectPool<IVertexBuffer<ImDrawVert>>(() =>
            {
                var buffer = _bufferFactory.CreateVertex<ImDrawVert>(MAX_PRIMITIVES * 4, true);
                _allBuffers.Add(buffer);
                return buffer;
            });
            _vertices = new ImDrawVert[MAX_PRIMITIVES];

            _indexBufferPool = new ObjectPool<IIndexBuffer16>(() =>
            {
                var buffer = _bufferFactory.CreateIndex16(MAX_INDICIES, true);
                _allBuffers.Add(buffer);
                return buffer;
            });
            _indices = new ushort[MAX_INDICIES];

            _defaultTexture = _textures["default"];//  new Texture2D(_device, 1, 1);

            Reset();
        }


        public void SetBlendMode(BlendFunction blendRgba, Blend srcRgba, Blend dstRgba) => _renderCommands.SetBlendMode(blendRgba, blendRgba, srcRgba, srcRgba, dstRgba, dstRgba);
        public void SetAlphaBlend() => _renderCommands.SetBlendMode(BlendFunction.Add, Blend.SourceAlpha, Blend.InverseSourceAlpha);

        public void SetTexture(ITexture2D texture)
        {
            if (_currentTexture != texture)
            {
                FlushRender();
                _renderCommands.SetTexture(texture);
                _currentTexture = texture;
            }
        }

        // public void SetBlendState(BlendState blendState)
        // {
        //     Assert.EqualTo(_isInBulkOperation, false);
        //     FlushRender();
        //     _renderCommands.SetBlendState(blendState);
        // }
        // public void SetDepthState(DepthStencilState depthState)
        // {
        //     Assert.EqualTo(_isInBulkOperation, false);
        //     FlushRender();
        //     _renderCommands.SetDepthState(depthState);
        // }
        // public void SetRasterizerState(RasterizerState rasitizerState)
        // {
        //     Assert.EqualTo(_isInBulkOperation, false);
        //     FlushRender();
        //     _renderCommands.SetRasterizerState(rasitizerState);
        // }
        internal static float InvLength(in float2 lhs, float fail_value)
        {
            float d = lhs.x * lhs.x + lhs.y * lhs.y;
            if (d > 0.0f)
                return 1.0f / glm.Sqrt(d);
            return fail_value;
        }

        public void SetCamera(in float4x4 matrix)
        {
            FlushRender();
            _renderCommands.SetCamera(RenderCameraType.World, matrix);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GeometrySegment TakePrimitives(int vertexCount, int indexCount, int primitiveCount, out int vertexPosition, out int indexPosition)
        {
            Assert.LessThan(indexCount / 3, MAX_PRIMITIVES);
            Assert.LessThan(indexCount, MAX_INDICIES);

            var remainingIndicies = MAX_INDICIES - _indexPosition;
            var remaniningPrimitives = remainingIndicies / 3;
            if (remaniningPrimitives < primitiveCount || remainingIndicies < indexCount)
                CompleteBuffer();

            var vertexSpan = _vertices.AsSpan();
            var indexSpan = _indices.AsSpan();

            vertexPosition = _vertexPosition;
            indexPosition = _indexPosition;

            _vertexPosition += vertexCount;
            _indexPosition += indexCount;
            _primitiveCount += primitiveCount;

            return new GeometrySegment(vertexSpan.Slice(vertexPosition, vertexCount), indexSpan.Slice(indexPosition, indexCount));
        }

        private void FlushRender()
        {
            if (_primitiveCount > 0)
            {
                var startIndex = _indexPosition - _primitiveCount * 3;
                _renderCommands.RenderPrimitives(startIndex, _primitiveCount);
                _primitiveCount = 0;
            }
        }
        private void CompleteBuffer(bool lastRender = false)
        {
            FlushRender();
            //Assert.GreatherThan(_vertexPosition, 0);

            if (_vertexPosition > 0 && _indexPosition > 0)
            {
                _vertexBuffer.SetData(_vertices, 0, _vertexPosition);//, 0, _vertexPosition, SetDataOptions.Discard);
                _vertexPosition = 0;

                _indexBuffer.SetData(_indices, 0, _indexPosition);
                _indexPosition = 0;
            }

            if (!lastRender)
                TakeNewBuffers();
        }

        public void Render()
        {
            _renderCommands.UpdateProjection();
            CompleteBuffer(true);
            _renderCommands.Render();
            Reset();
        }

        public void Reset()
        {
            //TODO: renderCommand should support updating effect params (like WVP)
            _renderCommands.ResetState();

            _currentTexture = _defaultTexture;
            _renderCommands.SetTexture(_currentTexture);

            ReturnUsedVertexBuffers();
            ReturnUsedIndexBuffers();
            TakeNewBuffers();
        }

        private void ReturnUsedVertexBuffers()
        {
            //we want to double buffer these buffers
            //i don't know if this is needed any more, but in the past
            //you could get gpu stalling when buffers were still being wrote to the gpu async
            var buffers = _usedVertexBuffers1;
            _usedVertexBuffers1 = _usedVertexBuffers0;
            _usedVertexBuffers0 = buffers;

            foreach (var it in buffers)
                _vertexBufferPool.Return(it);

            buffers.Clear();
        }

        private void ReturnUsedIndexBuffers()
        {
            //we want to double buffer these buffers
            //i don't know if this is needed any more, but in the past
            //you could get gpu stalling when buffers were still being wrote to the gpu async
            var buffers = _usedIndexBuffers1;
            _usedIndexBuffers1 = _usedIndexBuffers0;
            _usedIndexBuffers0 = buffers;

            foreach (var it in buffers)
                _indexBufferPool.Return(it);

            buffers.Clear();
        }

        private void TakeNewBuffers()
        {
            _primitiveCount = 0;

            _vertexPosition = 0;
            _vertexBuffer = _vertexBufferPool.Take();
            _usedVertexBuffers0.Add(_vertexBuffer);
            _renderCommands.SetVertexBuffer(_vertexBuffer);

            _indexPosition = 0;
            _indexBuffer = _indexBufferPool.Take();
            _usedIndexBuffers0.Add(_indexBuffer);
            _renderCommands.SetIndexBuffer(_indexBuffer);
        }

        protected override void OnUnmanagedDispose()
        {
            _vertexBuffer = null;
            _indexBuffer = null;
            _usedIndexBuffers0.Clear();
            _usedIndexBuffers1.Clear();
            _usedVertexBuffers0.Clear();
            _usedVertexBuffers1.Clear();
            _allBuffers.DisposeAll();
            _renderCommands.Dispose();
        }

        //    // Primitives
        internal void AddLine(float2 a, float2 b, uint col, float thickness = 1.0f)
        {
            if ((col >> 24) == 0)
                return;
            PathLineTo(a + new float2(0.5f, 0.5f));
            PathLineTo(b + new float2(0.5f, 0.5f));
            PathStroke(col, false, thickness);
        }

        // a: upper-left, b: lower-right
        internal void AddRect(float2 a, float2 b, uint col, float rounding = 0.0f, int rounding_corners = 0x0F, float thickness = 1.0f)
        {
            if ((col >> 24) == 0)
                return;
            PathRect(a + new float2(0.5f, 0.5f), b - new float2(0.5f, 0.5f), rounding, rounding_corners);
            PathStroke(col, true, thickness);
        }

        // a: upper-left, b: lower-right
        public void AddRectFilled(in float2 a, in float2 c, uint col, float rounding = 0.0f, int rounding_corners = 0x0F)
        {
            if ((col >> 24) == 0)
                return;
            if (rounding > 0.0f)
            {
                PathRect(a, c, rounding, rounding_corners);
                PathFill(col);
            }
            else
            {
                var b = new float2(c.x, a.y);
                var d = new float2(a.x, c.y);
                //var uv = new float2(-1, -1);
                var uv = FontTexUvWhitePixel;
                //float2 b(c.x, a.y), d(a.x, c.y), uv(GImGui->FontTexUvWhitePixel);
                var geometry = TakePrimitives(4, 6, 2, out var vertexPos, out var indexPos);
                ushort idx = (ushort)vertexPos;
                var VtxBuffer = geometry.Vertices;
                var IdxBuffer = geometry.Indices;

                IdxBuffer[0] = idx; IdxBuffer[1] = (ushort)(idx + 1); IdxBuffer[2] = (ushort)(idx + 2);
                IdxBuffer[3] = idx; IdxBuffer[4] = (ushort)(idx + 2); IdxBuffer[5] = (ushort)(idx + 3);

                VtxBuffer[0] = new ImDrawVert() { pos = a, uv = uv, col = col };
                VtxBuffer[1] = new ImDrawVert() { pos = b, uv = uv, col = col };
                VtxBuffer[2] = new ImDrawVert() { pos = c, uv = uv, col = col };
                VtxBuffer[3] = new ImDrawVert() { pos = d, uv = uv, col = col };
            }
        }

        internal void AddRectFilledMultiColor(float2 a, float2 c, uint col_upr_left, uint col_upr_right, uint col_bot_right, uint col_bot_left)
        {
            if (((col_upr_left | col_upr_right | col_bot_right | col_bot_left) >> 24) == 0)
                return;

            var b = new float2(c.x, a.y);
            var d = new float2(a.x, c.y);
            //var uv = new float2(-1, -1);
            var uv = FontTexUvWhitePixel;
            //float2 b(c.x, a.y), d(a.x, c.y), uv(GImGui->FontTexUvWhitePixel);
            var geometry = TakePrimitives(4, 6, 2, out var vertexPos, out var indexPos);
            ushort idx = (ushort)vertexPos;
            var VtxBuffer = geometry.Vertices;
            var IdxBuffer = geometry.Indices;

            IdxBuffer[0] = idx; IdxBuffer[1] = (ushort)(idx + 1); IdxBuffer[2] = (ushort)(idx + 2);
            IdxBuffer[3] = idx; IdxBuffer[4] = (ushort)(idx + 2); IdxBuffer[5] = (ushort)(idx + 3);

            VtxBuffer[0] = new ImDrawVert() { pos = a, uv = uv, col = col_upr_left };
            VtxBuffer[1] = new ImDrawVert() { pos = b, uv = uv, col = col_upr_right };
            VtxBuffer[2] = new ImDrawVert() { pos = c, uv = uv, col = col_bot_right };
            VtxBuffer[3] = new ImDrawVert() { pos = d, uv = uv, col = col_bot_left };
        }

        internal void AddTriangle(float2 a, float2 b, float2 c, uint col, float thickness = 1.0f)
        {
            if ((col >> 24) == 0)
                return;

            PathLineTo(a);
            PathLineTo(b);
            PathLineTo(c);
            PathStroke(col, true, thickness);
        }

        internal void AddTriangleFilled(float2 a, float2 b, float2 c, uint col)
        {
            if ((col >> 24) == 0)
                return;

            PathLineTo(a);
            PathLineTo(b);
            PathLineTo(c);
            PathFill(col);
        }

        internal void AddCircle(float2 centre, float radius, uint col, int num_segments = 12, float thickness = 1.0f)
        {
            if ((col >> 24) == 0)
                return;

            float a_max = glm.PI * 2.0f * (num_segments - 1.0f) / num_segments;
            PathArcTo(centre, radius - 0.5f, 0.0f, a_max, num_segments);
            PathStroke(col, true, thickness);
        }

        internal void AddCircleFilled(float2 centre, float radius, uint col, int num_segments = 12)
        {
            if ((col >> 24) == 0)
                return;

            float a_max = glm.PI * 2.0f * ((num_segments - 1.0f) / num_segments);
            PathArcTo(centre, radius, 0.0f, a_max, num_segments);
            PathFill(col);
        }

        public unsafe void AddConvexPolyFilled(Span<float2> points, uint col, bool anti_aliased)
        {
            var points_count = points.Length;
            float2 uv = FontTexUvWhitePixel;
            anti_aliased &= AntiAliasedShapes;
            //if (ImGui::GetIO().KeyCtrl) anti_aliased = false; // Debug

            if (anti_aliased)
            {
                // Anti-aliased Fill
                float AA_SIZE = 1.0f;
                uint col_trans = col & 0x00ffffff;
                int idx_count = (points_count - 2) * 3 + points_count * 6;
                int vtx_count = (points_count * 2);

                var _VtxWritePtr = 0;
                var _IdxWritePtr = 0;

                //var geometry = TakePrimitives(vtx_count, idx_count, idx_count / 3);
                var geometry = TakePrimitives(vtx_count, idx_count, idx_count / 3, out var vtx_inner_idx, out var indexPos);
                int vtx_outer_idx = vtx_inner_idx + 1;

                var VtxBuffer = geometry.Vertices;
                var IdxBuffer = geometry.Indices;

                // Add indexes for fill
                for (int i = 2; i < points_count; i++)
                {
                    IdxBuffer[_IdxWritePtr++] = (ushort)(vtx_inner_idx);
                    IdxBuffer[_IdxWritePtr++] = (ushort)(vtx_inner_idx + ((i - 1) << 1));
                    IdxBuffer[_IdxWritePtr++] = (ushort)(vtx_inner_idx + (i << 1));
                }

                // Compute normals
                Span<float2> temp_normals = stackalloc float2[points_count];
                //float2* temp_normals = (float2*)alloca(points_count * sizeof(float2));

                for (int i0 = points_count - 1, i1 = 0; i1 < points_count; i0 = i1++)
                {
                    float2 p0 = points[i0];
                    float2 p1 = points[i1];
                    float2 diff = p1 - p0;
                    diff *= InvLength(diff, 1.0f);
                    temp_normals[i0].x = diff.y;
                    temp_normals[i0].y = -diff.x;
                }

                for (int i0 = points_count - 1, i1 = 0; i1 < points_count; i0 = i1++)
                {
                    // Average normals
                    float2 n0 = temp_normals[i0];
                    float2 n1 = temp_normals[i1];
                    float2 dm = (n0 + n1) * 0.5f;
                    float dmr2 = dm.x * dm.x + dm.y * dm.y;
                    if (dmr2 > 0.000001f)
                    {
                        float scale = 1.0f / dmr2;
                        if (scale > 100.0f) scale = 100.0f;
                        dm *= scale;
                    }
                    dm *= AA_SIZE * 0.5f;

                    // Add vertices
                    VtxBuffer[_VtxWritePtr++] = new ImDrawVert() { pos = (points[i1] - dm), uv = uv, col = col };
                    VtxBuffer[_VtxWritePtr++] = new ImDrawVert() { pos = (points[i1] + dm), uv = uv, col = col_trans };

                    // Add indexes for fringes
                    IdxBuffer[_IdxWritePtr++] = (ushort)(vtx_inner_idx + (i1 << 1));
                    IdxBuffer[_IdxWritePtr++] = (ushort)(vtx_inner_idx + (i0 << 1));
                    IdxBuffer[_IdxWritePtr++] = (ushort)(vtx_outer_idx + (i0 << 1));

                    IdxBuffer[_IdxWritePtr++] = (ushort)(vtx_outer_idx + (i0 << 1));
                    IdxBuffer[_IdxWritePtr++] = (ushort)(vtx_outer_idx + (i1 << 1));
                    IdxBuffer[_IdxWritePtr++] = (ushort)(vtx_inner_idx + (i1 << 1));
                }
                //_VtxCurrentIdx += (ushort)vtx_count;
            }
            else
            {
                // Non Anti-aliased Fill
                int idx_count = (points_count - 2) * 3;
                int vtx_count = points_count;
                //var geometry = TakePrimitives(vtx_count, idx_count, points_count - 2);
                var geometry = TakePrimitives(vtx_count, idx_count, points_count - 2, out var _VtxCurrentIdx, out var indexPos);
                var VtxBuffer = geometry.Vertices;
                var IdxBuffer = geometry.Indices;

                for (int i = 0; i < vtx_count; i++)
                    VtxBuffer[i] = new ImDrawVert() { pos = points[i], uv = uv, col = col };

                var _IdxWritePtr = 0;
                for (uint i = 2u; i < points_count; i++)
                {
                    IdxBuffer[_IdxWritePtr++] = (ushort)(_VtxCurrentIdx);
                    IdxBuffer[_IdxWritePtr++] = (ushort)(_VtxCurrentIdx + i - 1);
                    IdxBuffer[_IdxWritePtr++] = (ushort)(_VtxCurrentIdx + i);
                }

                //_VtxCurrentIdx += (ushort)vtx_count;
            }
        }

        public unsafe void AddPolyline(Span<float2> points, uint col, bool closed, float thickness, bool anti_aliased)
        {
            var points_count = points.Length;
            if (points_count < 2)
                return;

            float2 uv = FontTexUvWhitePixel;
            anti_aliased &= AntiAliasedLines;
            //if (ImGui::GetIO().KeyCtrl) anti_aliased = false; // Debug

            int count = points_count;
            if (!closed)
                count = points_count - 1;

            bool thick_line = thickness > 1.0f;
            if (anti_aliased)
            {
                // Anti-aliased stroke
                float AA_SIZE = 1.0f;
                uint col_trans = col & 0x00ffffff;

                int idx_count = thick_line ? count * 18 : count * 12;
                int vtx_count = thick_line ? points_count * 4 : points_count * 3;

                var geometry = TakePrimitives(vtx_count, idx_count, idx_count / 3, out var vertexPos, out var _VtxCurrentIdx);

                var _VtxWritePtr = 0;
                var _IdxWritePtr = 0;

                var VtxBuffer = geometry.Vertices;
                var IdxBuffer = geometry.Indices;


                // Temporary buffer
                //float2* temp_normals = (float2*)alloca(points_count * (thick_line ? 5 : 3) * sizeof(float2));
                //float2* temp_points = temp_normals + points_count;
                Span<float2> temp_normals = stackalloc float2[points_count * (thick_line ? 5 : 3)];
                Span<float2> temp_points = stackalloc float2[points_count * (thick_line ? 5 : 3)];

                for (int i1 = 0; i1 < count; i1++)
                {
                    int i2 = (i1 + 1) == points_count ? 0 : i1 + 1;
                    float2 diff = points[i2] - points[i1];
                    diff *= InvLength(diff, 1.0f);
                    temp_normals[i1].x = diff.y;
                    temp_normals[i1].y = -diff.x;
                }
                if (!closed)
                    temp_normals[points_count - 1] = temp_normals[points_count - 2];

                if (!thick_line)
                {
                    if (!closed)
                    {
                        temp_points[0] = points[0] + temp_normals[0] * AA_SIZE;
                        temp_points[1] = points[0] - temp_normals[0] * AA_SIZE;
                        temp_points[(points_count - 1) * 2 + 0] = points[points_count - 1] + temp_normals[points_count - 1] * AA_SIZE;
                        temp_points[(points_count - 1) * 2 + 1] = points[points_count - 1] - temp_normals[points_count - 1] * AA_SIZE;
                    }

                    // FIXME-OPT: Merge the different loops, possibly remove the temporary buffer.
                    int idx1 = _VtxCurrentIdx;
                    for (int i1 = 0; i1 < count; i1++)
                    {
                        int i2 = (i1 + 1) == points_count ? 0 : i1 + 1;
                        int idx2 = (i1 + 1) == points_count ? _VtxCurrentIdx : idx1 + 3;

                        // Average normals
                        float2 dm = (temp_normals[i1] + temp_normals[i2]) * 0.5f;
                        float dmr2 = dm.x * dm.x + dm.y * dm.y;
                        if (dmr2 > 0.000001f)
                        {
                            float scale = 1.0f / dmr2;
                            if (scale > 100.0f) scale = 100.0f;
                            dm *= scale;
                        }
                        dm *= AA_SIZE;
                        temp_points[i2 * 2 + 0] = points[i2] + dm;
                        temp_points[i2 * 2 + 1] = points[i2] - dm;

                        // Add indexes

                        IdxBuffer[_IdxWritePtr++] = (ushort)(idx2 + 0); IdxBuffer[_IdxWritePtr++] = (ushort)(idx1 + 0); IdxBuffer[_IdxWritePtr++] = (ushort)(idx1 + 2);
                        IdxBuffer[_IdxWritePtr++] = (ushort)(idx1 + 2); IdxBuffer[_IdxWritePtr++] = (ushort)(idx2 + 2); IdxBuffer[_IdxWritePtr++] = (ushort)(idx2 + 0);
                        IdxBuffer[_IdxWritePtr++] = (ushort)(idx2 + 1); IdxBuffer[_IdxWritePtr++] = (ushort)(idx1 + 1); IdxBuffer[_IdxWritePtr++] = (ushort)(idx1 + 0);
                        IdxBuffer[_IdxWritePtr++] = (ushort)(idx1 + 0); IdxBuffer[_IdxWritePtr++] = (ushort)(idx2 + 0); IdxBuffer[_IdxWritePtr++] = (ushort)(idx2 + 1);
                        //_IdxWritePtr += 12;

                        idx1 = idx2;
                    }

                    // Add vertexes
                    for (int i = 0; i < points_count; i++)
                    {
                        VtxBuffer[_VtxWritePtr++] = new ImDrawVert() { pos = points[i], uv = uv, col = col };
                        VtxBuffer[_VtxWritePtr++] = new ImDrawVert() { pos = temp_points[i * 2 + 0], uv = uv, col = col_trans };
                        VtxBuffer[_VtxWritePtr++] = new ImDrawVert() { pos = temp_points[i * 2 + 1], uv = uv, col = col_trans };
                    }
                }
                else
                {
                    float half_inner_thickness = (thickness - AA_SIZE) * 0.5f;
                    if (!closed)
                    {
                        temp_points[0] = points[0] + temp_normals[0] * (half_inner_thickness + AA_SIZE);
                        temp_points[1] = points[0] + temp_normals[0] * (half_inner_thickness);
                        temp_points[2] = points[0] - temp_normals[0] * (half_inner_thickness);
                        temp_points[3] = points[0] - temp_normals[0] * (half_inner_thickness + AA_SIZE);
                        temp_points[(points_count - 1) * 4 + 0] = points[points_count - 1] + temp_normals[points_count - 1] * (half_inner_thickness + AA_SIZE);
                        temp_points[(points_count - 1) * 4 + 1] = points[points_count - 1] + temp_normals[points_count - 1] * (half_inner_thickness);
                        temp_points[(points_count - 1) * 4 + 2] = points[points_count - 1] - temp_normals[points_count - 1] * (half_inner_thickness);
                        temp_points[(points_count - 1) * 4 + 3] = points[points_count - 1] - temp_normals[points_count - 1] * (half_inner_thickness + AA_SIZE);
                    }

                    // FIXME-OPT: Merge the different loops, possibly remove the temporary buffer.
                    int idx1 = _VtxCurrentIdx;
                    for (int i1 = 0; i1 < count; i1++)
                    {
                        int i2 = (i1 + 1) == points_count ? 0 : i1 + 1;
                        int idx2 = (i1 + 1) == points_count ? _VtxCurrentIdx : idx1 + 4;

                        // Average normals
                        float2 dm = (temp_normals[i1] + temp_normals[i2]) * 0.5f;
                        float dmr2 = dm.x * dm.x + dm.y * dm.y;
                        if (dmr2 > 0.000001f)
                        {
                            float scale = 1.0f / dmr2;
                            if (scale > 100.0f) scale = 100.0f;
                            dm *= scale;
                        }
                        float2 dm_out = dm * (half_inner_thickness + AA_SIZE);
                        float2 dm_in = dm * half_inner_thickness;
                        temp_points[i2 * 4 + 0] = points[i2] + dm_out;
                        temp_points[i2 * 4 + 1] = points[i2] + dm_in;
                        temp_points[i2 * 4 + 2] = points[i2] - dm_in;
                        temp_points[i2 * 4 + 3] = points[i2] - dm_out;

                        // Add indexes
                        IdxBuffer[_IdxWritePtr++] = (ushort)(idx2 + 1); IdxBuffer[_IdxWritePtr++] = (ushort)(idx1 + 1); IdxBuffer[_IdxWritePtr++] = (ushort)(idx1 + 2);
                        IdxBuffer[_IdxWritePtr++] = (ushort)(idx1 + 2); IdxBuffer[_IdxWritePtr++] = (ushort)(idx2 + 2); IdxBuffer[_IdxWritePtr++] = (ushort)(idx2 + 1);
                        IdxBuffer[_IdxWritePtr++] = (ushort)(idx2 + 1); IdxBuffer[_IdxWritePtr++] = (ushort)(idx1 + 1); IdxBuffer[_IdxWritePtr++] = (ushort)(idx1 + 0);
                        IdxBuffer[_IdxWritePtr++] = (ushort)(idx1 + 0); IdxBuffer[_IdxWritePtr++] = (ushort)(idx2 + 0); IdxBuffer[_IdxWritePtr++] = (ushort)(idx2 + 1);
                        IdxBuffer[_IdxWritePtr++] = (ushort)(idx2 + 2); IdxBuffer[_IdxWritePtr++] = (ushort)(idx1 + 2); IdxBuffer[_IdxWritePtr++] = (ushort)(idx1 + 3);
                        IdxBuffer[_IdxWritePtr++] = (ushort)(idx1 + 3); IdxBuffer[_IdxWritePtr++] = (ushort)(idx2 + 3); IdxBuffer[_IdxWritePtr++] = (ushort)(idx2 + 2);
                        //_IdxWritePtr += 18;

                        idx1 = idx2;
                    }

                    // Add vertexes
                    for (int i = 0; i < points_count; i++)
                    {
                        VtxBuffer[_VtxWritePtr++] = new ImDrawVert() { pos = temp_points[i * 4 + 0], uv = uv, col = col_trans };
                        VtxBuffer[_VtxWritePtr++] = new ImDrawVert() { pos = temp_points[i * 4 + 1], uv = uv, col = col };
                        VtxBuffer[_VtxWritePtr++] = new ImDrawVert() { pos = temp_points[i * 4 + 2], uv = uv, col = col };
                        VtxBuffer[_VtxWritePtr++] = new ImDrawVert() { pos = temp_points[i * 4 + 3], uv = uv, col = col_trans };
                        //_VtxWritePtr += 4;
                    }
                }
                //_VtxCurrentIdx += (ushort)vtx_count;
            }
            else
            {
                // Non Anti-aliased Stroke
                int idx_count = count * 6;
                int vtx_count = count * 4;      // FIXME-OPT: Not sharing edges

                var geometry = TakePrimitives(vtx_count, idx_count, count, out var _VtxCurrentIdx, out var indexPos);
                var _VtxWritePtr = 0;
                var _IdxWritePtr = 0;
                var VtxBuffer = geometry.Vertices;
                var IdxBuffer = geometry.Indices;

                for (int i1 = 0; i1 < count; i1++)
                {
                    int i2 = (i1 + 1) == points_count ? 0 : i1 + 1;
                    float2 p1 = points[i1];
                    float2 p2 = points[i2];
                    float2 diff = p2 - p1;
                    diff *= InvLength(diff, 1.0f);

                    float dx = diff.x * (thickness * 0.5f);
                    float dy = diff.y * (thickness * 0.5f);
                    VtxBuffer[_VtxWritePtr++] = new ImDrawVert() { pos = new float2(p1.x + dy, p1.y - dx), uv = uv, col = col };
                    VtxBuffer[_VtxWritePtr++] = new ImDrawVert() { pos = new float2(p2.x + dy, p2.y - dx), uv = uv, col = col };
                    VtxBuffer[_VtxWritePtr++] = new ImDrawVert() { pos = new float2(p2.x - dy, p2.y + dx), uv = uv, col = col };
                    VtxBuffer[_VtxWritePtr++] = new ImDrawVert() { pos = new float2(p1.x - dy, p1.y + dx), uv = uv, col = col };
                    //_VtxWritePtr += 4;

                    IdxBuffer[_IdxWritePtr++] = (ushort)(_VtxCurrentIdx); IdxBuffer[_IdxWritePtr++] = (ushort)(_VtxCurrentIdx + 1); IdxBuffer[_IdxWritePtr++] = (ushort)(_VtxCurrentIdx + 2);
                    IdxBuffer[_IdxWritePtr++] = (ushort)(_VtxCurrentIdx); IdxBuffer[_IdxWritePtr++] = (ushort)(_VtxCurrentIdx + 2); IdxBuffer[_IdxWritePtr++] = (ushort)(_VtxCurrentIdx + 3);
                    //_IdxWritePtr += 6;
                    _VtxCurrentIdx += 4;
                }
            }
        }


        public void AddBezierCurve(float2 pos0, float2 cp0, float2 cp1, float2 pos1, uint col, float thickness, int num_segments = 0)
        {
            if ((col >> 24) == 0)
                return;

            PathLineTo(pos0);
            PathBezierCurveTo(cp0, cp1, pos1, num_segments);
            PathStroke(col, false, thickness);
        }

        //    // Stateful path API, add points then finish with PathFill() or PathStroke()
        public void PathClear() => _Path.Reset();

        internal void PathLineTo(in float2 pos) => _Path.Add(pos);

        internal void PathLineToMergeDuplicate(in float2 pos)
        { if (_Path.Length == 0 || _Path[_Path.Length - 1].x != pos.x || _Path[_Path.Length - 1].y != pos.y) _Path.Add(pos); }

        internal void PathFill(uint col)
        { AddConvexPolyFilled(_Path, col, true); PathClear(); }

        internal void PathStroke(uint col, bool closed, float thickness = 1.0f)
        { AddPolyline(_Path, col, closed, thickness, true); PathClear(); }

        internal void PathArcTo(float2 centre, float radius, float amin, float amax, int num_segments = 10)
        {
            if (radius == 0.0f)
                _Path.Add(centre);
            _Path.EnsureCapacity((num_segments + 1));
            for (int i = 0; i <= num_segments; i++)
            {
                float a = amin + ((float)i / (float)num_segments) * (amax - amin);
                _Path.Add(new float2(centre.x + glm.Cos(a) * radius, centre.y + glm.Sin(a) * radius));
            }
        }

        // Use precomputed angles for a 12 steps circle
        internal void PathArcToFast(float2 centre, float radius, int amin, int amax)
        {
            float2[] circle_vtx = new float2[12];
            bool circle_vtx_builds = false;
            int circle_vtx_count = circle_vtx.Length;
            if (!circle_vtx_builds)
            {
                for (int i = 0; i < circle_vtx_count; i++)
                {
                    float a = ((float)i / (float)circle_vtx_count) * 2 * glm.PI;
                    circle_vtx[i].x = glm.Cos(a);
                    circle_vtx[i].y = glm.Sin(a);
                }
                circle_vtx_builds = true;
            }

            if (amin > amax) return;
            if (radius == 0.0f)
            {
                _Path.Add(centre);
            }
            else
            {
                _Path.EnsureCapacity((amax - amin + 1));
                for (int a = amin; a <= amax; a++)
                {
                    float2 c = circle_vtx[a % circle_vtx_count];
                    _Path.Add(new float2(centre.x + c.x * radius, centre.y + c.y * radius));
                }
            }
        }

        internal void PathBezierCurveTo(float2 p2, float2 p3, float2 p4, int num_segments = 0)
        {
            float2 p1 = _Path[_Path.Length - 1];
            if (num_segments == 0)
            {
                // Auto-tessellated
                PathBezierToCasteljau(p1.x, p1.y, p2.x, p2.y, p3.x, p3.y, p4.x, p4.y, CurveTessellationTol, 0);
            }
            else
            {
                float t_step = 1.0f / (float)num_segments;
                for (int i_step = 1; i_step <= num_segments; i_step++)
                {
                    float t = t_step * i_step;
                    float u = 1.0f - t;
                    float w1 = u * u * u;
                    float w2 = 3 * u * u * t;
                    float w3 = 3 * u * t * t;
                    float w4 = t * t * t;
                    _Path.Add(new float2(w1 * p1.x + w2 * p2.x + w3 * p3.x + w4 * p4.x, w1 * p1.y + w2 * p2.y + w3 * p3.y + w4 * p4.y));
                }
            }
        }

        internal void PathBezierToCasteljau(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4, float tess_tol, int level)
        {
            float dx = x4 - x1;
            float dy = y4 - y1;
            float d2 = ((x2 - x4) * dy - (y2 - y4) * dx);
            float d3 = ((x3 - x4) * dy - (y3 - y4) * dx);
            d2 = (d2 >= 0) ? d2 : -d2;
            d3 = (d3 >= 0) ? d3 : -d3;
            if ((d2 + d3) * (d2 + d3) < tess_tol * (dx * dx + dy * dy))
            {
                _Path.Add(new float2(x4, y4));
            }
            else if (level < 10)
            {
                float x12 = (x1 + x2) * 0.5f, y12 = (y1 + y2) * 0.5f;
                float x23 = (x2 + x3) * 0.5f, y23 = (y2 + y3) * 0.5f;
                float x34 = (x3 + x4) * 0.5f, y34 = (y3 + y4) * 0.5f;
                float x123 = (x12 + x23) * 0.5f, y123 = (y12 + y23) * 0.5f;
                float x234 = (x23 + x34) * 0.5f, y234 = (y23 + y34) * 0.5f;
                float x1234 = (x123 + x234) * 0.5f, y1234 = (y123 + y234) * 0.5f;

                PathBezierToCasteljau(x1, y1, x12, y12, x123, y123, x1234, y1234, tess_tol, level + 1);
                PathBezierToCasteljau(x1234, y1234, x234, y234, x34, y34, x4, y4, tess_tol, level + 1);
            }
        }

        internal void PathRect(float2 a, float2 b, float rounding = 0.0f, int rounding_corners = 0x0F)
        {
            float r = rounding;
            r = glm.Min(r, glm.FastAbs(b.x - a.x) * (((rounding_corners & (1 | 2)) == (1 | 2)) || ((rounding_corners & (4 | 8)) == (4 | 8)) ? 0.5f : 1.0f) - 1.0f);
            r = glm.Min(r, glm.FastAbs(b.y - a.y) * (((rounding_corners & (1 | 8)) == (1 | 8)) || ((rounding_corners & (2 | 4)) == (2 | 4)) ? 0.5f : 1.0f) - 1.0f);

            if (r <= 0.0f || rounding_corners == 0)
            {
                PathLineTo(a);
                PathLineTo(new float2(b.x, a.y));
                PathLineTo(b);
                PathLineTo(new float2(a.x, b.y));
            }
            else
            {
                float r0 = (rounding_corners & 1) > 0 ? r : 0.0f;
                float r1 = (rounding_corners & 2) > 0 ? r : 0.0f;
                float r2 = (rounding_corners & 4) > 0 ? r : 0.0f;
                float r3 = (rounding_corners & 8) > 0 ? r : 0.0f;
                PathArcToFast(new float2(a.x + r0, a.y + r0), r0, 6, 9);
                PathArcToFast(new float2(b.x - r1, a.y + r1), r1, 9, 12);
                PathArcToFast(new float2(b.x - r2, b.y - r2), r2, 0, 3);
                PathArcToFast(new float2(a.x + r3, b.y - r3), r3, 3, 6);
            }
        }
    }
}