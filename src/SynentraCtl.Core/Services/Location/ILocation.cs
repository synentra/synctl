namespace VectraCtl.Core.Services.Location;

public interface ILocation
{
    string RootLocation { get; }
    string UserProfilePath { get; }
    string DefaultVectraDirectoryName { get; }
    string DefaultVectraBinaryDirectoryName { get; }
    string VectraBinaryName { get; }

    string LookupVectraCtlBinaryFilePath(string path);
    string LookupVectraBinaryFilePath(string path);
}