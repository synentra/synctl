using System.Diagnostics;
using System.Text;
using VectraCtl.Core.Models.Docker;
using VectraCtl.Core.Services.Logger;

namespace VectraCtl.Infrastructure.Services.Docker;

public class SystemDockerProcessRunner : IDockerProcessRunner
{
    private readonly IVectraCtlLogger _logger;

    public SystemDockerProcessRunner(IVectraCtlLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected virtual string Executable => "docker";

    public async Task<DockerCommandResult> RunAsync(IEnumerable<string> arguments, bool streamOutput, CancellationToken cancellationToken)
        => await RunAsync(arguments, streamOutput, executableOverride: null, cancellationToken);

    internal async Task<DockerCommandResult> RunAsync(IEnumerable<string> arguments, bool streamOutput, string? executableOverride, CancellationToken cancellationToken)
    {
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();
        using var process = new Process();

        try
        {
            process.StartInfo.FileName = executableOverride ?? Executable;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;

            foreach (var argument in arguments)
                process.StartInfo.ArgumentList.Add(argument);

            process.OutputDataReceived += (_, data) =>
            {
                if (data.Data is null) return;
                outputBuilder.AppendLine(data.Data);
                if (streamOutput)
                    _logger.Write(data.Data);
            };

            process.ErrorDataReceived += (_, data) =>
            {
                if (data.Data is null) return;
                errorBuilder.AppendLine(data.Data);
                if (streamOutput)
                    _logger.WriteError(data.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);
            return new DockerCommandResult
            {
                ExitCode = process.ExitCode,
                Output = outputBuilder.ToString().Trim(),
                Error = errorBuilder.ToString().Trim()
            };
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            return new DockerCommandResult { ExitCode = -1, Error = "Operation cancelled." };
        }
        catch (Exception ex)
        {
            TryKill(process);
            return new DockerCommandResult { ExitCode = -1, Error = ex.Message };
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(true);
        }
        catch
        {
            // Swallow exceptions; best-effort cleanup only.
        }
    }
}
