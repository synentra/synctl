using FluentAssertions;
using NSubstitute;
using VectraCtl.Core.Services.Logger;
using VectraCtl.Infrastructure.Services.ProcessHost;

namespace VectraCtl.Infrastructure.UnitTests.Services.ProcessHost;

public class ProcessHandlerTests
{
    private readonly IVectraCtlLogger _logger = Substitute.For<IVectraCtlLogger>();
    private readonly IProcessProvider _processProvider = Substitute.For<IProcessProvider>();
    private readonly ProcessHandler _sut;

    public ProcessHandlerTests()
    {
        _sut = new ProcessHandler(_logger, _processProvider);
    }

    // --- IsRunning ---

    [Fact]
    public void IsRunning_ProcessExists_ReturnsTrue()
    {
        var process = CreateProcess("myapp");
        _processProvider.GetProcessesByName("myapp", "localhost").Returns([process]);

        _sut.IsRunning("myapp", "localhost").Should().BeTrue();
    }

    [Fact]
    public void IsRunning_NoProcesses_ReturnsFalse()
    {
        _processProvider.GetProcessesByName("myapp", "localhost").Returns([]);

        _sut.IsRunning("myapp", "localhost").Should().BeFalse();
    }

    // --- Terminate ---

    [Fact]
    public void Terminate_ProcessesExist_KillsAllProcesses()
    {
        var p1 = CreateProcess("myapp");
        var p2 = CreateProcess("myapp");
        _processProvider.GetProcessesByName("myapp", "localhost").Returns([p1, p2]);

        _sut.Terminate("myapp", "localhost");

        p1.Received(1).Kill();
        p2.Received(1).Kill();
    }

    [Fact]
    public void Terminate_NoProcesses_DoesNotKillAnything()
    {
        _processProvider.GetProcessesByName("myapp", "localhost").Returns([]);

        var act = () => _sut.Terminate("myapp", "localhost");

        act.Should().NotThrow();
    }

    [Fact]
    public void Terminate_WritesLogForEachProcess()
    {
        var p1 = CreateProcess("myapp");
        _processProvider.GetProcessesByName("myapp", "localhost").Returns([p1]);

        _sut.Terminate("myapp", "localhost");

        _logger.Received(1).Write(Arg.Is<string>(s => s.Contains("myapp")));
    }

    // --- IsStopped ---

    [Fact]
    public void IsStopped_ProcessNotRunning_ReturnsTrue()
    {
        _processProvider.GetProcessesByName("myapp", "localhost").Returns([]);

        _sut.IsStopped("myapp", "localhost", force: false).Should().BeTrue();
    }

    [Fact]
    public void IsStopped_ProcessRunning_ForceIsFalse_ReturnsFalse()
    {
        var process = CreateProcess("myapp");
        _processProvider.GetProcessesByName("myapp", "localhost").Returns([process]);

        _sut.IsStopped("myapp", "localhost", force: false).Should().BeFalse();
    }

    [Fact]
    public void IsStopped_ProcessRunning_ForceIsTrue_TerminatesAndReturnsTrue()
    {
        var process = CreateProcess("myapp");
        _processProvider.GetProcessesByName("myapp", "localhost").Returns([process], []);

        _sut.IsStopped("myapp", "localhost", force: true).Should().BeTrue();
        process.Received().Kill();
    }

    [Fact]
    public void IsStopped_MultipleProcesses_ForceIsTrue_KillsAll()
    {
        var p1 = CreateProcess("svc");
        var p2 = CreateProcess("svc");
        _processProvider.GetProcessesByName("svc", ".").Returns([p1, p2], []);

        _sut.IsStopped("svc", ".", force: true).Should().BeTrue();
        p1.Received(1).Kill();
        p2.Received(1).Kill();
    }

    // --- Helper ---

    private static IProcessWrapper CreateProcess(string name)
    {
        var wrapper = Substitute.For<IProcessWrapper>();
        wrapper.ProcessName.Returns(name);
        return wrapper;
    }
}
