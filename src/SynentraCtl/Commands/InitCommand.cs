using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using SynentraCtl.Core.Models.Configuration;
using SynentraCtl.Core.Models.Docker;
using SynentraCtl.Core.Services.Configuration;
using SynentraCtl.Core.Services.Docker;
using SynentraCtl.Core.Services.Extractor;
using SynentraCtl.Core.Services.Github;
using SynentraCtl.Core.Services.Location;
using SynentraCtl.Core.Services.Logger;

namespace SynentraCtl.Commands;

internal static class InitCommand
{
    private const int GatewayContainerPort = 7080;
    private const string DefaultContainerName = "synentra-gateway";
    private const string DefaultContainerDataPath = "/app/data";
    private const string DefaultImageName = "synentra/synentra";

    public static Command Create(IServiceProvider serviceProvider)
    {
        var dockerOption = new Option<bool>("--docker")
        {
            Description = "Initialize using Docker instead of a local binary"
        };

        var versionOption = new Option<string?>("--version")
        {
            Description = "Specific version to install (e.g. 1.2.3). Defaults to the latest release."
        };

        var containerNameOption = new Option<string?>("--container-name")
        {
            Description = $"Docker container name (default: {DefaultContainerName})"
        };

        var portOption = new Option<int>("--port")
        {
            Description = $"Host port to map to the container (default: {GatewayContainerPort})",
            DefaultValueFactory = _ => 0
        };

        var mountOption = new Option<string?>("--mount")
        {
            Description = "Host path to mount as the container data directory"
        };

        var containerPathOption = new Option<string?>("--container-path")
        {
            Description = $"Container data path (default: {DefaultContainerDataPath})"
        };

        var command = new Command("init", "Initialize the Synentra gateway (binary or Docker)")
        {
            dockerOption,
            versionOption,
            containerNameOption,
            portOption,
            mountOption,
            containerPathOption
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var logger = serviceProvider.GetRequiredService<ISynentraCtlLogger>();
            var location = serviceProvider.GetRequiredService<ILocation>();
            var gitHub = serviceProvider.GetRequiredService<IGitHubReleaseManager>();
            var extractor = serviceProvider.GetRequiredService<IArchiveExtractor>();
            var docker = serviceProvider.GetRequiredService<IDockerService>();
            var appSettings = serviceProvider.GetRequiredService<IAppSettingsService>();

            var useDocker = parseResult.GetValue(dockerOption);
            var version = parseResult.GetValue(versionOption);
            var containerName = parseResult.GetValue(containerNameOption);
            var port = parseResult.GetValue(portOption);
            var mount = parseResult.GetValue(mountOption);
            var containerPath = parseResult.GetValue(containerPathOption);

            try
            {
                logger.Write("Initializing Synentra...");

                if (useDocker)
                {
                    await InitializeDockerAsync(
                        logger, docker, gitHub, appSettings, location,
                        version, containerName, port, mount, containerPath,
                        cancellationToken);
                }
                else
                {
                    await InitializeBinaryAsync(
                        logger, gitHub, extractor, appSettings, location,
                        version, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.WriteError(ex.Message);
            }
        });

        return command;
    }

    // -------------------------------------------------------------------------
    // Binary initialisation
    // -------------------------------------------------------------------------

    private static async Task InitializeBinaryAsync(
        ISynentraCtlLogger logger,
        IGitHubReleaseManager gitHub,
        IArchiveExtractor extractor,
        IAppSettingsService appSettingsService,
        ILocation location,
        string? requestedVersion,
        CancellationToken cancellationToken)
    {
        var binPath = location.DefaultSynentraBinaryDirectoryName;
        var binaryFile = location.LookupSynentraBinaryFilePath(binPath);

        if (File.Exists(binaryFile))
        {
            logger.Write("Synentra gateway is already installed.");
            return;
        }

        Directory.CreateDirectory(binPath);

        var installed = await DownloadAndExtractBinaryAsync(
            logger, gitHub, extractor, location, requestedVersion, cancellationToken);

        if (!installed)
            return;

        logger.Write("Setting executable permissions...");
        CommandHelpers.MakeExecutable(binaryFile);

        var settings = await appSettingsService.LoadAsync(cancellationToken);
        settings.DeploymentMode = DeploymentMode.Binary;
        await appSettingsService.SaveAsync(settings, cancellationToken);

        logger.Write($"Synentra gateway installed successfully to '{location.DefaultSynentraBinaryDirectoryName}'.");
    }

    private static async Task<bool> DownloadAndExtractBinaryAsync(
        ISynentraCtlLogger logger,
        IGitHubReleaseManager gitHub,
        IArchiveExtractor extractor,
        ILocation location,
        string? requestedVersion,
        CancellationToken cancellationToken)
    {
        var version = await ResolveVersion(gitHub, requestedVersion);
        if (string.IsNullOrWhiteSpace(version))
        {
            logger.WriteError("Failed to resolve the Synentra version to install.");
            return false;
        }

        logger.Write($"Downloading Synentra {version}...");

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
            logger.WriteError("Archive integrity check failed – the downloaded data may be corrupted.");
            return false;
        }

        logger.Write("Extracting archive...");
        ExtractAsset(extractor, location, archivePath, cancellationToken);
        return true;
    }

    // -------------------------------------------------------------------------
    // Docker initialisation
    // -------------------------------------------------------------------------

    private static async Task InitializeDockerAsync(
        ISynentraCtlLogger logger,
        IDockerService docker,
        IGitHubReleaseManager gitHub,
        IAppSettingsService appSettingsService,
        ILocation location,
        string? requestedVersion,
        string? containerNameOverride,
        int portOverride,
        string? mountOverride,
        string? containerPathOverride,
        CancellationToken cancellationToken)
    {
        if (!await docker.IsDockerAvailableAsync(cancellationToken))
        {
            logger.WriteError("Docker is not available on this machine. Re-run without --docker to install the local binary instead.");
            return;
        }

        var settings = await appSettingsService.LoadAsync(cancellationToken);
        settings.Docker ??= new DockerSettings();

        var containerName = string.IsNullOrWhiteSpace(containerNameOverride)
            ? DefaultContainerName
            : containerNameOverride!;

        var port = portOverride > 0 ? portOverride : GatewayContainerPort;

        var imageName = string.IsNullOrWhiteSpace(settings.Docker.ImageName)
            ? DefaultImageName
            : settings.Docker.ImageName;

        var version = await ResolveVersion(gitHub, requestedVersion);
        if (string.IsNullOrWhiteSpace(version))
        {
            logger.WriteError("Failed to resolve the Synentra image version.");
            return;
        }

        var platformSuffix = await CommandHelpers.GetPlatformSuffixAsync(docker, cancellationToken);
        if (string.IsNullOrWhiteSpace(platformSuffix))
        {
            logger.WriteError("Unsupported Docker platform – only linux-amd64 and windows-ltsc2022-amd64 are currently supported.");
            return;
        }

        var tag = $"{version}-{platformSuffix}";
        var (hostDataPath, containerDataPath) = ResolveMountPaths(
            mountOverride, containerPathOverride, settings, location);

        logger.Write($"Pulling Docker image {imageName}:{tag}...");
        var pullResult = await docker.PullImageAsync(imageName, tag, cancellationToken);
        if (!pullResult.Success)
        {
            logger.WriteError(string.IsNullOrWhiteSpace(pullResult.Error)
                ? "Failed to pull the Docker image."
                : pullResult.Error);
            return;
        }

        if (await docker.ContainerExistsAsync(containerName, cancellationToken))
        {
            logger.Write($"Removing existing container '{containerName}'...");
            await docker.RemoveContainerAsync(containerName, force: true, cancellationToken);
        }

        var runOptions = new DockerRunOptions
        {
            ImageName = imageName,
            Tag = tag,
            ContainerName = containerName,
            HostPort = port,
            ContainerPort = GatewayContainerPort,
            HostDataPath = hostDataPath,
            ContainerDataPath = containerDataPath,
            Detached = true,
            AdditionalArguments = "--start"
        };

        logger.Write($"Creating container '{containerName}' on port {port}...");
        var runResult = await docker.RunContainerAsync(runOptions, cancellationToken);
        if (!runResult.Success)
        {
            logger.WriteError(string.IsNullOrWhiteSpace(runResult.Error)
                ? "Failed to start the Docker container."
                : runResult.Error);
            return;
        }

        settings.DeploymentMode = DeploymentMode.Docker;
        settings.Docker = new DockerSettings
        {
            ImageName = imageName,
            Tag = tag,
            ContainerName = containerName,
            Port = port,
            HostDataPath = hostDataPath,
            ContainerDataPath = containerDataPath
        };

        await appSettingsService.SaveAsync(settings, cancellationToken);
        logger.Write($"Synentra gateway is running in container '{containerName}' (port {port}, data → {hostDataPath}).");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static async Task<string> ResolveVersion(IGitHubReleaseManager gitHub, string? requested)
    {
        var version = await gitHub.GetLatestVersion(GitHubSettings.Organization, GitHubSettings.SynentraRepository);

        if (!string.IsNullOrWhiteSpace(requested))
            version = requested;

        return CommandHelpers.NormalizeVersion(version);
    }

    private static (string hostDataPath, string containerDataPath) ResolveMountPaths(
        string? mountOverride,
        string? containerPathOverride,
        AppSettings settings,
        ILocation location)
    {
        var host = !string.IsNullOrWhiteSpace(mountOverride)
            ? mountOverride!
            : settings.Docker.HostDataPath;

        if (string.IsNullOrWhiteSpace(host))
            host = Path.Combine(location.DefaultSynentraDirectoryName, "data");

        var container = !string.IsNullOrWhiteSpace(containerPathOverride)
            ? containerPathOverride!
            : settings.Docker.ContainerDataPath;

        if (string.IsNullOrWhiteSpace(container))
            container = DefaultContainerDataPath;

        return (host, container);
    }

    private static void ExtractAsset(
        IArchiveExtractor extractor,
        ILocation location,
        string archivePath,
        CancellationToken cancellationToken)
    {
        CommandHelpers.ExtractAssetToRoot(extractor, location, archivePath, cancellationToken);
    }
}
