// namespace Worms.Prefabs
// {
//     using Atma.Entities;
//     using Microsoft.Xna.Framework;
//     using Worm.Managers;

//     public class PlayerPrefab : IPrefab
//     {

//         public void Create(in EntityCommandBuffer buffer)
//         {
//             buffer.Replace(new Sprite(Sprites.Player, 16, 24));
//             buffer.Replace(new Scale(2, 2));
//             buffer.Replace(new SpriteAnimation(Animations.Player.AnimationID, fps: 1f / 10, enabled: true));
//             buffer.Replace(Animations.Player.Frames[0].Region);
//             buffer.Replace(Color.White);
//         }
//     }
// }