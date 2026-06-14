using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
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

        CommandHelpers.MakeExecutable(binaryFile);
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
