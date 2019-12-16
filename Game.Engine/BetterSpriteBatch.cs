namespace Game.Engine.Managers
{
    using Game.Framework;
    using Game.Framework.Managers;

    [AutoRegister(true)]
    public class DrawContextFactory : IDrawContextFactory
    {
        private readonly ITextureManager _textures;
        private readonly IGraphicsBufferFactory _bufferFactory;
        private readonly IRenderCommandFactory _renderCommandFactory;
        public DrawContextFactory(ITextureManager textures, IGraphicsBufferFactory bufferFactory, IRenderCommandFactory renderCommandFactory)
        {
            _textures = textures;
            _bufferFactory = bufferFactory;
            _renderCommandFactory = renderCommandFactory;
        }

        public DrawContext CreateDrawContext() => new DrawContext(_textures, _bufferFactory, _renderCommandFactory.Create());
    }

}