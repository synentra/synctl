using System.Diagnostics;

namespace SynentraCtl.Services.Version;

public class VersionInfoProvider : IVersionInfoProvider
{
    public string GetFileVersion(string path)
    {
        var versionInfo = FileVersionInfo.GetVersionInfo(path);
        return versionInfo.FileVersion ?? string.Empty;
    }
}