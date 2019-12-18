namespace Game.Engine.Services
{
    using System.IO;
    using Game.Framework.Services;

    public class FileSystemMG : IFileSystem
    {
        public Stream ReadAsset(string path) => File.OpenRead(path);
    }
}