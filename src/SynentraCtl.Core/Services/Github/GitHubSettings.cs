using System.Runtime.InteropServices;

namespace VectraCtl.Core.Services.Github;

public static class GitHubSettings
{
    // Organization and Repository Names
    public const string Organization = "cortexiumlabs";
    public const string VectraRepository = "vectra";
    public const string VectraCtlRepository = "vectractl";

    // Archive and Hash File Extensions
    private static readonly string HashFileExtension = "sha256";
    private static readonly string CompressionFileExtension = GetCompressionExtension();

    // OS & Architecture Info
    private static readonly string OSType = GetOperatingSystemType();
    private static readonly string Architecture = RuntimeInformation.ProcessArchitecture.ToString();

    // Archive Names
    private static readonly string ArchiveName = $"{OSType}-{Architecture}.{CompressionFileExtension}";
    private static string ArchiveTemporaryName => $"{Guid.NewGuid()}.{CompressionFileExtension}";
    private static string ArchiveTemporaryHashName => $"{Guid.NewGuid()}.{CompressionFileExtension}";

    // Vectra Filenames
    public static string VectraArchiveFileName => $"{VectraRepository}-{ArchiveName.ToLower()}";
    public static string VectraArchiveTemporaryFileName => $"{VectraRepository}-{ArchiveTemporaryName.ToLower()}";
    public static string VectraArchiveHashFileName => $"{VectraRepository}-{ArchiveName.ToLower()}.{HashFileExtension}";
    public static string VectraArchiveTemporaryHashFileName => $"{VectraRepository}-{ArchiveTemporaryHashName.ToLower()}.{HashFileExtension}";

    // VectraCtl Filenames
    public static string VectraCtlArchiveFileName => $"{VectraCtlRepository}-{ArchiveName.ToLower()}";
    public static string VectraCtlArchiveHashFileName => $"{VectraCtlRepository}-{ArchiveName.ToLower()}.{HashFileExtension}";

    // Determine OS platform
    private static string GetOperatingSystemType()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "Linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            return "OSX";
        return "Windows";
    }

    // Determine compression extension based on OS
    private static string GetCompressionExtension()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "zip" : "tar.gz";
    }
}