using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using SynentraCtl.Core.Models.Configuration;
using SynentraCtl.Core.Services.Configuration;
using SynentraCtl.Core.Services.Docker;
using SynentraCtl.Core.Services.Location;
using SynentraCtl.Core.Services.Logger;
using SynentraCtl.Core.Services.ProcessHost;

namespace SynentraCtl.Commands;

internal static class UninstallCommand
{
    private const string DefaultContainerName = "synentra-engine";

    public static Command Create(IServiceProvider serviceProvider)
    {
        var dockerOption = new Option<bool>("--docker")
        {
            Description = "Uninstall the Docker-based deployment instead of the local binary"
        };

        var forceOption = new Option<bool>("--force")
        {
            Description = "Force removal even when the engine is still running"
        };

        var removeDataOption = new Option<bool>("--remove-data")
        {
            Description = "Also delete the engine data directory (Docker mode only)"
        };

        var command = new Command("uninstall", "Remove the Synentra engine (binary or Docker)")
        {
            dockerOption,
            forceOption,
            removeDataOption
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var logger = serviceProvider.GetRequiredService<ISynentraCtlLogger>();
            var location = serviceProvider.GetRequiredService<ILocation>();
            var processHandler = serviceProvider.GetRequiredService<IProcessHandler>();
            var docker = serviceProvider.GetRequiredService<IDockerService>();
            var appSettings = serviceProvider.GetRequiredService<IAppSettingsService>();

            var useDocker = parseResult.GetValue(dockerOption);
            var force = parseResult.GetValue(forceOption);
            var removeData = parseResult.GetValue(removeDataOption);

            try
            {
                logger.Write("Uninstalling Synentra...");

                if (useDocker)
                {
                    await UninstallDockerAsync(
                        logger, docker, appSettings,
                        force, removeData, cancellationToken);
                }
                else
                {
                    UninstallBinary(logger, location, processHandler, force);
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
    // Docker uninstall
    // -------------------------------------------------------------------------

    private static async Task UninstallDockerAsync(
        ISynentraCtlLogger logger,
        IDockerService docker,
        IAppSettingsService appSettingsService,
        bool force,
        bool removeData,
        CancellationToken cancellationToken)
    {
        if (!await docker.IsDockerAvailableAsync(cancellationToken))
        {
            logger.WriteError("Docker is not available on this machine.");
            return;
        }

        var settings = await appSettingsService.LoadAsync(cancellationToken);
        settings.Docker ??= new DockerSettings();

        var containerName = string.IsNullOrWhiteSpace(settings.Docker.ContainerName)
            ? DefaultContainerName
            : settings.Docker.ContainerName;

        if (await docker.ContainerExistsAsync(containerName, cancellationToken))
        {
            logger.Write($"Removing container '{containerName}'...");
            var result = await docker.RemoveContainerAsync(containerName, force, cancellationToken);
            if (!result.Success)
            {
                logger.WriteError(string.IsNullOrWhiteSpace(result.Error)
                    ? $"Failed to remove container '{containerName}'. Use --force to forcibly stop and remove it."
                    : result.Error);
                return;
            }
        }
        else
        {
            logger.Write($"Container '{containerName}' not found – nothing to remove.");
        }

        if (removeData)
            RemoveDataDirectory(logger, settings.Docker.HostDataPath);

        settings.DeploymentMode = DeploymentMode.Binary;
        settings.Docker = new DockerSettings();
        await appSettingsService.SaveAsync(settings, cancellationToken);

        logger.Write("Synentra engine uninstalled successfully.");
    }

    // -------------------------------------------------------------------------
    // Binary uninstall
    // -------------------------------------------------------------------------

    private static void UninstallBinary(
        ISynentraCtlLogger logger,
        ILocation location,
        IProcessHandler processHandler,
        bool force)
    {
        if (processHandler.IsRunning(location.SynentraBinaryName, "."))
        {
            if (force)
            {
                processHandler.Terminate(location.SynentraBinaryName, ".");
                logger.Write("Synentra engine process terminated.");
            }
            else
            {
                logger.WriteError(
                    "The Synentra engine is still running. Stop it first or re-run with --force.");
                return;
            }
        }

        var installDir = location.DefaultSynentraDirectoryName;
        if (Directory.Exists(installDir))
        {
            Directory.Delete(installDir, recursive: true);
            logger.Write($"Removed '{installDir}'.");
        }
        else
        {
            logger.Write("Synentra engine installation directory not found – nothing to remove.");
        }

        logger.Write("Synentra engine uninstalled successfully.");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static void RemoveDataDirectory(ISynentraCtlLogger logger, string? hostDataPath)
    {
        if (string.IsNullOrWhiteSpace(hostDataPath))
            return;

        var fullPath = Path.GetFullPath(hostDataPath);
        var root = Path.GetPathRoot(fullPath);

        // Guard against accidentally deleting a filesystem root
        if (!string.IsNullOrWhiteSpace(root) &&
            string.Equals(
                fullPath.TrimEnd(Path.DirectorySeparatorChar),
                root.TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase))
        {
            logger.Write("Skipping data directory deletion – refusing to delete a filesystem root.");
            return;
        }

        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, recursive: true);
            logger.Write($"Removed data directory '{fullPath}'.");
        }
    }
}
