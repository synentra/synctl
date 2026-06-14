using System.Runtime.InteropServices;
using SynentraCtl.Core.Services.Location;

namespace SynentraCtl.Services.Location;

public class LocationService : ILocation
{
    private readonly string _rootLocation;

    public LocationService()
    {
        _rootLocation = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) 
            ?? throw new DirectoryNotFoundException("The base location could not be found.");
    }

    public string RootLocation => _rootLocation;

    public string UserProfilePath => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public string DefaultSynentraDirectoryName => Path.Combine(UserProfilePath, ".synentra");

    public string DefaultSynentraBinaryDirectoryName => Path.Combine(DefaultSynentraDirectoryName, "gateway");

    public string SynentraBinaryName => "synentra";

    public string LookupSynentraBinaryFilePath(string path) =>
        Path.Combine(path, AppendExecutableExtension("synentra"));

    public string LookupSynentraCtlBinaryFilePath(string path) =>
        Path.Combine(path, AppendExecutableExtension("synctl"));

    private static string AppendExecutableExtension(string binaryName) =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"{binaryName}.exe" : binaryName;
}