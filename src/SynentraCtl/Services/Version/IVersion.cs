namespace SynentraCtl.Services.Version;

public interface IVersion
{
    string SynentraCtlVersion { get; }
    string GetVersionFromPath(string path);
}