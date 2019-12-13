namespace Worm
{
    using System.Collections.Generic;
    using Atma;
    using Atma.Memory;
    using Game.Framework;
    using Game.Framework.Managers;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    [AutoRegister(true)]
    public class RenderCommandFactory
    {
        private readonly IAllocator _memory;
        private readonly GraphicsDevice _device;

        public RenderCommandFactory(IAllocator memory, GraphicsDevice device)
        {
            _memory = memory;
            _device = device;
        }

        public RenderCommandBuffer Create() => new RenderCommandBuffer(_memory, _device);
    }

    public unsafe class RenderCommandBuffer : UnmanagedDispose
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

        public enum CameraType
        {
            World,
            Projection,
            View
        }

        private struct CameraOperation
        {
            public CommandTypes CommandType;
            public int Size;
            public CameraType CameraType;
            public Matrix Matrix;

            public CameraOperation(CameraType cameraType, Matrix matrix)
            {
                CommandType = CommandTypes.Camera;
                CameraType = cameraType;
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

        public GraphicsDevice GraphicsDevice => _device;

        public RenderCommandBuffer(IAllocator allocator, GraphicsDevice device)
        {
            _buffer = new NativeBuffer(allocator);
            _device = device;
        }

        private List<BlendState> _blendStates = new List<BlendState>();
        public void SetBlendState(BlendState blendState)
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

        private List<Texture> _textures = new List<Texture>();
        public void SetTexture(Texture texture)
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

        public void SetCamera(CameraType cameraType, Matrix matrix)
        {
            _buffer.Add(new CameraOperation(cameraType, matrix));
        }

        public void RenderOp(int startIndex, int primitiveCount)
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
                            var tex = _textures[((GraphicsStateChange*)cmd)->StateIndex];
                            _device.Textures[0] = tex;
                            if (currentEffect is BasicEffect basic && tex is Texture2D tex2d)
                                basic.Texture = tex2d;

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
                            if (it->CameraType == CameraType.World)
                                effect.World = it->Matrix;
                            else if (it->CameraType == CameraType.View)
                                effect.View = it->Matrix;
                            else if (it->CameraType == CameraType.Projection)
                                effect.Projection = it->Matrix;
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

        protected override void OnUnmanagedDispose()
        {
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