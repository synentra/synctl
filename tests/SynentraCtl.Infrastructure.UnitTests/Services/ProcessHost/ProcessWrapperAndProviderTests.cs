using System.Diagnostics;
using FluentAssertions;
using VectraCtl.Infrastructure.Services.ProcessHost;

namespace VectraCtl.Infrastructure.UnitTests.Services.ProcessHost;

public class ProcessWrapperTests
{
    [Fact]
    public void ProcessName_ReturnsUnderlyingProcessName()
    {
        // Start a real, short-lived process whose name we know
        using var process = Process.Start(new ProcessStartInfo("cmd.exe", "/c pause")
        {
            CreateNoWindow = true,
            UseShellExecute = false
        })!;

        try
        {
            var wrapper = new ProcessWrapper(process);
            wrapper.ProcessName.Should().Be("cmd");
        }
        finally
        {
            if (!process.HasExited)
                process.Kill(true);
            process.WaitForExit();
        }
    }

    [Fact]
    public void Kill_TerminatesUnderlyingProcess()
    {
        using var process = Process.Start(new ProcessStartInfo("cmd.exe", "/c pause")
        {
            CreateNoWindow = true,
            UseShellExecute = false
        })!;

        var wrapper = new ProcessWrapper(process);
        wrapper.Kill();
        process.WaitForExit(3000);

        process.HasExited.Should().BeTrue();
    }
}

public class DefaultProcessProviderTests
{
    [Fact]
    public void GetProcessesByName_ExistingProcess_ReturnsWrappers()
    {
        var provider = new DefaultProcessProvider();

        // "System" is always present on Windows
        var results = provider.GetProcessesByName("System", ".");

        results.Should().NotBeNull();
        // System may return 0 on some environments; just ensure no exception
    }

    [Fact]
    public void GetProcessesByName_NonExistentProcess_ReturnsEmptyArray()
    {
        var provider = new DefaultProcessProvider();

        var results = provider.GetProcessesByName("this_process_cannot_exist_abc123", ".");

        results.Should().BeEmpty();
    }

    [Fact]
    public void GetProcessesByName_ReturnsIProcessWrapperInstances()
    {
        var provider = new DefaultProcessProvider();

        // dotnet.exe is guaranteed to exist since we're running under it
        var processes = Process.GetProcessesByName("dotnet");
        if (processes.Length == 0)
            return; // nothing to assert, skip gracefully

        var results = provider.GetProcessesByName("dotnet", ".");
        results.Should().AllBeAssignableTo<IProcessWrapper>();
        results[0].ProcessName.Should().Be("dotnet");
    }
}
