// namespace Worm.Graphics
// {
//     using Atma;
//     using System;
//     using System.Collections.Generic;
//     using Microsoft.Xna.Framework.Graphics;
//     using System.IO;
//     using GlobExpressions;
//     using System.Linq;
//     using Microsoft.Xna.Framework;

//     public class AtlasBuilder
//     {
//         private int _padding = 1;
//         private int _maxWidth;

//         private List<KeyValuePair<string, Texture2D>> _textures = new List<KeyValuePair<string, Texture2D>>();

//         public AtlasBuilder(int maxWidth = 1024, int padding = 0)
//         {
//             _maxWidth = maxWidth;
//             _padding = padding;
//         }

//         public void AddPath(string path, string globPattern = null)
//         {
//             if (!Path.IsPathRooted(path))
//                 path = Path.Combine(Environment.CurrentDirectory, path);

//             if (!path.EndsWith("\\"))
//                 path += "\\";

//             if (globPattern == null)
//             {
//                 foreach (var it in Directory.GetFiles(path))
//                     AddFile(it.Substring(path.Length), it);
//             }
//             else
//             {
//                 foreach (var g in Glob.Files(path, globPattern))
//                 {
//                     var it = Path.Combine(path, g);
//                     AddFile(it.Substring(path.Length), it);
//                 }
//             }
//         }

//         public void AddFile(string key, string file)
//         {
//             using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
//             var texture = Texture2D.FromStream(Engine.Instance.GraphicsDevice, fs);

//             _textures.Add(new KeyValuePair<string, Texture2D>(key.ToLower().Replace('\\', '/'), texture));
//         }

//         public unsafe Atlas Build()
//         {
//             var packer = new MaxRectsBinPack(_maxWidth, int.MaxValue, false);

//             Span<PackRect> srcRects = stackalloc PackRect[_textures.Count];
//             Span<PackRect> dstRects = stackalloc PackRect[_textures.Count];
//             for (var i = 0; i < srcRects.Length; i++)
//                 srcRects[i] = new PackRect(0, 0, _textures[i].Value.Width + _padding, _textures[i].Value.Height + _padding);

//             packer.Insert(srcRects, dstRects, FreeRectChoiceHeuristic.RectBestShortSideFit);
//             var maxY = 0;
//             for (var i = 0; i < dstRects.Length; i++)
//                 if (dstRects[i].MaxY > maxY)
//                     maxY = dstRects[i].MaxY;

//             maxY = Utils.NextPowerOf2(maxY);

//             var dstTexture = new Texture2D(Engine.Instance.GraphicsDevice, _maxWidth, maxY);
//             var mtexture = new MTexture(dstTexture);
//             var dict = new Dictionary<int, MTexture>();
//             var dstColors = new Color[dstTexture.Width * dstTexture.Height];
//             for (var i = 0; i < _textures.Count; i++)
//             {
//                 var key = _textures[i].Key;
//                 var srcTexture = _textures[i].Value;
//                 ReadOnlySpan<char> chars = key;
//                 var id = String.GetHashCode(chars, StringComparison.InvariantCultureIgnoreCase);

//                 var x = dstRects[i].X;
//                 var y = dstRects[i].Y;
//                 var w = srcTexture.Width;
//                 var h = srcTexture.Height;

//                 var mt = new MTexture(mtexture, x, y, w, h);
//                 dict.Add(id, mt);

//                 var srcColors = new Color[w * h];
//                 srcTexture.GetData(srcColors);

//                 for (var yy = y; yy < y + h + _padding; yy++)
//                 {
//                     for (var xx = x; xx < x + w + _padding; xx++)
//                     {
//                         var srcX = xx - x;
//                         var srcY = yy - y;
//                         var srcIndex = srcY * srcTexture.Width + srcX;
//                         var dstIndex = yy * dstTexture.Width + xx;

//                         if (srcX >= srcTexture.Width || srcY >= srcTexture.Height)
//                             dstColors[dstIndex] = Color.Transparent; //not sure what textures are initialized to
//                         else
//                             dstColors[dstIndex] = srcColors[srcIndex];
//                     }
//                 }
//             }

//             dstTexture.SetData(dstColors);
//             return new Atlas(mtexture, dict);
//         }

//     }

//     public class Atlas : IDisposable
//     {
//         private Dictionary<int, MTexture> _textures;
//         public readonly MTexture Texture;
//         public IReadOnlyDictionary<int, MTexture> Textures => _textures;

//         public Atlas(MTexture texture, Dictionary<int, MTexture> textures)
//         {
//             Texture = texture;
//             _textures = textures;
//         }

//         public unsafe MTexture GetTexture(string key)
//         {
//             ReadOnlySpan<char> chars = key;
//             var id = String.GetHashCode(chars, StringComparison.InvariantCultureIgnoreCase);
//             if (!Textures.TryGetValue(id, out var texture))
//                 return null;
//             return texture;
//         }

//         public void Dispose()
//         {
//             Texture.Dispose();
//             _textures.Clear();
//         }

//     }
// }