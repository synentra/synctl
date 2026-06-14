using VectraCtl.Core.Models.Docker;
using VectraCtl.Core.Services.Docker;

namespace VectraCtl.Infrastructure.Services.Docker;

public class DockerService : IDockerService
{
    private static readonly string[] DockerInfoOsTypeArgs = ["info", "--format", "{{.OSType}}"];
    private static readonly string[] DockerInfoArgs = ["info"];

    private readonly IDockerProcessRunner _runner;

    public DockerService(IDockerProcessRunner runner)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    public async Task<string> GetDockerModeAsync(CancellationToken cancellationToken = default)
    {
        var result = await RunDockerAsync(DockerInfoOsTypeArgs, streamOutput: false, cancellationToken);
        if (!result.Success || string.IsNullOrWhiteSpace(result.Output))
        {
            return "Unknown";
        }

        // Docker OSType usually returns "linux" or "windows"
        var osType = result.Output.Trim().ToLowerInvariant();
        return osType switch
        {
            "linux" => "Linux",
            "windows" => "Windows",
            _ => "Unknown"
        };
    }

    public Task<bool> IsDockerAvailableAsync(CancellationToken cancellationToken = default)
    {
        return ContainerQueryAsync(DockerInfoArgs, cancellationToken);
    }

    public Task<DockerCommandResult> PullImageAsync(string imageName, string tag, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(imageName);

        var arguments = new[] { "pull", $"{imageName}:{tag}" };
        return RunDockerAsync(arguments, streamOutput: true, cancellationToken);
    }

    public Task<DockerCommandResult> RunContainerAsync(DockerRunOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ImageName))
            throw new ArgumentException("Image name is required.", nameof(options));

        if (string.IsNullOrWhiteSpace(options.Tag))
            throw new ArgumentException("Image tag is required.", nameof(options));

        if (string.IsNullOrWhiteSpace(options.ContainerName))
            throw new ArgumentException("Container name is required.", nameof(options));

        if (string.IsNullOrWhiteSpace(options.HostDataPath))
            throw new ArgumentException("Host data path is required.", nameof(options));

        if (string.IsNullOrWhiteSpace(options.ContainerDataPath))
            throw new ArgumentException("Container data path is required.", nameof(options));

        Directory.CreateDirectory(options.HostDataPath);

        var arguments = new List<string> { "run" };
        if (options.Detached)
            arguments.Add("-d");

        arguments.Add("--name");
        arguments.Add(options.ContainerName);

        arguments.Add("-p");
        arguments.Add($"{options.HostPort}:{options.ContainerPort}");

        arguments.Add("-v");
        arguments.Add($"{options.HostDataPath}:{options.ContainerDataPath}");

        arguments.Add($"{options.ImageName}:{options.Tag}");

        if (!string.IsNullOrWhiteSpace(options.AdditionalArguments))
        {
            arguments.AddRange(options.AdditionalArguments.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        return RunDockerAsync(arguments, streamOutput: true, cancellationToken);
    }

    public Task<DockerCommandResult> StartContainerAsync(string containerName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(containerName);
        return RunDockerAsync(new[] { "start", containerName }, streamOutput: true, cancellationToken);
    }

    public Task<DockerCommandResult> StopContainerAsync(string containerName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(containerName);
        return RunDockerAsync(new[] { "stop", containerName }, streamOutput: true, cancellationToken);
    }

    public Task<DockerCommandResult> RemoveContainerAsync(string containerName, bool force, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(containerName);

        var arguments = new List<string> { "rm" };
        if (force)
            arguments.Add("-f");

        arguments.Add(containerName);
        return RunDockerAsync(arguments, streamOutput: true, cancellationToken);
    }

    public Task<bool> ContainerExistsAsync(string containerName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(containerName);

        var arguments = new[]
        {
            "ps",
            "-a",
            "--filter",
            $"name={containerName}",
            "--format",
            "{{.ID}}"
        };

        return ContainerQueryAsync(arguments, cancellationToken);
    }

    public Task<bool> IsContainerRunningAsync(string containerName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(containerName);

        var arguments = new[]
        {
            "ps",
            "--filter",
            $"name={containerName}",
            "--filter",
            "status=running",
            "--format",
            "{{.ID}}"
        };

        return ContainerQueryAsync(arguments, cancellationToken);
    }

    public Task<DockerCommandResult> TailLogsAsync(string containerName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(containerName);
        return RunDockerAsync(new[] { "logs", "-f", containerName }, streamOutput: true, cancellationToken);
    }

    private async Task<bool> ContainerQueryAsync(IEnumerable<string> arguments, CancellationToken cancellationToken)
    {
        var result = await RunDockerAsync(arguments, streamOutput: false, cancellationToken);
        return result.Success && !string.IsNullOrWhiteSpace(result.Output);
    }

    private Task<DockerCommandResult> RunDockerAsync(IEnumerable<string> arguments, bool streamOutput, CancellationToken cancellationToken)
    {
        return _runner.RunAsync(arguments, streamOutput, cancellationToken);
    }
}
