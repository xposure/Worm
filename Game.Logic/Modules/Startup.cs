using System;
using System.IO;
using Atma.Entities;
using Atma.Events;
using Atma.Math;
using Atma.Systems;

namespace Game.Logic
{

    public class Startup : SystemBase
    {
        public const int TILE_SIZE = 4;

        private EntityManager _entities;

        public Startup(EntityManager entities, IEventManager events)
        {
            _entities = entities;
            var mi = typeof(Startup).GetMethod("LoadScene");
            var action = (Action)mi.CreateDelegate(typeof(Action), this);

            Track(events.Subscribe(nameof(Events.LoadScene), action));

        }

        public void LoadScene()
        {
            CreateWalls();
            CreateCamera(0);
        }

        public void CreateWalls()
        {
            //TODO: IFileSystem
            using (var sr = File.OpenText(@"Assets\room.data"))
            {
                var wallSpec = EntitySpec.Create<Position, Sprite, Color, Solid, Scale>();
                var em = _entities;

                var wall2 = em.Create(wallSpec);
                em.Replace(wall2, new Sprite(0, 100, 100) { OriginX = 0.5f, OriginY = 0.5f });
                em.Replace(wall2, new Scale(1, 1));
                em.Replace(wall2, new Position(0, 0));
                em.Replace(wall2, Color.White);

                string line = null;
                var y = 0;
                while ((line = sr.ReadLine()) != null)
                {
                    for (var x = 0; x < line.Length; x++)
                    {
                        if (line[x] == '1')
                        {
                            var wall = em.Create(wallSpec);
                            em.Replace(wall, new Sprite(0, TILE_SIZE, TILE_SIZE) { OriginX = 0, OriginY = 0 });
                            em.Replace(wall, new Position(x * TILE_SIZE, y * TILE_SIZE));
                            em.Replace(wall, new Scale(1, 1));
                            em.Replace(wall, Color.White);
                            em.Replace(wall, new Solid() { Bounds = AxisAlignedBox2.FromRect(float2.Zero, new float2(TILE_SIZE, TILE_SIZE)) });
                        }
                    }
                    y++;
                }
            }
        }

        private uint CreatePlayer()
        {
            var em = _entities;

            var playerSpec = EntitySpec.Create<Position, Sprite, PlayerInput, TextureRegion, Collider, Move, Gravity, Input>();
            var player = em.Create(playerSpec);
            em.Replace(player, new PlayerInput());
            em.Replace(player, new Move() { Friction = new float2(0.85f), Acceleration = new float2(2000), Speed = float2.Zero });
            em.Replace(player, new Position(TILE_SIZE, 21 * TILE_SIZE));
            //_entities.Replace(player, new Sprite(Sprites.Player, 32, 48) { OriginX = 0.5f, OriginY = 1f });
            em.Replace(player, new Collider() { Type = ColliderType.Player, Bounds = AxisAlignedBox2.FromRect(new float2(16, 48), new float2(32, 48)) });
            //_entities.Replace(player, FromTexture(Sprites.Player, 16, 24, 0, 2));
            em.Replace(player, Gravity.Default);
            return player;
        }
        // public static TextureRegion FromTexture(Texture2D texture, int tileWidth, int tileHeight, int tileX, int tileY)
        // {
        //     var tilesX = texture.Width / tileWidth;
        //     var tilesY = texture.Height / tileHeight;

        //     Assert.EqualTo(tilesX * tileWidth, texture.Width);
        //     Assert.EqualTo(tilesY * tileHeight, texture.Height);

        //     var tileW = 1f / tilesX;
        //     var tileH = 1f / tilesY;

        //     return TextureRegion.FromSize(tileX * tileW, tileY * tileW, tileW, tileH);
        // }
        private void CreateCamera(uint target)
        {
            var em = _entities;
            var cameraSpec = EntitySpec.Create<Camera, Position>();
            var camera = em.Create(cameraSpec);
            em.Replace(camera, new Camera()
            {
                EntityTrack = target,
                TrackSpeed = 10f,
                Width = 800,
                Height = 480
            });
        }


        protected override void OnGatherDependencies(DependencyListConfig config)
        {

        }

        protected override void OnInit()
        {
        }

        protected override void OnTick(SystemManager systemManager, EntityManager entityManager)
        {
        }
    }
}