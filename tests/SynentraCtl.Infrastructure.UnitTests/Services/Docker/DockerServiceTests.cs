using FluentAssertions;
using NSubstitute;
using VectraCtl.Core.Models.Docker;
using VectraCtl.Infrastructure.Services.Docker;

namespace VectraCtl.Infrastructure.UnitTests.Services.Docker;

public class DockerServiceTests : IDisposable
{
    private readonly IDockerProcessRunner _runner = Substitute.For<IDockerProcessRunner>();
    private readonly DockerService _sut;
    private readonly string _tempDir;

    public DockerServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _sut = new DockerService(_runner);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
        GC.SuppressFinalize(this);
    }

    // --- Constructor ---

    [Fact]
    public void Constructor_NullRunner_ThrowsArgumentNullException()
    {
        var act = () => new DockerService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("runner");
    }

    // --- GetDockerModeAsync ---

    [Theory]
    [InlineData("linux", "Linux")]
    [InlineData("windows", "Windows")]
    [InlineData("something", "Unknown")]
    public async Task GetDockerModeAsync_MapsOsTypeCorrectly(string rawOutput, string expected)
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), false, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0, Output = rawOutput });

        var result = await _sut.GetDockerModeAsync();
        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetDockerModeAsync_FailedResult_ReturnsUnknown()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), false, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 1, Output = string.Empty });

        var result = await _sut.GetDockerModeAsync();
        result.Should().Be("Unknown");
    }

    [Fact]
    public async Task GetDockerModeAsync_EmptyOutput_ReturnsUnknown()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), false, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0, Output = "   " });

        var result = await _sut.GetDockerModeAsync();
        result.Should().Be("Unknown");
    }

    // --- IsDockerAvailableAsync ---

    [Fact]
    public async Task IsDockerAvailableAsync_RunnerSucceedsWithOutput_ReturnsTrue()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), false, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0, Output = "something" });

        var result = await _sut.IsDockerAvailableAsync();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsDockerAvailableAsync_RunnerFails_ReturnsFalse()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), false, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 1, Output = string.Empty });

        var result = await _sut.IsDockerAvailableAsync();
        result.Should().BeFalse();
    }

    // --- PullImageAsync ---

    [Fact]
    public async Task PullImageAsync_NullImageName_ThrowsArgumentNullException()
    {
        var act = () => _sut.PullImageAsync(null!, "latest");
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task PullImageAsync_ValidArgs_DelegatesToRunner()
    {
        var expected = new DockerCommandResult { ExitCode = 0, Output = "pulled" };
        _runner.RunAsync(Arg.Is<IEnumerable<string>>(a => a.Contains("pull")), true, Arg.Any<CancellationToken>())
               .Returns(expected);

        var result = await _sut.PullImageAsync("myimage", "v1");
        result.Should().Be(expected);
    }

    // --- RunContainerAsync ---

    [Fact]
    public async Task RunContainerAsync_NullOptions_Throws()
    {
        var act = () => _sut.RunContainerAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData("", "tag", "name", "/host", "/container")]
    [InlineData("image", "", "name", "/host", "/container")]
    [InlineData("image", "tag", "", "/host", "/container")]
    [InlineData("image", "tag", "name", "", "/container")]
    [InlineData("image", "tag", "name", "/host", "")]
    public async Task RunContainerAsync_MissingRequiredFields_Throws(
        string image, string tag, string name, string hostPath, string containerPath)
    {
        var options = new DockerRunOptions
        {
            ImageName = image, Tag = tag, ContainerName = name,
            HostDataPath = hostPath, ContainerDataPath = containerPath
        };
        var act = () => _sut.RunContainerAsync(options);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RunContainerAsync_ValidOptions_Detached_PassesMinusD()
    {
        var hostPath = Path.Combine(_tempDir, "hostdata");
        var options = new DockerRunOptions
        {
            ImageName = "img", Tag = "v1", ContainerName = "c1",
            HostDataPath = hostPath, ContainerDataPath = "/data",
            Detached = true
        };
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), true, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0 });

        await _sut.RunContainerAsync(options);

        await _runner.Received(1).RunAsync(
            Arg.Is<IEnumerable<string>>(a => a.Contains("-d")), true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunContainerAsync_NotDetached_DoesNotPassMinusD()
    {
        var hostPath = Path.Combine(_tempDir, "hostdata2");
        var options = new DockerRunOptions
        {
            ImageName = "img", Tag = "v1", ContainerName = "c1",
            HostDataPath = hostPath, ContainerDataPath = "/data",
            Detached = false
        };
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), true, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0 });

        await _sut.RunContainerAsync(options);

        await _runner.Received(1).RunAsync(
            Arg.Is<IEnumerable<string>>(a => !a.Contains("-d")), true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunContainerAsync_WithAdditionalArguments_IncludesThemInArgs()
    {
        var hostPath = Path.Combine(_tempDir, "hostdata3");
        var options = new DockerRunOptions
        {
            ImageName = "img", Tag = "v1", ContainerName = "c1",
            HostDataPath = hostPath, ContainerDataPath = "/data",
            AdditionalArguments = "--env FOO=bar"
        };
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), true, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0 });

        await _sut.RunContainerAsync(options);

        await _runner.Received(1).RunAsync(
            Arg.Is<IEnumerable<string>>(a => a.Contains("--env") && a.Contains("FOO=bar")),
            true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunContainerAsync_EmptyAdditionalArguments_DoesNotAppendExtras()
    {
        var hostPath = Path.Combine(_tempDir, "hostdata4");
        var options = new DockerRunOptions
        {
            ImageName = "img", Tag = "v1", ContainerName = "c1",
            HostDataPath = hostPath, ContainerDataPath = "/data",
            AdditionalArguments = string.Empty
        };
        IEnumerable<string>? captured = null;
        _runner.RunAsync(Arg.Do<IEnumerable<string>>(a => captured = a.ToList()), true, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0 });

        await _sut.RunContainerAsync(options);

        // The last argument must be "img:v1" (no extras appended)
        captured.Should().NotBeNull();
        captured!.Last().Should().Be("img:v1");
    }

    // --- StartContainerAsync ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task StartContainerAsync_NullOrEmpty_Throws(string? name)
    {
        var act = () => _sut.StartContainerAsync(name!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task StartContainerAsync_ValidName_PassesStartCommand()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), true, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0 });

        await _sut.StartContainerAsync("mycontainer");

        await _runner.Received(1).RunAsync(
            Arg.Is<IEnumerable<string>>(a => a.Contains("start") && a.Contains("mycontainer")),
            true, Arg.Any<CancellationToken>());
    }

    // --- StopContainerAsync ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task StopContainerAsync_NullOrEmpty_Throws(string? name)
    {
        var act = () => _sut.StopContainerAsync(name!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task StopContainerAsync_ValidName_PassesStopCommand()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), true, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0 });

        await _sut.StopContainerAsync("mycontainer");

        await _runner.Received(1).RunAsync(
            Arg.Is<IEnumerable<string>>(a => a.Contains("stop") && a.Contains("mycontainer")),
            true, Arg.Any<CancellationToken>());
    }

    // --- RemoveContainerAsync ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task RemoveContainerAsync_NullOrEmpty_Throws(string? name)
    {
        var act = () => _sut.RemoveContainerAsync(name!, false);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RemoveContainerAsync_ForceTrue_PassesMinusF()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), true, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0 });

        await _sut.RemoveContainerAsync("mycontainer", force: true);

        await _runner.Received(1).RunAsync(
            Arg.Is<IEnumerable<string>>(a => a.Contains("-f")), true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveContainerAsync_ForceFalse_DoesNotPassMinusF()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), true, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0 });

        await _sut.RemoveContainerAsync("mycontainer", force: false);

        await _runner.Received(1).RunAsync(
            Arg.Is<IEnumerable<string>>(a => !a.Contains("-f")), true, Arg.Any<CancellationToken>());
    }

    // --- ContainerExistsAsync ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ContainerExistsAsync_NullOrEmpty_Throws(string? name)
    {
        var act = () => _sut.ContainerExistsAsync(name!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ContainerExistsAsync_RunnerReturnsOutput_ReturnsTrue()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), false, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0, Output = "abc123" });

        var result = await _sut.ContainerExistsAsync("mycontainer");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ContainerExistsAsync_RunnerReturnsEmptyOutput_ReturnsFalse()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), false, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0, Output = string.Empty });

        var result = await _sut.ContainerExistsAsync("mycontainer");
        result.Should().BeFalse();
    }

    // --- IsContainerRunningAsync ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task IsContainerRunningAsync_NullOrEmpty_Throws(string? name)
    {
        var act = () => _sut.IsContainerRunningAsync(name!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task IsContainerRunningAsync_RunnerReturnsId_ReturnsTrue()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), false, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0, Output = "containerid" });

        var result = await _sut.IsContainerRunningAsync("mycontainer");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsContainerRunningAsync_EmptyOutput_ReturnsFalse()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), false, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0, Output = string.Empty });

        var result = await _sut.IsContainerRunningAsync("mycontainer");
        result.Should().BeFalse();
    }

    // --- TailLogsAsync ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task TailLogsAsync_NullOrEmpty_Throws(string? name)
    {
        var act = () => _sut.TailLogsAsync(name!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task TailLogsAsync_ValidName_PassesLogsCommand()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), true, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0 });

        await _sut.TailLogsAsync("mycontainer");

        await _runner.Received(1).RunAsync(
            Arg.Is<IEnumerable<string>>(a => a.Contains("logs") && a.Contains("-f") && a.Contains("mycontainer")),
            true, Arg.Any<CancellationToken>());
    }
}
