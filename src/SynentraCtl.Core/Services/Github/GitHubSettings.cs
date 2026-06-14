using System.Runtime.InteropServices;

namespace SynentraCtl.Core.Services.Github;

public static class GitHubSettings
{
    // Organization and Repository Names
    public const string Organization = "synentra";
    public const string SynentraRepository = "synentra";
    public const string SynentraCtlRepository = "synctl";

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

    // Synentra Filenames
    public static string SynentraArchiveFileName => $"{SynentraRepository}-{ArchiveName.ToLower()}";
    public static string SynentraArchiveTemporaryFileName => $"{SynentraRepository}-{ArchiveTemporaryName.ToLower()}";
    public static string SynentraArchiveHashFileName => $"{SynentraRepository}-{ArchiveName.ToLower()}.{HashFileExtension}";
    public static string SynentraArchiveTemporaryHashFileName => $"{SynentraRepository}-{ArchiveTemporaryHashName.ToLower()}.{HashFileExtension}";

    // SynCtl Filenames
    public static string SynentraCtlArchiveFileName => $"{SynentraCtlRepository}-{ArchiveName.ToLower()}";
    public static string SynentraCtlArchiveHashFileName => $"{SynentraCtlRepository}-{ArchiveName.ToLower()}.{HashFileExtension}";

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