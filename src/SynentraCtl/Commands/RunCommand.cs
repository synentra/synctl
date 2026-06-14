using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.Diagnostics;
using System.Runtime.InteropServices;
using VectraCtl.Core.Models.Configuration;
using VectraCtl.Core.Models.Docker;
using VectraCtl.Core.Services.Configuration;
using VectraCtl.Core.Services.Docker;
using VectraCtl.Core.Services.Github;
using VectraCtl.Core.Services.Location;
using VectraCtl.Core.Services.Logger;

namespace VectraCtl.Commands;

internal static class RunCommand
{
    private const int GatewayContainerPort = 7080;
    private const string DefaultContainerName = "vectra-gateway";
    private const string DefaultContainerDataPath = "/app/data";
    private const string DefaultImageName = "cortexiumlabs/vectra";

    public static Command Create(IServiceProvider serviceProvider)
    {
        var dockerOption = new Option<bool>("--docker")
        {
            Description = "Run the gateway via Docker"
        };

        var backgroundOption = new Option<bool>("--background")
        {
            Description = "Start the gateway in the background (detached); do not stream output"
        };

        var command = new Command("run", "Start the Vectra gateway (binary or Docker)")
        {
            dockerOption,
            backgroundOption
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var logger = serviceProvider.GetRequiredService<IVectraCtlLogger>();
            var location = serviceProvider.GetRequiredService<ILocation>();
            var docker = serviceProvider.GetRequiredService<IDockerService>();
            var appSettings = serviceProvider.GetRequiredService<IAppSettingsService>();
            var gitHub = serviceProvider.GetRequiredService<IGitHubReleaseManager>();

            var useDocker = parseResult.GetValue(dockerOption);
            var background = parseResult.GetValue(backgroundOption);

            try
            {
                if (useDocker)
                {
                    await RunDockerAsync(
                        logger, docker, appSettings, gitHub, location,
                        background, cancellationToken);
                    return;
                }

                var settings = await appSettings.LoadAsync(cancellationToken);
                var binPath = location.DefaultVectraBinaryDirectoryName;
                var binaryFile = location.LookupVectraBinaryFilePath(binPath);

                if (settings.DeploymentMode == DeploymentMode.Docker && !File.Exists(binaryFile))
                {
                    logger.Write("Vectra is configured for Docker mode. Re-run with --docker or run 'vectractl init' to install the local binary.");
                    return;
                }

                RunBinary(logger, binPath, binaryFile, background);
            }
            catch (Exception ex)
            {
                logger.WriteError(ex.Message);
            }
        });

        return command;
    }

    // -------------------------------------------------------------------------
    // Binary run
    // -------------------------------------------------------------------------

    private static void RunBinary(
        IVectraCtlLogger logger,
        string binPath,
        string binaryFile,
        bool background)
    {
        if (!File.Exists(binaryFile))
        {
            logger.WriteError($"Vectra gateway binary not found at '{binaryFile}'. Run 'vectractl init' first.");
            return;
        }

        var startInfo = new ProcessStartInfo(binaryFile)
        {
            UseShellExecute = false,
            RedirectStandardOutput = !background,
            RedirectStandardError = !background,
            WorkingDirectory = binPath
        };

        var process = new Process { StartInfo = startInfo };

        if (!background)
        {
            process.OutputDataReceived += (_, e) => { if (e.Data is not null) logger.Write(e.Data); };
            process.ErrorDataReceived  += (_, e) => { if (e.Data is not null) logger.WriteError(e.Data); };
        }

        process.Start();

        if (!background)
        {
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }
        else
        {
            logger.Write($"Vectra gateway started in the background (PID {process.Id}).");
        }
    }

    // -------------------------------------------------------------------------
    // Docker run
    // -------------------------------------------------------------------------

    private static async Task RunDockerAsync(
        IVectraCtlLogger logger,
        IDockerService docker,
        IAppSettingsService appSettingsService,
        IGitHubReleaseManager gitHub,
        ILocation location,
        bool background,
        CancellationToken cancellationToken)
    {
        if (!await docker.IsDockerAvailableAsync(cancellationToken))
        {
            logger.WriteError("Docker is not available on this machine.");
            return;
        }

        var settings = await appSettingsService.LoadAsync(cancellationToken);
        settings.Docker ??= new DockerSettings();
        var ds = settings.Docker;

        var containerName = string.IsNullOrWhiteSpace(ds.ContainerName) ? DefaultContainerName : ds.ContainerName;
        var port = ds.Port > 0 ? ds.Port : GatewayContainerPort;
        var imageName = string.IsNullOrWhiteSpace(ds.ImageName) ? DefaultImageName : ds.ImageName;
        var hostDataPath = string.IsNullOrWhiteSpace(ds.HostDataPath)
            ? Path.Combine(location.DefaultVectraDirectoryName, "data")
            : ds.HostDataPath;
        var containerDataPath = string.IsNullOrWhiteSpace(ds.ContainerDataPath)
            ? DefaultContainerDataPath
            : ds.ContainerDataPath;

        var tag = await ResolveTagAsync(logger, docker, gitHub, ds.Tag, cancellationToken);
        if (tag is null) return;

        Directory.CreateDirectory(hostDataPath);

        var started = await EnsureContainerRunningAsync(
            logger, docker, imageName, tag, containerName, port, hostDataPath, containerDataPath, cancellationToken);
        if (!started) return;

        await PersistDockerSettingsAsync(
            appSettingsService, settings, imageName, tag, containerName, hostDataPath, containerDataPath, port, cancellationToken);

        logger.Write($"Vectra gateway is running in container '{containerName}' (port {port}).");

        if (!background)
        {
            logger.Write("Attaching to container logs (Ctrl+C to detach)...");
            await docker.TailLogsAsync(containerName, cancellationToken);
        }
    }

    private static async Task<string?> ResolveTagAsync(
        IVectraCtlLogger logger,
        IDockerService docker,
        IGitHubReleaseManager gitHub,
        string? savedTag,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(savedTag))
            return savedTag;

        var platformSuffix = await GetPlatformSuffixAsync(docker, cancellationToken);
        if (string.IsNullOrWhiteSpace(platformSuffix))
        {
            logger.WriteError("Unsupported Docker platform – only linux-amd64 and windows-ltsc2022-amd64 are supported.");
            return null;
        }

        var version = await ResolveDockerVersionAsync(gitHub);
        if (string.IsNullOrWhiteSpace(version))
        {
            logger.WriteError("Failed to resolve the Vectra image version. Specify a tag via 'vectractl init --docker' first.");
            return null;
        }

        return $"{version}-{platformSuffix}";
    }

    private static async Task<bool> EnsureContainerRunningAsync(
        IVectraCtlLogger logger,
        IDockerService docker,
        string imageName,
        string tag,
        string containerName,
        int port,
        string hostDataPath,
        string containerDataPath,
        CancellationToken cancellationToken)
    {
        if (!await docker.ContainerExistsAsync(containerName, cancellationToken))
            return await CreateAndRunContainerAsync(logger, docker, imageName, tag, containerName, port, hostDataPath, containerDataPath, cancellationToken);

        if (!await docker.IsContainerRunningAsync(containerName, cancellationToken))
            return await StartExistingContainerAsync(logger, docker, containerName, cancellationToken);

        logger.Write($"Container '{containerName}' is already running.");
        return true;
    }

    private static async Task<bool> CreateAndRunContainerAsync(
        IVectraCtlLogger logger,
        IDockerService docker,
        string imageName,
        string tag,
        string containerName,
        int port,
        string hostDataPath,
        string containerDataPath,
        CancellationToken cancellationToken)
    {
        logger.Write($"Pulling image {imageName}:{tag}...");
        var pullResult = await docker.PullImageAsync(imageName, tag, cancellationToken);
        if (!pullResult.Success)
        {
            logger.WriteError(string.IsNullOrWhiteSpace(pullResult.Error) ? "Failed to pull the Docker image." : pullResult.Error);
            return false;
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
            Detached = true
        };

        logger.Write($"Creating container '{containerName}' on port {port}...");
        var runResult = await docker.RunContainerAsync(runOptions, cancellationToken);
        if (!runResult.Success)
        {
            logger.WriteError(string.IsNullOrWhiteSpace(runResult.Error) ? "Failed to start the Docker container." : runResult.Error);
            return false;
        }

        return true;
    }

    private static async Task<bool> StartExistingContainerAsync(
        IVectraCtlLogger logger,
        IDockerService docker,
        string containerName,
        CancellationToken cancellationToken)
    {
        logger.Write($"Starting existing container '{containerName}'...");
        var startResult = await docker.StartContainerAsync(containerName, cancellationToken);
        if (!startResult.Success)
        {
            logger.WriteError(string.IsNullOrWhiteSpace(startResult.Error) ? $"Failed to start container '{containerName}'." : startResult.Error);
            return false;
        }

        return true;
    }

    private static async Task PersistDockerSettingsAsync(
        IAppSettingsService appSettingsService,
        AppSettings settings,
        string imageName,
        string tag,
        string containerName,
        string hostDataPath,
        string containerDataPath,
        int port,
        CancellationToken cancellationToken)
    {
        settings.DeploymentMode = DeploymentMode.Docker;
        settings.Docker!.ImageName = imageName;
        settings.Docker.Tag = tag;
        settings.Docker.ContainerName = containerName;
        settings.Docker.HostDataPath = hostDataPath;
        settings.Docker.ContainerDataPath = containerDataPath;
        settings.Docker.Port = port;
        await appSettingsService.SaveAsync(settings, cancellationToken);
    }
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static async Task<string> ResolveDockerVersionAsync(IGitHubReleaseManager gitHub)
    {
        var version = await gitHub.GetLatestVersion(GitHubSettings.Organization, GitHubSettings.VectraRepository);
        return NormalizeDockerVersion(version);
    }

    private static Task<string?> GetPlatformSuffixAsync(IDockerService docker, CancellationToken cancellationToken)
        => CommandHelpers.GetPlatformSuffixAsync(docker, cancellationToken);

    private static string NormalizeDockerVersion(string? version)
        => CommandHelpers.NormalizeDockerVersion(version);
}
