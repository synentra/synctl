namespace VectraCtl.Services.Version;

public interface IVersion
{
    string VectraCtlVersion { get; }
    string GetVersionFromPath(string path);
}