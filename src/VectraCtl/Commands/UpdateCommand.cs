using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.Runtime.InteropServices;
using VectraCtl.Core.Services.Extractor;
using VectraCtl.Core.Services.Github;
using VectraCtl.Core.Services.Location;
using VectraCtl.Core.Services.Logger;
using VectraCtl.Core.Services.ProcessHost;
using VectraCtl.Services.Version;

namespace VectraCtl.Commands;

internal static class UpdateCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var versionOption = new Option<string?>("--version")
        {
            Description = "Target version to install (e.g. 1.2.3). Defaults to the latest release."
        };

        var forceOption = new Option<bool>("--force")
        {
            Description = "Stop the running gateway automatically before updating"
        };

        var command = new Command("update", "Update the Vectra gateway binary to a newer version")
        {
            versionOption,
            forceOption
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var logger = serviceProvider.GetRequiredService<IVectraCtlLogger>();
            var gitHub = serviceProvider.GetRequiredService<IGitHubReleaseManager>();
            var extractor = serviceProvider.GetRequiredService<IArchiveExtractor>();
            var processHandler = serviceProvider.GetRequiredService<IProcessHandler>();
            var version = serviceProvider.GetRequiredService<IVersion>();
            var location = serviceProvider.GetRequiredService<ILocation>();

            var requestedVersion = parseResult.GetValue(versionOption);
            var force = parseResult.GetValue(forceOption);

            try
            {
                logger.Write("Checking for Vectra gateway updates...");
                await UpdateVectraAsync(
                    logger, gitHub, extractor, processHandler, version, location,
                    requestedVersion, force, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.WriteError(ex.Message);
            }
        });

        return command;
    }

    // -------------------------------------------------------------------------
    // Core update logic
    // -------------------------------------------------------------------------

    private static async Task UpdateVectraAsync(
        IVectraCtlLogger logger,
        IGitHubReleaseManager gitHub,
        IArchiveExtractor extractor,
        IProcessHandler processHandler,
        IVersion version,
        ILocation location,
        string? requestedVersion,
        bool force,
        CancellationToken cancellationToken)
    {
        var latestVersion = await gitHub.GetLatestVersion(
            GitHubSettings.Organization, GitHubSettings.VectraRepository);

        var binaryFile = location.LookupVectraBinaryFilePath(
            Path.Combine(location.DefaultVectraBinaryDirectoryName, "gateway"));

        var (isUpdateAvailable, targetVersion) = ResolveUpdate(
            version.GetVersionFromPath(binaryFile), requestedVersion, latestVersion);

        if (!isUpdateAvailable)
        {
            logger.Write("Vectra gateway is already up to date.");
            return;
        }

        logger.Write($"Update available: {targetVersion}. Preparing to install...");

        if (!processHandler.IsStopped(location.VectraBinaryName, ".", force))
        {
            logger.WriteError(
                "The Vectra gateway is still running. Stop it first or re-run with --force.");
            return;
        }

        await DownloadValidateAndExtractAsync(
            logger, gitHub, extractor, location, targetVersion, cancellationToken);

        logger.Write($"Vectra gateway updated to {targetVersion} successfully.");

        MakeExecutable(binaryFile);
    }

    // -------------------------------------------------------------------------
    // Download / validate / extract
    // -------------------------------------------------------------------------

    private static async Task<bool> DownloadValidateAndExtractAsync(
        IVectraCtlLogger logger,
        IGitHubReleaseManager gitHub,
        IArchiveExtractor extractor,
        ILocation location,
        string version,
        CancellationToken cancellationToken)
    {
        logger.Write("Downloading Vectra gateway...");

        var tempArchive = Path.Combine(Path.GetTempPath(), GitHubSettings.VectraArchiveTemporaryFileName);
        var archivePath = await gitHub.DownloadAsset(
            GitHubSettings.Organization,
            GitHubSettings.VectraRepository,
            GitHubSettings.VectraArchiveFileName,
            tempArchive,
            version);

        var tempHash = Path.Combine(Path.GetTempPath(), GitHubSettings.VectraArchiveTemporaryHashFileName);
        var hashPath = await gitHub.DownloadAsset(
            GitHubSettings.Organization,
            GitHubSettings.VectraRepository,
            GitHubSettings.VectraArchiveHashFileName,
            tempHash,
            version);

        logger.Write("Validating download integrity...");
        if (!gitHub.ValidateDownloadedAsset(archivePath, hashPath))
        {
            logger.WriteError("Integrity check failed – the downloaded archive may be corrupted.");
            return false;
        }

        logger.Write("Extracting archive...");
        ExtractAsset(extractor, location, archivePath, "gateway", cancellationToken);
        return true;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns whether an update is available and the resolved target version string.
    /// </summary>
    private static (bool isUpdateAvailable, string targetVersion) ResolveUpdate(
        string currentVersion,
        string? requestedVersion,
        string latestVersion)
    {
        var target = NormalizeVersion(
            string.IsNullOrWhiteSpace(requestedVersion) ? latestVersion : requestedVersion);

        return (IsNewerVersion(target, currentVersion), target);
    }

    /// <summary>Returns true when <paramref name="candidate"/> is strictly newer than <paramref name="current"/>.</summary>
    private static bool IsNewerVersion(string candidate, string current)
    {
        if (string.IsNullOrWhiteSpace(candidate) || string.IsNullOrWhiteSpace(current))
            return false;

        if (Version.TryParse(StripVersionPrefix(candidate), out var v1) &&
            Version.TryParse(StripVersionPrefix(current), out var v2))
        {
            return v1 > v2;
        }

        // Fall back to segment-by-segment comparison for non-standard version strings
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
    private static string StripVersionPrefix(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return string.Empty;

        var dash = version.IndexOf('-');
        if (dash >= 0)
            version = version[..dash];

        return version.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? version[1..] : version;
    }

    private static string NormalizeVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return string.Empty;

        return version.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? version : $"v{version}";
    }

    private static void ExtractAsset(
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

    private static void CopyFilesRecursively(string source, string destination, CancellationToken cancellationToken)
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

    private static void MakeExecutable(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        const UnixFileMode permissions =
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
            UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
            UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute;

        File.SetUnixFileMode(path, permissions);
    }
}
