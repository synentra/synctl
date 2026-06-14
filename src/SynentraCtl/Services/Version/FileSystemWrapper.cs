namespace VectraCtl.Services.Version;

public class FileSystemWrapper : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);
}