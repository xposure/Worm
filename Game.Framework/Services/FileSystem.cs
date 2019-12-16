namespace Game.Framework.Services
{
    using System.IO;

    [GameService]
    public interface IFileSystem
    {
        Stream OpenFile(string path);
    }
}