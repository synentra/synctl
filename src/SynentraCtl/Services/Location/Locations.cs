using System.Runtime.InteropServices;
using VectraCtl.Core.Services.Location;

namespace VectraCtl.Services.Location;

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

    public string DefaultVectraDirectoryName => Path.Combine(UserProfilePath, ".vectra");

    public string DefaultVectraBinaryDirectoryName => Path.Combine(DefaultVectraDirectoryName, "gateway");

    public string VectraBinaryName => "vectra";

    public string LookupVectraBinaryFilePath(string path) =>
        Path.Combine(path, AppendExecutableExtension("vectra"));

    public string LookupVectraCtlBinaryFilePath(string path) =>
        Path.Combine(path, AppendExecutableExtension("vectractl"));

    private static string AppendExecutableExtension(string binaryName) =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"{binaryName}.exe" : binaryName;
}