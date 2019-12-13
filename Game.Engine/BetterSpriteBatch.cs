using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Atma;
using Atma.Common;
using Atma.Math;
using Atma.Memory;
using Game.Framework;
using Game.Framework.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Worm
{


    [AutoRegister(true)]
    public class DrawContextFactory
    {
        private readonly ITextureManager _textures;
        private readonly IGraphicsBufferFactory _bufferFactory;
        private readonly RenderCommandFactory _renderCommandFactory;
        public DrawContextFactory(ITextureManager textures, IGraphicsBufferFactory bufferFactory, RenderCommandFactory renderCommandFactory)
        {
            _textures = textures;
            _bufferFactory = bufferFactory;
            _renderCommandFactory = renderCommandFactory;
        }

        public BetterSpriteBatch CreateDrawContext() => new BetterSpriteBatch(_textures, _bufferFactory, _renderCommandFactory.Create());
    }

}