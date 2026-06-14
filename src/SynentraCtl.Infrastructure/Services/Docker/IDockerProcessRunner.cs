using VectraCtl.Core.Models.Docker;

namespace VectraCtl.Infrastructure.Services.Docker;

public interface IDockerProcessRunner
{
    Task<DockerCommandResult> RunAsync(IEnumerable<string> arguments, bool streamOutput, CancellationToken cancellationToken);
}
