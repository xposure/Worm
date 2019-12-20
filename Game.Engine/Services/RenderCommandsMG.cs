namespace Game.Engine.Services
{
    using System.Collections.Generic;
    using Atma;
    using Atma.Math;
    using Atma.Memory;
    using Game.Framework.Services.Graphics;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Blend = Framework.Services.Graphics.Blend;
    using BlendFunction = Framework.Services.Graphics.BlendFunction;

    public class RenderCommandFactoryMG : Framework.Services.Graphics.IRenderCommandFactory
    {
        private readonly IAllocator _memory;
        private readonly GraphicsDevice _device;

        public RenderCommandFactoryMG(IAllocator memory, GraphicsDevice device)
        {
            _memory = memory;
            _device = device;
        }

        public IRenderCommandBuffer Create() => new RenderCommandBuffer(_memory, _device);
    }

    public unsafe class RenderCommandBuffer : UnmanagedDispose, IRenderCommandBuffer
    {
        private enum CommandTypes
        {
            Blend,
            Depth,
            Rasterizer,
            Sampler,
            Effect,
            Texture,
            IndexBuffer,
            VertexBuffer,
            Camera,
            Render
        }

        private struct RenderCommand
        {
            public CommandTypes CommandType;
            public int Size;

            public RenderCommand(CommandTypes type)
            {
                CommandType = type;
                Size = sizeof(RenderCommand);
            }
        }

        private struct GraphicsStateChange
        {
            public CommandTypes CommandType;
            public int Size;
            public int StateIndex;

            public GraphicsStateChange(CommandTypes type, int stateIndex)
            {
                CommandType = type;
                StateIndex = stateIndex;
                Size = sizeof(GraphicsStateChange);
            }
        }

        // private struct EffectParamMatrix
        // {
        //     public CommandTypes CommandType;
        //     public int PayloadSize;
        //     public int Index;
        //     public Matrix Value;

        //     public EffectParamMatrix(CommandTypes type, int index, Matrix value)
        //     {
        //         CommandType = type;
        //         Index = index;
        //         Value = value;
        //         PayloadSize = sizeof(int) + sizeof(Matrix);
        //     }
        // }

        private struct CameraOperation
        {
            public CommandTypes CommandType;
            public int Size;
            public RenderCameraType RenderCameraType;
            public Matrix Matrix;

            public CameraOperation(RenderCameraType cameraType, Matrix matrix)
            {
                CommandType = CommandTypes.Camera;
                RenderCameraType = cameraType;
                Matrix = matrix;
                Size = sizeof(CameraOperation);
            }
        }

        private struct RenderOperation
        {
            public CommandTypes CommandType;
            public int Size;
            public int StartIndex;
            public int PrimitiveCount;

            public RenderOperation(int startIndex, int primitiveCount)
            {
                CommandType = CommandTypes.Render;
                StartIndex = startIndex;
                PrimitiveCount = primitiveCount;
                Size = sizeof(RenderOperation);
            }
        }

        public int Commands { get; private set; }
        public int Triangles { get; private set; }

        private NativeBuffer _buffer;
        private readonly GraphicsDevice _device;
        private BasicEffect _defaultEffect;
        private DepthStencilState _defaultDepth = DepthStencilState.None;
        private BlendState _defaultBlend = BlendState.Opaque;
        private RasterizerState _defaultRasterizer = RasterizerState.CullNone;
        private SamplerState _defaultSampler = SamplerState.PointClamp;

        public GraphicsDevice GraphicsDevice => _device;
        private float4x4 _projection = float4x4.Identity;
        private Viewport _lastViewport = new Viewport();

        public RenderCommandBuffer(IAllocator allocator, GraphicsDevice device)
        {
            _buffer = new NativeBuffer(allocator);
            _device = device;

            _defaultEffect = new BasicEffect(_device);
            _defaultEffect.TextureEnabled = true;
            _defaultEffect.VertexColorEnabled = true;
        }


        private Dictionary<int, BlendState> _blendStateCache = new Dictionary<int, BlendState>();
        public void SetBlendMode(BlendFunction blendRgb, BlendFunction blendA, Blend srcRgb, Blend srcA, Blend dstRgb, Blend dstA)
        {
            var hash = HashCode.Combine((int)blendRgb, (int)blendA, (int)srcRgb, (int)srcA, (int)dstRgb, (int)dstA);
            if (!_blendStateCache.TryGetValue(hash, out var blendState))
            {
                blendState = new BlendState();
                blendState.ColorWriteChannels = ColorWriteChannels.All;
                blendState.ColorWriteChannels1 = ColorWriteChannels.All;
                blendState.ColorWriteChannels2 = ColorWriteChannels.All;
                blendState.ColorWriteChannels3 = ColorWriteChannels.All;
                blendState.AlphaBlendFunction = (Microsoft.Xna.Framework.Graphics.BlendFunction)blendA;
                blendState.ColorBlendFunction = (Microsoft.Xna.Framework.Graphics.BlendFunction)blendRgb;
                blendState.ColorSourceBlend = (Microsoft.Xna.Framework.Graphics.Blend)srcRgb;
                blendState.ColorDestinationBlend = (Microsoft.Xna.Framework.Graphics.Blend)dstRgb;
                blendState.AlphaSourceBlend = (Microsoft.Xna.Framework.Graphics.Blend)srcA;
                blendState.AlphaDestinationBlend = (Microsoft.Xna.Framework.Graphics.Blend)dstA;

                _blendStateCache.Add(hash, blendState);
            }

            SetBlendState(blendState);
        }

        private List<BlendState> _blendStates = new List<BlendState>();
        private void SetBlendState(BlendState blendState)
        {
            var index = _blendStates.Count;
            _blendStates.Add(blendState);
            _buffer.Add(new GraphicsStateChange(CommandTypes.Blend, index));
        }

        private List<DepthStencilState> _depthStates = new List<DepthStencilState>();
        public void SetDepthState(DepthStencilState depthState)
        {
            var index = _depthStates.Count;
            _depthStates.Add(depthState);
            _buffer.Add(new GraphicsStateChange(CommandTypes.Depth, index));
        }

        private List<RasterizerState> _rasterizerStates = new List<RasterizerState>();
        public void SetRasterizerState(RasterizerState rasitizerState)
        {
            var index = _rasterizerStates.Count;
            _rasterizerStates.Add(rasitizerState);
            _buffer.Add(new GraphicsStateChange(CommandTypes.Rasterizer, index));
        }

        private List<SamplerState> _samplerStates = new List<SamplerState>();
        public void SetSamplerState(SamplerState samplerState)
        {
            var index = _samplerStates.Count;
            _samplerStates.Add(samplerState);
            _buffer.Add(new GraphicsStateChange(CommandTypes.Sampler, index));
        }

        private List<ITexture> _textures = new List<ITexture>();
        public void SetTexture(ITexture texture)
        {
            var index = _textures.Count;
            _textures.Add(texture);
            _buffer.Add(new GraphicsStateChange(CommandTypes.Texture, index));
        }

        private List<Effect> _effects = new List<Effect>();
        public void SetEffect(Effect effect)
        {
            var index = _effects.Count;
            _effects.Add(effect);
            _buffer.Add(new GraphicsStateChange(CommandTypes.Effect, index));
        }

        private List<IIndexBuffer> _indicies = new List<IIndexBuffer>();
        public void SetIndexBuffer(IIndexBuffer indicies)
        {
            var index = _indicies.Count;
            _indicies.Add(indicies);
            _buffer.Add(new GraphicsStateChange(CommandTypes.IndexBuffer, index));
        }

        private List<IVertexBuffer> _vertices = new List<IVertexBuffer>();
        public void SetVertexBuffer(IVertexBuffer vertices)
        {
            var index = _vertices.Count;
            _vertices.Add(vertices);
            _buffer.Add(new GraphicsStateChange(CommandTypes.VertexBuffer, index));
        }

        public void SetCamera(RenderCameraType cameraType, in float4x4 matrix)
        {
            _buffer.Add(new CameraOperation(cameraType, Convert4x4(matrix)));
        }

        private static Matrix Convert4x4(in float4x4 m)
            => new Matrix(Convert4(m.Column0), Convert4(m.Column1), Convert4(m.Column2), Convert4(m.Column3));

        private static Vector2 Convert2(in float2 it) => new Vector2(it.x, it.y);
        private static Vector3 Convert3(in float3 it) => new Vector3(it.x, it.y, it.z);
        private static Vector4 Convert4(in float4 it) => new Vector4(it.x, it.y, it.z, it.w);

        public void RenderPrimitives(int startIndex, int primitiveCount)
        {
            _buffer.Add(new RenderOperation(startIndex, primitiveCount));
        }


        public void Render()
        {
            Commands = 0;
            Triangles = 0;
            var rawPtr = _buffer.RawPointer;
            Effect currentEffect = null;
            while (rawPtr < _buffer.EndPointer)
            {
                Commands++;
                var cmd = (RenderCommand*)rawPtr;
                switch (cmd->CommandType)
                {
                    case CommandTypes.Blend:
                        {
                            var it = _blendStates[((GraphicsStateChange*)cmd)->StateIndex];
                            _device.BlendState = it;
                            break;
                        }
                    case CommandTypes.Depth:
                        {
                            var it = _depthStates[((GraphicsStateChange*)cmd)->StateIndex];
                            _device.DepthStencilState = it;
                            break;
                        }
                    case CommandTypes.Rasterizer:
                        {
                            var it = _rasterizerStates[((GraphicsStateChange*)cmd)->StateIndex];
                            _device.RasterizerState = it;
                            break;
                        }
                    case CommandTypes.Sampler:
                        {
                            var it = _samplerStates[((GraphicsStateChange*)cmd)->StateIndex];
                            _device.SamplerStates[0] = it;
                            break;
                        }
                    case CommandTypes.Effect:
                        {
                            currentEffect = _effects[((GraphicsStateChange*)cmd)->StateIndex];
                            break;
                        }
                    case CommandTypes.Texture:
                        {
                            _textures[((GraphicsStateChange*)cmd)->StateIndex].Bind();
                            if (currentEffect is BasicEffect basic && _device.Textures[0] is Texture2D tex)
                                basic.Texture = tex;

                            break;
                        }
                    case CommandTypes.VertexBuffer:
                        {
                            _vertices[((GraphicsStateChange*)cmd)->StateIndex].Bind();
                            break;
                        }
                    case CommandTypes.IndexBuffer:
                        {
                            _indicies[((GraphicsStateChange*)cmd)->StateIndex].Bind();
                            break;
                        }
                    case CommandTypes.Camera:
                        {
                            Assert.EqualTo(currentEffect is BasicEffect, true);
                            var effect = (BasicEffect)currentEffect;
                            var it = (CameraOperation*)cmd;
                            if (it->RenderCameraType == RenderCameraType.World)
                                effect.World = it->Matrix;
                            else if (it->RenderCameraType == RenderCameraType.View)
                                effect.View = it->Matrix;
                            else if (it->RenderCameraType == RenderCameraType.Projection)
                            {
                                //Matrix.CreateOrthographicOffCenter(0, 800, 480, 0, 0, -1, out var projection);                                
                                effect.Projection = Convert4x4(_projection);
                            }
                            else
                                throw new System.NotImplementedException();
                            break;
                        }
                    case CommandTypes.Render:
                        {
                            var renderOp = (RenderOperation*)cmd;
                            if (currentEffect != null)
                            {
                                var technique = currentEffect.CurrentTechnique;
                                foreach (var pass in technique.Passes)
                                {
                                    pass.Apply();
                                    Triangles += renderOp->PrimitiveCount;
                                    _device.DrawIndexedPrimitives(PrimitiveType.TriangleList, renderOp->StartIndex, 0, renderOp->PrimitiveCount);
                                }
                            }
                            else
                            {
                                Triangles += renderOp->PrimitiveCount;
                                _device.DrawIndexedPrimitives(PrimitiveType.TriangleList, renderOp->StartIndex, 0, renderOp->PrimitiveCount);
                            }
                            break;
                        }
                }
                rawPtr += cmd->Size;
            }

            _buffer.Reset();
            _blendStates.Clear();
            _depthStates.Clear();
            _rasterizerStates.Clear();
            _samplerStates.Clear();
            _textures.Clear();
            _effects.Clear();
            _indicies.Clear();
            _vertices.Clear();
        }


        public void ResetState()
        {
            UpdateProjection();
            SetEffect(_defaultEffect);
            SetDepthState(_defaultDepth);
            SetBlendState(_defaultBlend);
            SetRasterizerState(_defaultRasterizer);
            SetSamplerState(_defaultSampler);
            SetCamera(RenderCameraType.World, float4x4.Identity);
            SetCamera(RenderCameraType.Projection, _projection);
            SetCamera(RenderCameraType.View, float4x4.Identity);
        }

        public unsafe void UpdateProjection()
        {
            // set up our matrix to match basic effect.
            Viewport viewport = _device.Viewport;
            //
            var vp = _device.Viewport;
            if ((_lastViewport.Width != vp.Width) || (_lastViewport.Height != vp.Height))
            {
                _projection = float4x4.Identity;
                // Normal 3D cameras look into the -z direction (z = 1 is in front of z = 0). The
                // sprite batch layer depth is the opposite (z = 0 is in front of z = 1).
                // --> We get the correct matrix with near plane 0 and far plane -1.
                _projection = float4x4.Ortho(0, vp.Width, vp.Height, 0, 0, -1);
                //Matrix.CreateOrthographicOffCenter(0, vp.Width, vp.Height, 0, 0, -1, out var projection);
                _projection[10] = 1f;
                _projection[14] = 0f;

                // Some platforms require a half pixel offset to match DX.
                //if (NeedsHalfPixelOffset)
                // {
                //     _projection.M41 += -0.5f * _projection.M11;
                //     _projection.M42 += -0.5f * _projection.M22;
                // }

                _defaultEffect.World = Matrix.Identity;
                _defaultEffect.Projection = Convert4x4(_projection);
                _defaultEffect.View = Matrix.Identity;

                _lastViewport = vp;

            }
        }

        protected override void OnUnmanagedDispose()
        {
            _defaultEffect.Dispose();
            _blendStateCache.DisposeAll();
            _blendStates.Clear();
            _depthStates.Clear();
            _rasterizerStates.Clear();
            _samplerStates.Clear();
            _textures.Clear();
            _effects.Clear();
            _indicies.Clear();
            _vertices.Clear();
            _buffer.Dispose();
        }
    }
}