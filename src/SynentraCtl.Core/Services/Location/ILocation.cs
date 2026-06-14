namespace SynentraCtl.Core.Services.Location;

public interface ILocation
{
    string RootLocation { get; }
    string UserProfilePath { get; }
    string DefaultSynentraDirectoryName { get; }
    string DefaultSynentraBinaryDirectoryName { get; }
    string SynentraBinaryName { get; }

    string LookupSynentraCtlBinaryFilePath(string path);
    string LookupSynentraBinaryFilePath(string path);
}