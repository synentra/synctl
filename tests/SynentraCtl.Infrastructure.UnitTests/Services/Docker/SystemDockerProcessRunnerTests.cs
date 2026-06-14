using FluentAssertions;
using NSubstitute;
using VectraCtl.Core.Models.Docker;
using VectraCtl.Core.Services.Logger;
using VectraCtl.Infrastructure.Services.Docker;

namespace VectraCtl.Infrastructure.UnitTests.Services.Docker;

public class SystemDockerProcessRunnerTests
{
    private readonly IVectraCtlLogger _logger = Substitute.For<IVectraCtlLogger>();
    private readonly SystemDockerProcessRunner _sut;

    public SystemDockerProcessRunnerTests()
    {
        _sut = new SystemDockerProcessRunner(_logger);
    }

    // --- Constructor ---

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new SystemDockerProcessRunner(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    // --- RunAsync: non-existent executable ---

    [Fact]
    public async Task RunAsync_ExecutableNotFound_ReturnsErrorResult()
    {
        // Use a runner that targets a guaranteed-nonexistent binary so the
        // process.Start() path throws and we get ExitCode = -1
        var runner = new FakeExecutableRunner(_logger, "this_binary_absolutely_does_not_exist_xyz");

        var result = await runner.RunAsync([], streamOutput: false, CancellationToken.None);

        result.ExitCode.Should().Be(-1);
        result.Error.Should().NotBeNullOrEmpty();
    }

    // --- RunAsync: real process - stdout captured ---

    [Fact]
    public async Task RunAsync_RealProcess_CapturesStdout()
    {
        var runner = new FakeExecutableRunner(_logger, "cmd.exe", "/c", "echo hello_world");

        var result = await runner.RunAsync([], streamOutput: false, CancellationToken.None);

        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("hello_world");
    }

    // --- RunAsync: streamOutput = true - logs written ---

    [Fact]
    public async Task RunAsync_StreamOutputTrue_WritesToLogger()
    {
        var runner = new FakeExecutableRunner(_logger, "cmd.exe", "/c", "echo streamed_output");

        await runner.RunAsync([], streamOutput: true, CancellationToken.None);

        _logger.Received().Write(Arg.Is<string>(s => s.Contains("streamed_output")));
    }

    // --- RunAsync: process writes to stderr ---

    [Fact]
    public async Task RunAsync_ProcessWritesToStderr_CapturesError()
    {
        // cmd /c "echo error>&2" writes to stderr
        var runner = new FakeExecutableRunner(_logger, "cmd.exe", "/c", "echo stderr_content 1>&2");

        var result = await runner.RunAsync([], streamOutput: false, CancellationToken.None);

        result.Error.Should().Contain("stderr_content");
    }

    // --- RunAsync: streamOutput = true for stderr ---

    [Fact]
    public async Task RunAsync_StreamOutputTrue_WritesErrorToLogger()
    {
        var runner = new FakeExecutableRunner(_logger, "cmd.exe", "/c", "echo err_log 1>&2");

        await runner.RunAsync([], streamOutput: true, CancellationToken.None);

        _logger.Received().WriteError(Arg.Is<string>(s => s.Contains("err_log")));
    }

    // --- RunAsync: non-zero exit code ---

    [Fact]
    public async Task RunAsync_ProcessExitsWithNonZeroCode_ReturnsFailure()
    {
        var runner = new FakeExecutableRunner(_logger, "cmd.exe", "/c", "exit 42");

        var result = await runner.RunAsync([], streamOutput: false, CancellationToken.None);

        result.ExitCode.Should().Be(42);
        result.Success.Should().BeFalse();
    }

    // --- RunAsync: cancellation ---

    [Fact]
    public async Task RunAsync_Cancelled_ReturnsErrorResult()
    {
        using var cts = new CancellationTokenSource();
        // Start a long-running process and cancel immediately
        var runner = new FakeExecutableRunner(_logger, "cmd.exe", "/c", "ping -n 60 127.0.0.1 > nul");
        cts.CancelAfter(50);

        var result = await runner.RunAsync([], streamOutput: false, cts.Token);

        result.ExitCode.Should().Be(-1);
        result.Error.Should().Contain("cancelled");
    }

    // ─── Helper: subclass that overrides the executable ────────────────────────

    /// <summary>
    /// Derives from SystemDockerProcessRunner and invokes the internal overload
    /// with a custom executable so we can test all code paths without Docker.
    /// </summary>
    private sealed class FakeExecutableRunner : SystemDockerProcessRunner
    {
        private readonly string _exe;
        private readonly string[] _fixedArgs;

        public FakeExecutableRunner(IVectraCtlLogger logger, string exe, params string[] fixedArgs)
            : base(logger)
        {
            _exe = exe;
            _fixedArgs = fixedArgs;
        }

        public new Task<DockerCommandResult> RunAsync(
            IEnumerable<string> arguments, bool streamOutput, CancellationToken cancellationToken)
        {
            return base.RunAsync(_fixedArgs, streamOutput, executableOverride: _exe, cancellationToken);
        }
    }
}
