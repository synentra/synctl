using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.CommandLine;
using VectraCtl.Commands;
using VectraCtl.Core.Models.Configuration;
using VectraCtl.Core.Models.Docker;
using VectraCtl.Core.Services.Configuration;
using VectraCtl.Core.Services.Docker;
using VectraCtl.Core.Services.Extractor;
using VectraCtl.Core.Services.Github;
using VectraCtl.Core.Services.Location;
using VectraCtl.Core.Services.Logger;
using VectraCtl.Core.Services.ProcessHost;
using VectraCtl.Services.Version;

namespace VectraCtl.UnitTests.Commands;

/// <summary>
/// Tests that invoke the <see cref="Command.SetAction"/> handlers of each command so that the
/// action lambdas contribute to line/branch coverage. We rely on mock substitutes to steer
/// execution into different branches without touching the real file system or Docker daemon.
/// </summary>
public class CommandActionTests
{
    // ── Shared helpers ──────────────────────────────────────────────────────

    private static ServiceProvider BuildProvider(Action<ServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();

        services.AddSingleton(Substitute.For<IVectraCtlLogger>());
        services.AddSingleton(Substitute.For<ILocation>());
        services.AddSingleton(Substitute.For<IDockerService>());
        services.AddSingleton(Substitute.For<IAppSettingsService>());
        services.AddSingleton(Substitute.For<IGitHubReleaseManager>());
        services.AddSingleton(Substitute.For<IArchiveExtractor>());
        services.AddSingleton(Substitute.For<IProcessHandler>());
        services.AddSingleton(Substitute.For<IVersion>());

        configure?.Invoke(services);

        return services.BuildServiceProvider();
    }

    private static AppSettings DefaultSettings() => new()
    {
        Docker = new DockerSettings
        {
            ContainerName = "test-container",
            ImageName = "test-image",
            Tag = "1.0",
            Port = 7080,
            HostDataPath = "/tmp/data",
            ContainerDataPath = "/app/data"
        }
    };

    private static async Task InvokeAsync(Command command, string[] args)
    {
        await command.Parse(args).InvokeAsync();
    }

    // ── InitCommand ─────────────────────────────────────────────────────────

    [Fact]
    public async Task InitCommand_DockerUnavailable_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var docker = Substitute.For<IDockerService>();
            docker.IsDockerAvailableAsync(Arg.Any<CancellationToken>()).Returns(false);
            s.AddSingleton(docker);

            var gh = Substitute.For<IGitHubReleaseManager>();
            gh.GetLatestVersion(Arg.Any<string>(), Arg.Any<string>()).Returns("v1.0.0");
            s.AddSingleton(gh);

            var settings = Substitute.For<IAppSettingsService>();
            settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
            s.AddSingleton(settings);
        });

        var cmd = InitCommand.Create(provider);
        await InvokeAsync(cmd, ["--docker"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    [Fact]
    public async Task InitCommand_BinaryAlreadyInstalled_WritesMessage()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var binaryFile = Path.Combine(tempDir, "vectra");
        File.WriteAllText(binaryFile, "fake");

        try
        {
            var provider = BuildProvider(s =>
            {
                var loc = Substitute.For<ILocation>();
                loc.DefaultVectraBinaryDirectoryName.Returns(tempDir);
                loc.LookupVectraBinaryFilePath(Arg.Any<string>()).Returns(binaryFile);
                s.AddSingleton(loc);

                var settings = Substitute.For<IAppSettingsService>();
                settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
                s.AddSingleton(settings);

                var gh = Substitute.For<IGitHubReleaseManager>();
                gh.GetLatestVersion(Arg.Any<string>(), Arg.Any<string>()).Returns("v1.0.0");
                s.AddSingleton(gh);
            });

            var cmd = InitCommand.Create(provider);
            await InvokeAsync(cmd, []);

            provider.GetRequiredService<IVectraCtlLogger>().Received().Write(Arg.Is<string>(s => s.Contains("already installed")));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task InitCommand_BinaryGitHubFails_WritesError()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var binaryFile = Path.Combine(tempDir, "vectra");

        var provider = BuildProvider(s =>
        {
            var loc = Substitute.For<ILocation>();
            loc.DefaultVectraBinaryDirectoryName.Returns(tempDir);
            loc.LookupVectraBinaryFilePath(Arg.Any<string>()).Returns(binaryFile);
            s.AddSingleton(loc);

            var gh = Substitute.For<IGitHubReleaseManager>();
            gh.GetLatestVersion(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
            s.AddSingleton(gh);

            var settings = Substitute.For<IAppSettingsService>();
            settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
            s.AddSingleton(settings);
        });

        var cmd = InitCommand.Create(provider);
        await InvokeAsync(cmd, []);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    [Fact]
    public async Task InitCommand_DockerSuccess_PullsAndCreatesContainer()
    {
        var provider = BuildProvider(s =>
        {
            var docker = Substitute.For<IDockerService>();
            docker.IsDockerAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            docker.GetDockerModeAsync(Arg.Any<CancellationToken>()).Returns("linux");
            docker.ContainerExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
            docker.PullImageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                  .Returns(new DockerCommandResult { ExitCode = 0 });
            docker.RunContainerAsync(Arg.Any<DockerRunOptions>(), Arg.Any<CancellationToken>())
                  .Returns(new DockerCommandResult { ExitCode = 0 });
            s.AddSingleton(docker);

            var gh = Substitute.For<IGitHubReleaseManager>();
            gh.GetLatestVersion(Arg.Any<string>(), Arg.Any<string>()).Returns("v1.2.3");
            s.AddSingleton(gh);

            var settings = Substitute.For<IAppSettingsService>();
            settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
            s.AddSingleton(settings);

            var loc = Substitute.For<ILocation>();
            loc.DefaultVectraDirectoryName.Returns("/tmp/.vectra");
            s.AddSingleton(loc);
        });

        var cmd = InitCommand.Create(provider);
        await InvokeAsync(cmd, ["--docker"]);

        await provider.GetRequiredService<IDockerService>()
            .Received().PullImageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitCommand_Exception_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var gh = Substitute.For<IGitHubReleaseManager>();
            gh.GetLatestVersion(Arg.Any<string>(), Arg.Any<string>()).Throws(new Exception("network error"));
            s.AddSingleton(gh);

            var loc = Substitute.For<ILocation>();
            loc.DefaultVectraBinaryDirectoryName.Returns(Path.GetTempPath());
            loc.LookupVectraBinaryFilePath(Arg.Any<string>()).Returns(Path.Combine(Path.GetTempPath(), "no-binary"));
            s.AddSingleton(loc);

            var settings = Substitute.For<IAppSettingsService>();
            settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
            s.AddSingleton(settings);
        });

        var cmd = InitCommand.Create(provider);
        await InvokeAsync(cmd, []);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    // ── RunCommand ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RunCommand_DockerUnavailable_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var docker = Substitute.For<IDockerService>();
            docker.IsDockerAvailableAsync(Arg.Any<CancellationToken>()).Returns(false);
            s.AddSingleton(docker);

            var settings = Substitute.For<IAppSettingsService>();
            settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
            s.AddSingleton(settings);

            var gh = Substitute.For<IGitHubReleaseManager>();
            gh.GetLatestVersion(Arg.Any<string>(), Arg.Any<string>()).Returns("v1.0.0");
            s.AddSingleton(gh);
        });

        var cmd = RunCommand.Create(provider);
        await InvokeAsync(cmd, ["--docker"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    [Fact]
    public async Task RunCommand_BinaryMissing_WritesConfiguredForDocker()
    {
        var settings = new AppSettings { DeploymentMode = DeploymentMode.Docker };

        var provider = BuildProvider(s =>
        {
            var appSettings = Substitute.For<IAppSettingsService>();
            appSettings.LoadAsync(Arg.Any<CancellationToken>()).Returns(settings);
            s.AddSingleton(appSettings);

            var loc = Substitute.For<ILocation>();
            loc.DefaultVectraBinaryDirectoryName.Returns(Path.GetTempPath());
            loc.LookupVectraBinaryFilePath(Arg.Any<string>()).Returns(Path.Combine(Path.GetTempPath(), "no-binary"));
            s.AddSingleton(loc);

            var gh = Substitute.For<IGitHubReleaseManager>();
            gh.GetLatestVersion(Arg.Any<string>(), Arg.Any<string>()).Returns("v1.0.0");
            s.AddSingleton(gh);
        });

        var cmd = RunCommand.Create(provider);
        await InvokeAsync(cmd, []);

        provider.GetRequiredService<IVectraCtlLogger>().Received().Write(Arg.Is<string>(s => s.Contains("Docker")));
    }

    [Fact]
    public async Task RunCommand_DockerContainerAlreadyRunning_WritesRunning()
    {
        var provider = BuildProvider(s =>
        {
            var docker = Substitute.For<IDockerService>();
            docker.IsDockerAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            docker.GetDockerModeAsync(Arg.Any<CancellationToken>()).Returns("linux");
            docker.ContainerExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
            docker.IsContainerRunningAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
            s.AddSingleton(docker);

            var settings = Substitute.For<IAppSettingsService>();
            settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
            s.AddSingleton(settings);

            var gh = Substitute.For<IGitHubReleaseManager>();
            gh.GetLatestVersion(Arg.Any<string>(), Arg.Any<string>()).Returns("v1.0.0");
            s.AddSingleton(gh);

            var loc = Substitute.For<ILocation>();
            loc.DefaultVectraDirectoryName.Returns(Path.GetTempPath());
            s.AddSingleton(loc);
        });

        var cmd = RunCommand.Create(provider);
        await InvokeAsync(cmd, ["--docker", "--background"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().Write(Arg.Is<string>(s => s.Contains("already running")));
    }

    [Fact]
    public async Task RunCommand_DockerContainerExistsNotRunning_StartsContainer()
    {
        var provider = BuildProvider(s =>
        {
            var docker = Substitute.For<IDockerService>();
            docker.IsDockerAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            docker.GetDockerModeAsync(Arg.Any<CancellationToken>()).Returns("linux");
            docker.ContainerExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
            docker.IsContainerRunningAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
            docker.StartContainerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                  .Returns(new DockerCommandResult { ExitCode = 0 });
            s.AddSingleton(docker);

            var settings = Substitute.For<IAppSettingsService>();
            settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
            s.AddSingleton(settings);

            var gh = Substitute.For<IGitHubReleaseManager>();
            gh.GetLatestVersion(Arg.Any<string>(), Arg.Any<string>()).Returns("v1.0.0");
            s.AddSingleton(gh);

            var loc = Substitute.For<ILocation>();
            loc.DefaultVectraDirectoryName.Returns(Path.GetTempPath());
            s.AddSingleton(loc);
        });

        var cmd = RunCommand.Create(provider);
        await InvokeAsync(cmd, ["--docker", "--background"]);

        await provider.GetRequiredService<IDockerService>()
            .Received().StartContainerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunCommand_Exception_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var appSettings = Substitute.For<IAppSettingsService>();
            appSettings.LoadAsync(Arg.Any<CancellationToken>()).Throws(new Exception("boom"));
            s.AddSingleton(appSettings);

            var loc = Substitute.For<ILocation>();
            loc.DefaultVectraBinaryDirectoryName.Returns(Path.GetTempPath());
            loc.LookupVectraBinaryFilePath(Arg.Any<string>()).Returns(Path.Combine(Path.GetTempPath(), "no-binary"));
            s.AddSingleton(loc);
        });

        var cmd = RunCommand.Create(provider);
        await InvokeAsync(cmd, []);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    // ── StopCommand ─────────────────────────────────────────────────────────

    [Fact]
    public async Task StopCommand_DockerUnavailable_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var docker = Substitute.For<IDockerService>();
            docker.IsDockerAvailableAsync(Arg.Any<CancellationToken>()).Returns(false);
            s.AddSingleton(docker);

            var settings = Substitute.For<IAppSettingsService>();
            settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
            s.AddSingleton(settings);
        });

        var cmd = StopCommand.Create(provider);
        await InvokeAsync(cmd, ["--docker"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    [Fact]
    public async Task StopCommand_DockerContainerNotRunning_WritesNotRunning()
    {
        var provider = BuildProvider(s =>
        {
            var docker = Substitute.For<IDockerService>();
            docker.IsDockerAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            docker.IsContainerRunningAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
            s.AddSingleton(docker);

            var settings = Substitute.For<IAppSettingsService>();
            settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
            s.AddSingleton(settings);
        });

        var cmd = StopCommand.Create(provider);
        await InvokeAsync(cmd, ["--docker"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().Write(Arg.Is<string>(s => s.Contains("not running")));
    }

    [Fact]
    public async Task StopCommand_DockerStopSuccess_WritesSuccess()
    {
        var provider = BuildProvider(s =>
        {
            var docker = Substitute.For<IDockerService>();
            docker.IsDockerAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            docker.IsContainerRunningAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
            docker.StopContainerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                  .Returns(new DockerCommandResult { ExitCode = 0 });
            s.AddSingleton(docker);

            var settings = Substitute.For<IAppSettingsService>();
            settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
            s.AddSingleton(settings);
        });

        var cmd = StopCommand.Create(provider);
        await InvokeAsync(cmd, ["--docker"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().Write(Arg.Is<string>(s => s.Contains("stopped successfully")));
    }

    [Fact]
    public async Task StopCommand_DockerStopFails_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var docker = Substitute.For<IDockerService>();
            docker.IsDockerAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            docker.IsContainerRunningAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
            docker.StopContainerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                  .Returns(new DockerCommandResult { ExitCode = 1, Error = "fail" });
            s.AddSingleton(docker);

            var settings = Substitute.For<IAppSettingsService>();
            settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
            s.AddSingleton(settings);
        });

        var cmd = StopCommand.Create(provider);
        await InvokeAsync(cmd, ["--docker"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    [Fact]
    public async Task StopCommand_BinaryMode_StopsBinary()
    {
        var settings = new AppSettings { DeploymentMode = DeploymentMode.Binary };

        var provider = BuildProvider(s =>
        {
            var appSettings = Substitute.For<IAppSettingsService>();
            appSettings.LoadAsync(Arg.Any<CancellationToken>()).Returns(settings);
            s.AddSingleton(appSettings);

            var loc = Substitute.For<ILocation>();
            loc.VectraBinaryName.Returns("vectra-nonexistent-xyz");
            s.AddSingleton(loc);
        });

        var cmd = StopCommand.Create(provider);
        // No --docker flag, binary mode — no running process expected
        await InvokeAsync(cmd, []);

        provider.GetRequiredService<IVectraCtlLogger>().Received().Write(Arg.Any<string>());
    }

    [Fact]
    public async Task StopCommand_Exception_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var appSettings = Substitute.For<IAppSettingsService>();
            appSettings.LoadAsync(Arg.Any<CancellationToken>()).Throws(new Exception("oops"));
            s.AddSingleton(appSettings);
        });

        var cmd = StopCommand.Create(provider);
        await InvokeAsync(cmd, []);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    // ── UninstallCommand ────────────────────────────────────────────────────

    [Fact]
    public async Task UninstallCommand_DockerUnavailable_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var docker = Substitute.For<IDockerService>();
            docker.IsDockerAvailableAsync(Arg.Any<CancellationToken>()).Returns(false);
            s.AddSingleton(docker);

            var settings = Substitute.For<IAppSettingsService>();
            settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
            s.AddSingleton(settings);
        });

        var cmd = UninstallCommand.Create(provider);
        await InvokeAsync(cmd, ["--docker"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    [Fact]
    public async Task UninstallCommand_DockerContainerNotFound_WritesNothing()
    {
        var provider = BuildProvider(s =>
        {
            var docker = Substitute.For<IDockerService>();
            docker.IsDockerAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            docker.ContainerExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
            s.AddSingleton(docker);

            var settings = Substitute.For<IAppSettingsService>();
            settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
            s.AddSingleton(settings);
        });

        var cmd = UninstallCommand.Create(provider);
        await InvokeAsync(cmd, ["--docker"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().Write(Arg.Is<string>(s => s.Contains("not found")));
    }

    [Fact]
    public async Task UninstallCommand_DockerRemoveSuccess_WritesSuccess()
    {
        var provider = BuildProvider(s =>
        {
            var docker = Substitute.For<IDockerService>();
            docker.IsDockerAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            docker.ContainerExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
            docker.RemoveContainerAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                  .Returns(new DockerCommandResult { ExitCode = 0 });
            s.AddSingleton(docker);

            var settings = Substitute.For<IAppSettingsService>();
            settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
            s.AddSingleton(settings);
        });

        var cmd = UninstallCommand.Create(provider);
        await InvokeAsync(cmd, ["--docker"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().Write(Arg.Is<string>(s => s.Contains("uninstalled successfully")));
    }

    [Fact]
    public async Task UninstallCommand_BinaryNotRunning_RemovesDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var provider = BuildProvider(s =>
            {
                var ph = Substitute.For<IProcessHandler>();
                ph.IsRunning(Arg.Any<string>(), Arg.Any<string>()).Returns(false);
                s.AddSingleton(ph);

                var loc = Substitute.For<ILocation>();
                loc.VectraBinaryName.Returns("vectra");
                loc.DefaultVectraDirectoryName.Returns(tempDir);
                s.AddSingleton(loc);

                var settings = Substitute.For<IAppSettingsService>();
                settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
                s.AddSingleton(settings);
            });

            var cmd = UninstallCommand.Create(provider);
            await InvokeAsync(cmd, []);

            Directory.Exists(tempDir).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task UninstallCommand_BinaryRunning_Force_Terminates()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        var provider = BuildProvider(s =>
        {
            var ph = Substitute.For<IProcessHandler>();
            ph.IsRunning(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
            s.AddSingleton(ph);

            var loc = Substitute.For<ILocation>();
            loc.VectraBinaryName.Returns("vectra");
            loc.DefaultVectraDirectoryName.Returns(tempDir);
            s.AddSingleton(loc);

            var settings = Substitute.For<IAppSettingsService>();
            settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
            s.AddSingleton(settings);
        });

        var cmd = UninstallCommand.Create(provider);
        await InvokeAsync(cmd, ["--force"]);

        provider.GetRequiredService<IProcessHandler>().Received().Terminate(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task UninstallCommand_BinaryRunning_NoForce_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var ph = Substitute.For<IProcessHandler>();
            ph.IsRunning(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
            s.AddSingleton(ph);

            var loc = Substitute.For<ILocation>();
            loc.VectraBinaryName.Returns("vectra");
            s.AddSingleton(loc);

            var settings = Substitute.For<IAppSettingsService>();
            settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
            s.AddSingleton(settings);
        });

        var cmd = UninstallCommand.Create(provider);
        await InvokeAsync(cmd, []);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    [Fact]
    public async Task UninstallCommand_Exception_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var appSettings = Substitute.For<IAppSettingsService>();
            appSettings.LoadAsync(Arg.Any<CancellationToken>()).Throws(new Exception("disk error"));
            s.AddSingleton(appSettings);

            var ph = Substitute.For<IProcessHandler>();
            ph.IsRunning(Arg.Any<string>(), Arg.Any<string>()).Returns(false);
            s.AddSingleton(ph);

            var loc = Substitute.For<ILocation>();
            loc.VectraBinaryName.Returns("vectra");
            loc.DefaultVectraDirectoryName.Returns(Path.GetTempPath());
            s.AddSingleton(loc);
        });

        var cmd = UninstallCommand.Create(provider);
        await InvokeAsync(cmd, ["--docker"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    // ── UpdateCommand ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateCommand_AlreadyUpToDate_WritesUpToDate()
    {
        var provider = BuildProvider(s =>
        {
            var gh = Substitute.For<IGitHubReleaseManager>();
            gh.GetLatestVersion(Arg.Any<string>(), Arg.Any<string>()).Returns("v1.0.0");
            s.AddSingleton(gh);

            var ver = Substitute.For<IVersion>();
            ver.GetVersionFromPath(Arg.Any<string>()).Returns("1.0.0");
            s.AddSingleton(ver);

            var loc = Substitute.For<ILocation>();
            loc.DefaultVectraBinaryDirectoryName.Returns(Path.GetTempPath());
            loc.LookupVectraBinaryFilePath(Arg.Any<string>()).Returns(Path.Combine(Path.GetTempPath(), "vectra"));
            s.AddSingleton(loc);

            var ph = Substitute.For<IProcessHandler>();
            ph.IsStopped(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);
            s.AddSingleton(ph);
        });

        var cmd = UpdateCommand.Create(provider);
        await InvokeAsync(cmd, []);

        provider.GetRequiredService<IVectraCtlLogger>().Received().Write(Arg.Is<string>(s => s.Contains("up to date")));
    }

    [Fact]
    public async Task UpdateCommand_GatewayStillRunning_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var gh = Substitute.For<IGitHubReleaseManager>();
            gh.GetLatestVersion(Arg.Any<string>(), Arg.Any<string>()).Returns("v2.0.0");
            s.AddSingleton(gh);

            var ver = Substitute.For<IVersion>();
            ver.GetVersionFromPath(Arg.Any<string>()).Returns("1.0.0");
            s.AddSingleton(ver);

            var ph = Substitute.For<IProcessHandler>();
            ph.IsStopped(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(false);
            s.AddSingleton(ph);

            var loc = Substitute.For<ILocation>();
            loc.DefaultVectraBinaryDirectoryName.Returns(Path.GetTempPath());
            loc.LookupVectraBinaryFilePath(Arg.Any<string>()).Returns(Path.Combine(Path.GetTempPath(), "vectra"));
            loc.VectraBinaryName.Returns("vectra");
            s.AddSingleton(loc);
        });

        var cmd = UpdateCommand.Create(provider);
        await InvokeAsync(cmd, []);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    [Fact]
    public async Task UpdateCommand_Exception_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var gh = Substitute.For<IGitHubReleaseManager>();
            gh.GetLatestVersion(Arg.Any<string>(), Arg.Any<string>()).Throws(new Exception("timeout"));
            s.AddSingleton(gh);

            var ver = Substitute.For<IVersion>();
            ver.GetVersionFromPath(Arg.Any<string>()).Returns("1.0.0");
            s.AddSingleton(ver);

            var loc = Substitute.For<ILocation>();
            loc.DefaultVectraBinaryDirectoryName.Returns(Path.GetTempPath());
            loc.LookupVectraBinaryFilePath(Arg.Any<string>()).Returns(Path.Combine(Path.GetTempPath(), "vectra"));
            s.AddSingleton(loc);
        });

        var cmd = UpdateCommand.Create(provider);
        await InvokeAsync(cmd, []);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    [Fact]
    public async Task UpdateCommand_HashValidationFails_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var gh = Substitute.For<IGitHubReleaseManager>();
            gh.GetLatestVersion(Arg.Any<string>(), Arg.Any<string>()).Returns("v2.0.0");
            gh.DownloadAsset(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
              .Returns(x => (string)x[3]); // return the dest path
            gh.ValidateDownloadedAsset(Arg.Any<string>(), Arg.Any<string>()).Returns(false);
            s.AddSingleton(gh);

            var ver = Substitute.For<IVersion>();
            ver.GetVersionFromPath(Arg.Any<string>()).Returns("1.0.0");
            s.AddSingleton(ver);

            var ph = Substitute.For<IProcessHandler>();
            ph.IsStopped(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()).Returns(true);
            s.AddSingleton(ph);

            var loc = Substitute.For<ILocation>();
            loc.DefaultVectraBinaryDirectoryName.Returns(Path.GetTempPath());
            loc.LookupVectraBinaryFilePath(Arg.Any<string>()).Returns(Path.Combine(Path.GetTempPath(), "vectra"));
            loc.VectraBinaryName.Returns("vectra");
            s.AddSingleton(loc);
        });

        var cmd = UpdateCommand.Create(provider);
        await InvokeAsync(cmd, []);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    // ── InitCommand extra branches ───────────────────────────────────────────

    [Fact]
    public async Task InitCommand_Docker_PullFails_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var docker = Substitute.For<IDockerService>();
            docker.IsDockerAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            docker.GetDockerModeAsync(Arg.Any<CancellationToken>()).Returns("linux");
            docker.ContainerExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
            docker.PullImageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                  .Returns(new DockerCommandResult { ExitCode = 1, Error = "pull failed" });
            s.AddSingleton(docker);

            var gh = Substitute.For<IGitHubReleaseManager>();
            gh.GetLatestVersion(Arg.Any<string>(), Arg.Any<string>()).Returns("v1.2.3");
            s.AddSingleton(gh);

            var settings = Substitute.For<IAppSettingsService>();
            settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
            s.AddSingleton(settings);

            var loc = Substitute.For<ILocation>();
            loc.DefaultVectraDirectoryName.Returns(Path.GetTempPath());
            s.AddSingleton(loc);
        });

        var cmd = InitCommand.Create(provider);
        await InvokeAsync(cmd, ["--docker"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    [Fact]
    public async Task InitCommand_Docker_PullFails_NoErrorMessage_WritesGenericError()
    {
        var provider = BuildProvider(s =>
        {
            var docker = Substitute.For<IDockerService>();
            docker.IsDockerAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            docker.GetDockerModeAsync(Arg.Any<CancellationToken>()).Returns("linux");
            docker.ContainerExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
            docker.PullImageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                  .Returns(new DockerCommandResult { ExitCode = 1, Error = "" });
            s.AddSingleton(docker);

            var gh = Substitute.For<IGitHubReleaseManager>();
            gh.GetLatestVersion(Arg.Any<string>(), Arg.Any<string>()).Returns("v1.2.3");
            s.AddSingleton(gh);

            var settings = Substitute.For<IAppSettingsService>();
            settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
            s.AddSingleton(settings);

            var loc = Substitute.For<ILocation>();
            loc.DefaultVectraDirectoryName.Returns(Path.GetTempPath());
            s.AddSingleton(loc);
        });

        var cmd = InitCommand.Create(provider);
        await InvokeAsync(cmd, ["--docker"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    [Fact]
    public async Task InitCommand_Docker_ContainerExists_RemovesThenRuns()
    {
        var provider = BuildProvider(s =>
        {
            var docker = Substitute.For<IDockerService>();
            docker.IsDockerAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            docker.GetDockerModeAsync(Arg.Any<CancellationToken>()).Returns("linux");
            docker.ContainerExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
            docker.RemoveContainerAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                  .Returns(new DockerCommandResult { ExitCode = 0 });
            docker.PullImageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                  .Returns(new DockerCommandResult { ExitCode = 0 });
            docker.RunContainerAsync(Arg.Any<DockerRunOptions>(), Arg.Any<CancellationToken>())
                  .Returns(new DockerCommandResult { ExitCode = 0 });
            s.AddSingleton(docker);

            var gh = Substitute.For<IGitHubReleaseManager>();
            gh.GetLatestVersion(Arg.Any<string>(), Arg.Any<string>()).Returns("v1.2.3");
            s.AddSingleton(gh);

            var settings = Substitute.For<IAppSettingsService>();
            settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
            s.AddSingleton(settings);

            var loc = Substitute.For<ILocation>();
            loc.DefaultVectraDirectoryName.Returns(Path.GetTempPath());
            s.AddSingleton(loc);
        });

        var cmd = InitCommand.Create(provider);
        await InvokeAsync(cmd, ["--docker"]);

        await provider.GetRequiredService<IDockerService>()
            .Received().RemoveContainerAsync(Arg.Any<string>(), true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitCommand_Docker_RunContainerFails_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var docker = Substitute.For<IDockerService>();
            docker.IsDockerAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            docker.GetDockerModeAsync(Arg.Any<CancellationToken>()).Returns("linux");
            docker.ContainerExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
            docker.PullImageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                  .Returns(new DockerCommandResult { ExitCode = 0 });
            docker.RunContainerAsync(Arg.Any<DockerRunOptions>(), Arg.Any<CancellationToken>())
                  .Returns(new DockerCommandResult { ExitCode = 1, Error = "run error" });
            s.AddSingleton(docker);

            var gh = Substitute.For<IGitHubReleaseManager>();
            gh.GetLatestVersion(Arg.Any<string>(), Arg.Any<string>()).Returns("v1.2.3");
            s.AddSingleton(gh);

            var settings = Substitute.For<IAppSettingsService>();
            settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
            s.AddSingleton(settings);

            var loc = Substitute.For<ILocation>();
            loc.DefaultVectraDirectoryName.Returns(Path.GetTempPath());
            s.AddSingleton(loc);
        });

        var cmd = InitCommand.Create(provider);
        await InvokeAsync(cmd, ["--docker"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    [Fact]
    public async Task InitCommand_Binary_HashValidationFails_WritesError()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            var provider = BuildProvider(s =>
            {
                var loc = Substitute.For<ILocation>();
                loc.DefaultVectraBinaryDirectoryName.Returns(tempDir);
                loc.LookupVectraBinaryFilePath(Arg.Any<string>()).Returns(Path.Combine(tempDir, "vectra"));
                s.AddSingleton(loc);

                var gh = Substitute.For<IGitHubReleaseManager>();
                gh.GetLatestVersion(Arg.Any<string>(), Arg.Any<string>()).Returns("v1.0.0");
                gh.DownloadAsset(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                  .Returns(x => (string)x[3]);
                gh.ValidateDownloadedAsset(Arg.Any<string>(), Arg.Any<string>()).Returns(false);
                s.AddSingleton(gh);

                var settings = Substitute.For<IAppSettingsService>();
                settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
                s.AddSingleton(settings);
            });

            var cmd = InitCommand.Create(provider);
            await InvokeAsync(cmd, []);

            provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    // ── RunCommand extra branches ────────────────────────────────────────────

    [Fact]
    public async Task RunCommand_Docker_NewContainer_PullFails_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var docker = Substitute.For<IDockerService>();
            docker.IsDockerAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            docker.GetDockerModeAsync(Arg.Any<CancellationToken>()).Returns("linux");
            docker.ContainerExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
            docker.PullImageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                  .Returns(new DockerCommandResult { ExitCode = 1, Error = "pull error" });
            s.AddSingleton(docker);

            var settings = Substitute.For<IAppSettingsService>();
            settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
            s.AddSingleton(settings);

            var gh = Substitute.For<IGitHubReleaseManager>();
            gh.GetLatestVersion(Arg.Any<string>(), Arg.Any<string>()).Returns("v1.0.0");
            s.AddSingleton(gh);

            var loc = Substitute.For<ILocation>();
            loc.DefaultVectraDirectoryName.Returns(Path.GetTempPath());
            s.AddSingleton(loc);
        });

        var cmd = RunCommand.Create(provider);
        await InvokeAsync(cmd, ["--docker", "--background"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    [Fact]
    public async Task RunCommand_Docker_NewContainer_RunFails_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var docker = Substitute.For<IDockerService>();
            docker.IsDockerAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            docker.GetDockerModeAsync(Arg.Any<CancellationToken>()).Returns("linux");
            docker.ContainerExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
            docker.PullImageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                  .Returns(new DockerCommandResult { ExitCode = 0 });
            docker.RunContainerAsync(Arg.Any<DockerRunOptions>(), Arg.Any<CancellationToken>())
                  .Returns(new DockerCommandResult { ExitCode = 1, Error = "" });
            s.AddSingleton(docker);

            var settings = Substitute.For<IAppSettingsService>();
            settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
            s.AddSingleton(settings);

            var gh = Substitute.For<IGitHubReleaseManager>();
            gh.GetLatestVersion(Arg.Any<string>(), Arg.Any<string>()).Returns("v1.0.0");
            s.AddSingleton(gh);

            var loc = Substitute.For<ILocation>();
            loc.DefaultVectraDirectoryName.Returns(Path.GetTempPath());
            s.AddSingleton(loc);
        });

        var cmd = RunCommand.Create(provider);
        await InvokeAsync(cmd, ["--docker", "--background"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    [Fact]
    public async Task RunCommand_Docker_StartFails_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var docker = Substitute.For<IDockerService>();
            docker.IsDockerAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            docker.GetDockerModeAsync(Arg.Any<CancellationToken>()).Returns("linux");
            docker.ContainerExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
            docker.IsContainerRunningAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
            docker.StartContainerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                  .Returns(new DockerCommandResult { ExitCode = 1, Error = "start error" });
            s.AddSingleton(docker);

            var settings = Substitute.For<IAppSettingsService>();
            settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
            s.AddSingleton(settings);

            var gh = Substitute.For<IGitHubReleaseManager>();
            gh.GetLatestVersion(Arg.Any<string>(), Arg.Any<string>()).Returns("v1.0.0");
            s.AddSingleton(gh);

            var loc = Substitute.For<ILocation>();
            loc.DefaultVectraDirectoryName.Returns(Path.GetTempPath());
            s.AddSingleton(loc);
        });

        var cmd = RunCommand.Create(provider);
        await InvokeAsync(cmd, ["--docker", "--background"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    [Fact]
    public async Task RunCommand_Docker_VersionResolveFails_WritesError()
    {
        var settingsWithNoTag = new AppSettings
        {
            Docker = new DockerSettings { ContainerName = "c", ImageName = "img", Tag = "" }
        };

        var provider = BuildProvider(s =>
        {
            var docker = Substitute.For<IDockerService>();
            docker.IsDockerAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            docker.GetDockerModeAsync(Arg.Any<CancellationToken>()).Returns("linux");
            s.AddSingleton(docker);

            var settings = Substitute.For<IAppSettingsService>();
            settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(settingsWithNoTag);
            s.AddSingleton(settings);

            var gh = Substitute.For<IGitHubReleaseManager>();
            gh.GetLatestVersion(Arg.Any<string>(), Arg.Any<string>()).Returns(string.Empty);
            s.AddSingleton(gh);

            var loc = Substitute.For<ILocation>();
            loc.DefaultVectraDirectoryName.Returns(Path.GetTempPath());
            s.AddSingleton(loc);
        });

        var cmd = RunCommand.Create(provider);
        await InvokeAsync(cmd, ["--docker", "--background"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    // ── StopCommand extra branches ───────────────────────────────────────────

    [Fact]
    public async Task StopCommand_NoFlag_DockerMode_UsesDockerPath()
    {
        var settings = new AppSettings
        {
            DeploymentMode = DeploymentMode.Docker,
            Docker = new DockerSettings { ContainerName = "test-container" }
        };

        var provider = BuildProvider(s =>
        {
            var docker = Substitute.For<IDockerService>();
            docker.IsDockerAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            docker.IsContainerRunningAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
            s.AddSingleton(docker);

            var appSettings = Substitute.For<IAppSettingsService>();
            appSettings.LoadAsync(Arg.Any<CancellationToken>()).Returns(settings);
            s.AddSingleton(appSettings);
        });

        var cmd = StopCommand.Create(provider);
        await InvokeAsync(cmd, []); // no --docker flag, but mode is Docker

        await provider.GetRequiredService<IDockerService>()
            .Received().IsDockerAvailableAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StopCommand_DockerStop_NoErrorMessage_WritesGenericError()
    {
        var provider = BuildProvider(s =>
        {
            var docker = Substitute.For<IDockerService>();
            docker.IsDockerAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            docker.IsContainerRunningAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
            docker.StopContainerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                  .Returns(new DockerCommandResult { ExitCode = 1, Error = "" });
            s.AddSingleton(docker);

            var settings = Substitute.For<IAppSettingsService>();
            settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
            s.AddSingleton(settings);
        });

        var cmd = StopCommand.Create(provider);
        await InvokeAsync(cmd, ["--docker"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    // ── UninstallCommand extra branches ──────────────────────────────────────

    [Fact]
    public async Task UninstallCommand_Docker_RemoveFails_WritesError()
    {
        var provider = BuildProvider(s =>
        {
            var docker = Substitute.For<IDockerService>();
            docker.IsDockerAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            docker.ContainerExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
            docker.RemoveContainerAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                  .Returns(new DockerCommandResult { ExitCode = 1, Error = "remove error" });
            s.AddSingleton(docker);

            var settings = Substitute.For<IAppSettingsService>();
            settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
            s.AddSingleton(settings);
        });

        var cmd = UninstallCommand.Create(provider);
        await InvokeAsync(cmd, ["--docker"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    [Fact]
    public async Task UninstallCommand_Docker_RemoveFails_NoErrorMessage_WritesGenericError()
    {
        var provider = BuildProvider(s =>
        {
            var docker = Substitute.For<IDockerService>();
            docker.IsDockerAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            docker.ContainerExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
            docker.RemoveContainerAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                  .Returns(new DockerCommandResult { ExitCode = 1, Error = "" });
            s.AddSingleton(docker);

            var settings = Substitute.For<IAppSettingsService>();
            settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
            s.AddSingleton(settings);
        });

        var cmd = UninstallCommand.Create(provider);
        await InvokeAsync(cmd, ["--docker"]);

        provider.GetRequiredService<IVectraCtlLogger>().Received().WriteError(Arg.Any<string>());
    }

    [Fact]
    public async Task UninstallCommand_Docker_RemoveData_RemovesDataDirectory()
    {
        var tempDataDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDataDir);

        try
        {
            var settingsWithData = new AppSettings
            {
                Docker = new DockerSettings
                {
                    ContainerName = "test-c",
                    HostDataPath = tempDataDir
                }
            };

            var provider = BuildProvider(s =>
            {
                var docker = Substitute.For<IDockerService>();
                docker.IsDockerAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
                docker.ContainerExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
                s.AddSingleton(docker);

                var settings = Substitute.For<IAppSettingsService>();
                settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(settingsWithData);
                s.AddSingleton(settings);
            });

            var cmd = UninstallCommand.Create(provider);
            await InvokeAsync(cmd, ["--docker", "--remove-data"]);

            Directory.Exists(tempDataDir).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(tempDataDir)) Directory.Delete(tempDataDir, true);
        }
    }

    [Fact]
    public async Task UninstallCommand_Binary_DirNotFound_WritesNothing()
    {
        var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        var provider = BuildProvider(s =>
        {
            var ph = Substitute.For<IProcessHandler>();
            ph.IsRunning(Arg.Any<string>(), Arg.Any<string>()).Returns(false);
            s.AddSingleton(ph);

            var loc = Substitute.For<ILocation>();
            loc.VectraBinaryName.Returns("vectra");
            loc.DefaultVectraDirectoryName.Returns(nonExistentDir);
            s.AddSingleton(loc);

            var settings = Substitute.For<IAppSettingsService>();
            settings.LoadAsync(Arg.Any<CancellationToken>()).Returns(DefaultSettings());
            s.AddSingleton(settings);
        });

        var cmd = UninstallCommand.Create(provider);
        await InvokeAsync(cmd, []);

        provider.GetRequiredService<IVectraCtlLogger>().Received().Write(Arg.Is<string>(s => s.Contains("not found")));
    }
}
