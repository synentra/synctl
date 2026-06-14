using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.Runtime.InteropServices;
using VectraCtl.Core.Services.Docker;
using VectraCtl.Core.Services.Extractor;
using VectraCtl.Core.Services.Location;
using VectraCtl.Core.Services.Logger;

namespace VectraCtl.Commands;

internal static class CommandHelpers
{
    // ── Shared CLI option factories ───────────────────────────────────────────

    internal static Option<OutputType> CreateOutputOption() =>
        new Option<OutputType>("--output", "-o")
        {
            Description = "Formatting Command-Line output",
            Required = false,
            DefaultValueFactory = _ => OutputType.Json
        };

    internal static Option<int> CreatePageOption() =>
        new Option<int>("--page") { Description = "Page number", DefaultValueFactory = _ => 1 };

    internal static Option<int> CreatePageSizeOption() =>
        new Option<int>("--page-size") { Description = "Page size", DefaultValueFactory = _ => 25 };

    // ── Version comparison ────────────────────────────────────────────────────

    /// <summary>Returns true when <paramref name="candidate"/> is strictly newer than <paramref name="current"/>.</summary>
    internal static bool IsNewerVersion(string candidate, string current)
    {
        if (string.IsNullOrWhiteSpace(candidate) || string.IsNullOrWhiteSpace(current))
            return false;

        if (Version.TryParse(StripVersionPrefix(candidate), out var v1) &&
            Version.TryParse(StripVersionPrefix(current), out var v2))
        {
            return v1 > v2;
        }

        var latestParts = StripVersionPrefix(candidate).Split('.');
        var currentParts = StripVersionPrefix(current).Split('.');
        int max = Math.Max(latestParts.Length, currentParts.Length);

        for (int i = 0; i < max; i++)
        {
            int l = i < latestParts.Length && int.TryParse(latestParts[i], out var lp) ? lp : 0;
            int c = i < currentParts.Length && int.TryParse(currentParts[i], out var cp) ? cp : 0;
            if (l > c) return true;
            if (l < c) return false;
        }

        return false;
    }

    /// <summary>Strips a leading "v"/"V" and any pre-release suffix (e.g. "-beta") from a version string.</summary>
    internal static string StripVersionPrefix(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return string.Empty;

        var dash = version.IndexOf('-');
        if (dash >= 0)
            version = version[..dash];

        return version.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? version[1..] : version;
    }

    // ── Action boilerplate ────────────────────────────────────────────────────

    internal static async Task ExecuteAsync(
        IServiceProvider serviceProvider,
        Func<IVectraCtlLogger, IServiceProvider, Task> action)
    {
        var logger = serviceProvider.GetRequiredService<IVectraCtlLogger>();
        try
        {
            await action(logger, serviceProvider);
        }
        catch (Exception ex)
        {
            logger.WriteError(ex.Message);
        }
    }

    /// <summary>Ensures the version string has a leading "v" (e.g. "1.2.3" → "v1.2.3").</summary>
    internal static string NormalizeVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return string.Empty;

        return version.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? version : $"v{version}";
    }

    /// <summary>Strips the leading "v" and pre-release suffix from a version string for Docker tags (e.g. "v1.2.3-beta" → "1.2.3").</summary>
    internal static string NormalizeDockerVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return string.Empty;

        var v = version.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? version[1..] : version;
        var dash = v.IndexOf('-');
        if (dash >= 0)
            v = v[..dash];

        return v;
    }

    internal static async Task<string?> GetPlatformSuffixAsync(IDockerService docker, CancellationToken cancellationToken)
    {
        var mode = await docker.GetDockerModeAsync(cancellationToken);
        return mode == "Windows" ? "windows-ltsc2022-amd64" : "linux-amd64";
    }

    internal static void MakeExecutable(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        const UnixFileMode permissions =
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
            UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
            UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute;

        File.SetUnixFileMode(path, permissions);
    }

    internal static void CopyFilesRecursively(string source, string destination, CancellationToken cancellationToken)
    {
        foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            if (cancellationToken.IsCancellationRequested) break;
            Directory.CreateDirectory(dir.Replace(source, destination));
        }

        foreach (var file in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
        {
            if (cancellationToken.IsCancellationRequested) break;
            File.Copy(file, file.Replace(source, destination), overwrite: true);
        }
    }

    /// <summary>Extracts an archive into a subfolder under the binary directory, then copies to the destination.</summary>
    internal static void ExtractAsset(
        IArchiveExtractor extractor,
        ILocation location,
        string archivePath,
        string destinationFolder,
        CancellationToken cancellationToken)
    {
        var stagingDir = Path.Combine(location.DefaultVectraBinaryDirectoryName, destinationFolder, "downloadedFiles");
        var destDir = Path.Combine(location.DefaultVectraBinaryDirectoryName, destinationFolder);

        Directory.CreateDirectory(stagingDir);
        extractor.ExtractArchive(archivePath, stagingDir);
        Directory.CreateDirectory(destDir);
        CopyFilesRecursively(stagingDir, destDir, cancellationToken);
        Directory.Delete(stagingDir, recursive: true);
    }

    /// <summary>Extracts an archive directly into the binary directory root (no subfolder).</summary>
    internal static void ExtractAssetToRoot(
        IArchiveExtractor extractor,
        ILocation location,
        string archivePath,
        CancellationToken cancellationToken)
    {
        var stagingDir = Path.Combine(location.DefaultVectraBinaryDirectoryName, "downloadedFiles");
        var destDir = location.DefaultVectraBinaryDirectoryName;

        Directory.CreateDirectory(stagingDir);
        extractor.ExtractArchive(archivePath, stagingDir);
        Directory.CreateDirectory(destDir);
        CopyFilesRecursively(stagingDir, destDir, cancellationToken);
        Directory.Delete(stagingDir, recursive: true);
    }
}
