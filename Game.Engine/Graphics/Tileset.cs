// namespace Worm.Graphics
// {
//     public class Tileset
//     {
//         private MTexture[,] tiles;

//         public Tileset(MTexture texture, int tilesX, int tilesY)
//         {
//             Texture = texture;
//             TileWidth = Texture.Width / tilesX;
//             TileHeight = texture.Height / tilesY;

//             tiles = new MTexture[Texture.Width / TileWidth, Texture.Height / TileHeight];
//             for (int x = 0; x < Texture.Width / TileWidth; x++)
//                 for (int y = 0; y < Texture.Height / TileHeight; y++)
//                     tiles[x, y] = new MTexture(Texture, x * TileWidth, y * TileHeight, TileWidth, TileHeight);
//         }

//         public MTexture Texture
//         {
//             get; private set;
//         }

//         public int TileWidth
//         {
//             get; private set;
//         }

//         public int TileHeight
//         {
//             get; private set;
//         }

//         public MTexture this[int x, int y]
//         {
//             get
//             {
//                 return tiles[x, y];
//             }
//         }

//         public MTexture this[int index]
//         {
//             get
//             {
//                 if (index < 0)
//                     return null;
//                 else
//                     return tiles[index % tiles.GetLength(0), index / tiles.GetLength(0)];
//             }
//         }
//     }
// }
