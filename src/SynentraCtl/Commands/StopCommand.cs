using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.Diagnostics;
using VectraCtl.Core.Models.Configuration;
using VectraCtl.Core.Services.Configuration;
using VectraCtl.Core.Services.Docker;
using VectraCtl.Core.Services.Location;
using VectraCtl.Core.Services.Logger;

namespace VectraCtl.Commands;

internal static class StopCommand
{
    private const string DefaultContainerName = "vectra-gateway";

    public static Command Create(IServiceProvider serviceProvider)
    {
        var dockerOption = new Option<bool>("--docker")
        {
            Description = "Stop the Docker-based gateway container instead of the local binary"
        };

        var command = new Command("stop", "Stop the running Vectra gateway (binary or Docker)")
        {
            dockerOption
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var logger = serviceProvider.GetRequiredService<IVectraCtlLogger>();
            var location = serviceProvider.GetRequiredService<ILocation>();
            var docker = serviceProvider.GetRequiredService<IDockerService>();
            var appSettings = serviceProvider.GetRequiredService<IAppSettingsService>();

            var useDocker = parseResult.GetValue(dockerOption);

            try
            {
                if (useDocker)
                {
                    await StopDockerAsync(logger, docker, appSettings, cancellationToken);
                    return;
                }

                // Fall back to the saved deployment mode when --docker is not supplied
                var settings = await appSettings.LoadAsync(cancellationToken);
                if (settings.DeploymentMode == DeploymentMode.Docker)
                {
                    await StopDockerAsync(logger, docker, appSettings, cancellationToken);
                    return;
                }

                StopBinary(logger, location.VectraBinaryName);
            }
            catch (Exception ex)
            {
                logger.WriteError(ex.Message);
            }
        });

        return command;
    }

    // -------------------------------------------------------------------------
    // Docker stop
    // -------------------------------------------------------------------------

    private static async Task StopDockerAsync(
        IVectraCtlLogger logger,
        IDockerService docker,
        IAppSettingsService appSettingsService,
        CancellationToken cancellationToken)
    {
        if (!await docker.IsDockerAvailableAsync(cancellationToken))
        {
            logger.WriteError("Docker is not available on this machine.");
            return;
        }

        var settings = await appSettingsService.LoadAsync(cancellationToken);
        var containerName = string.IsNullOrWhiteSpace(settings.Docker?.ContainerName)
            ? DefaultContainerName
            : settings.Docker!.ContainerName;

        if (!await docker.IsContainerRunningAsync(containerName, cancellationToken))
        {
            logger.Write($"Container '{containerName}' is not running.");
            return;
        }

        var result = await docker.StopContainerAsync(containerName, cancellationToken);
        if (!result.Success)
        {
            logger.WriteError(string.IsNullOrWhiteSpace(result.Error)
                ? $"Failed to stop container '{containerName}'."
                : result.Error);
            return;
        }

        logger.Write($"Container '{containerName}' stopped successfully.");
    }

    // -------------------------------------------------------------------------
    // Binary stop
    // -------------------------------------------------------------------------

    private static void StopBinary(IVectraCtlLogger logger, string processName)
    {
        var processes = Process.GetProcessesByName(processName);

        if (processes.Length == 0)
        {
            logger.Write($"No running '{processName}' process found.");
            return;
        }

        var killed = 0;
        foreach (var process in processes)
        {
            try
            {
                process.Kill(entireProcessTree: true);
                killed++;
            }
            catch (Exception ex)
            {
                logger.WriteError($"Failed to stop process {process.Id}: {ex.Message}");
            }
            finally
            {
                process.Dispose();
            }
        }

        if (killed > 0)
            logger.Write($"Vectra gateway stopped ({killed} process{(killed == 1 ? "" : "es")} terminated).");
    }
}
