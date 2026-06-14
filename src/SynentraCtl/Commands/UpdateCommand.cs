using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using SynentraCtl.Core.Services.Extractor;
using SynentraCtl.Core.Services.Github;
using SynentraCtl.Core.Services.Location;
using SynentraCtl.Core.Services.Logger;
using SynentraCtl.Core.Services.ProcessHost;
using SynentraCtl.Services.Version;

namespace SynentraCtl.Commands;

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

        var command = new Command("update", "Update the Synentra gateway binary to a newer version")
        {
            versionOption,
            forceOption
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var logger = serviceProvider.GetRequiredService<ISynentraCtlLogger>();
            var gitHub = serviceProvider.GetRequiredService<IGitHubReleaseManager>();
            var extractor = serviceProvider.GetRequiredService<IArchiveExtractor>();
            var processHandler = serviceProvider.GetRequiredService<IProcessHandler>();
            var version = serviceProvider.GetRequiredService<IVersion>();
            var location = serviceProvider.GetRequiredService<ILocation>();

            var requestedVersion = parseResult.GetValue(versionOption);
            var force = parseResult.GetValue(forceOption);

            try
            {
                logger.Write("Checking for Synentra gateway updates...");
                await UpdateSynentraAsync(
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

    private static async Task UpdateSynentraAsync(
        ISynentraCtlLogger logger,
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
            GitHubSettings.Organization, GitHubSettings.SynentraRepository);

        var binaryFile = location.LookupSynentraBinaryFilePath(
            Path.Combine(location.DefaultSynentraBinaryDirectoryName, "gateway"));

        var (isUpdateAvailable, targetVersion) = ResolveUpdate(
            version.GetVersionFromPath(binaryFile), requestedVersion, latestVersion);

        if (!isUpdateAvailable)
        {
            logger.Write("Synentra gateway is already up to date.");
            return;
        }

        logger.Write($"Update available: {targetVersion}. Preparing to install...");

        if (!processHandler.IsStopped(location.SynentraBinaryName, ".", force))
        {
            logger.WriteError(
                "The Synentra gateway is still running. Stop it first or re-run with --force.");
            return;
        }

        await DownloadValidateAndExtractAsync(
            logger, gitHub, extractor, location, targetVersion, cancellationToken);

        logger.Write($"Synentra gateway updated to {targetVersion} successfully.");

        CommandHelpers.MakeExecutable(binaryFile);
    }

    // -------------------------------------------------------------------------
    // Download / validate / extract
    // -------------------------------------------------------------------------

    private static async Task<bool> DownloadValidateAndExtractAsync(
        ISynentraCtlLogger logger,
        IGitHubReleaseManager gitHub,
        IArchiveExtractor extractor,
        ILocation location,
        string version,
        CancellationToken cancellationToken)
    {
        logger.Write("Downloading Synentra gateway...");

        var tempArchive = Path.Combine(Path.GetTempPath(), GitHubSettings.SynentraArchiveTemporaryFileName);
        var archivePath = await gitHub.DownloadAsset(
            GitHubSettings.Organization,
            GitHubSettings.SynentraRepository,
            GitHubSettings.SynentraArchiveFileName,
            tempArchive,
            version);

        var tempHash = Path.Combine(Path.GetTempPath(), GitHubSettings.SynentraArchiveTemporaryHashFileName);
        var hashPath = await gitHub.DownloadAsset(
            GitHubSettings.Organization,
            GitHubSettings.SynentraRepository,
            GitHubSettings.SynentraArchiveHashFileName,
            tempHash,
            version);

        logger.Write("Validating download integrity...");
        if (!gitHub.ValidateDownloadedAsset(archivePath, hashPath))
        {
            logger.WriteError("Integrity check failed – the downloaded archive may be corrupted.");
            return false;
        }

        logger.Write("Extracting archive...");
        CommandHelpers.ExtractAsset(extractor, location, archivePath, "gateway", cancellationToken);
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
        var target = CommandHelpers.NormalizeVersion(
            string.IsNullOrWhiteSpace(requestedVersion) ? latestVersion : requestedVersion);

        return (IsNewerVersion(target, currentVersion), target);
    }

    private static bool IsNewerVersion(string candidate, string current) =>
        CommandHelpers.IsNewerVersion(candidate, current);

}
