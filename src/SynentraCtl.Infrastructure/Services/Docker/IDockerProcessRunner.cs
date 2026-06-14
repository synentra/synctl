using SynentraCtl.Core.Models.Docker;

namespace SynentraCtl.Infrastructure.Services.Docker;

public interface IDockerProcessRunner
{
    Task<DockerCommandResult> RunAsync(IEnumerable<string> arguments, bool streamOutput, CancellationToken cancellationToken);
}
