using System.Reflection;

namespace VectraCtl.Services.Version;

public class VersionHandler : IVersion
{
    private readonly IFileSystem _fileSystem;
    private readonly IVersionInfoProvider _versionInfoProvider;

    public VersionHandler(IFileSystem fileSystem, IVersionInfoProvider versionInfoProvider)
    {
        _fileSystem = fileSystem;
        _versionInfoProvider = versionInfoProvider;
    }

    public string VectraCtlVersion
    {
        get
        {
            var attributes = Assembly
                .GetExecutingAssembly()
                .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);

            return attributes.Length == 0
                ? string.Empty
                : ((AssemblyInformationalVersionAttribute)attributes[0]).InformationalVersion;
        }
    }

    public string GetVersionFromPath(string path)
    {
        if (string.IsNullOrEmpty(path) || !_fileSystem.FileExists(path))
            return string.Empty;

        return _versionInfoProvider.GetFileVersion(path) ?? string.Empty;
    }
}