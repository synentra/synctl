using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.CommandLine;
using Vectra.Client.Abstractions;
using VectraCtl.Commands;
using VectraCtl.Core.Models.Configuration;
using VectraCtl.Core.Models.Docker;
using VectraCtl.Core.Serialization;
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
/// Validates that each command builder creates a Command with the expected name and subcommands,
/// and that the service provider wiring doesn't throw.
/// </summary>
public class CommandCreationTests
{
    private readonly IServiceProvider _sp;

    public CommandCreationTests()
    {
        // Build a service provider with all stubs needed by every command's Create() method
        var services = new ServiceCollection();

        var logger = Substitute.For<IVectraCtlLogger>();
        var location = Substitute.For<ILocation>();
        location.RootLocation.Returns(Path.GetTempPath());
        location.DefaultVectraDirectoryName.Returns(Path.Combine(Path.GetTempPath(), ".vectra"));
        location.DefaultVectraBinaryDirectoryName.Returns(Path.Combine(Path.GetTempPath(), ".vectra", "bin"));
        location.VectraBinaryName.Returns("vectra");
        location.LookupVectraBinaryFilePath(Arg.Any<string>()).Returns(Path.Combine(Path.GetTempPath(), "vectra"));

        var docker = Substitute.For<IDockerService>();
        var appSettings = Substitute.For<IAppSettingsService>();
        appSettings.LoadAsync(Arg.Any<CancellationToken>()).Returns(new AppSettings());
        var gitHub = Substitute.For<IGitHubReleaseManager>();
        var extractor = Substitute.For<IArchiveExtractor>();
        var processHandler = Substitute.For<IProcessHandler>();
        var version = Substitute.For<IVersion>();
        var vectraClient = Substitute.For<IVectraClient>();

        services.AddSingleton(logger);
        services.AddSingleton(location);
        services.AddSingleton(docker);
        services.AddSingleton(appSettings);
        services.AddSingleton(gitHub);
        services.AddSingleton(extractor);
        services.AddSingleton(processHandler);
        services.AddSingleton(version);
        services.AddSingleton(vectraClient);

        _sp = services.BuildServiceProvider();
    }

    [Fact]
    public void AgentsCommand_Create_ReturnsCommandWithCorrectNameAndSubcommands()
    {
        var cmd = AgentsCommand.Create(_sp);
        cmd.Name.Should().Be("agents");
        cmd.Subcommands.Should().Contain(s => s.Name == "list");
        cmd.Subcommands.Should().Contain(s => s.Name == "register");
        cmd.Subcommands.Should().Contain(s => s.Name == "assign-policy");
        cmd.Subcommands.Should().Contain(s => s.Name == "lift-quarantine");
        cmd.Subcommands.Should().Contain(s => s.Name == "delete");
    }

    [Fact]
    public void HitlCommand_Create_ReturnsCommandWithCorrectNameAndSubcommands()
    {
        var cmd = HitlCommand.Create(_sp);
        cmd.Name.Should().Be("hitl");
        cmd.Subcommands.Should().Contain(s => s.Name == "list");
        cmd.Subcommands.Should().Contain(s => s.Name == "status");
        cmd.Subcommands.Should().Contain(s => s.Name == "approve");
        cmd.Subcommands.Should().Contain(s => s.Name == "deny");
    }

    [Fact]
    public void PoliciesCommand_Create_ReturnsCommandWithCorrectNameAndSubcommands()
    {
        var cmd = PoliciesCommand.Create(_sp);
        cmd.Name.Should().Be("policies");
        cmd.Subcommands.Should().Contain(s => s.Name == "list");
        cmd.Subcommands.Should().Contain(s => s.Name == "details");
    }

    [Fact]
    public void TokenCommand_Create_ReturnsCommandWithCorrectName()
    {
        var cmd = TokenCommand.Create(_sp);
        cmd.Name.Should().Be("token");
    }

    [Fact]
    public void InitCommand_Create_ReturnsCommandWithCorrectName()
    {
        var cmd = InitCommand.Create(_sp);
        cmd.Name.Should().Be("init");
    }

    [Fact]
    public void RunCommand_Create_ReturnsCommandWithCorrectName()
    {
        var cmd = RunCommand.Create(_sp);
        cmd.Name.Should().Be("run");
    }

    [Fact]
    public void StopCommand_Create_ReturnsCommandWithCorrectName()
    {
        var cmd = StopCommand.Create(_sp);
        cmd.Name.Should().Be("stop");
    }

    [Fact]
    public void UninstallCommand_Create_ReturnsCommandWithCorrectName()
    {
        var cmd = UninstallCommand.Create(_sp);
        cmd.Name.Should().Be("uninstall");
    }

    [Fact]
    public void UpdateCommand_Create_ReturnsCommandWithCorrectName()
    {
        var cmd = UpdateCommand.Create(_sp);
        cmd.Name.Should().Be("update");
    }

    [Fact]
    public void VectraCommandLine_Create_ReturnsRootCommandWithAllSubcommands()
    {
        var root = VectraCommandLine.Create(_sp, Array.Empty<string>());
        root.Should().NotBeNull();

        var names = root.Subcommands.Select(s => s.Name).ToList();
        names.Should().Contain("agents");
        names.Should().Contain("hitl");
        names.Should().Contain("init");
        names.Should().Contain("policies");
        names.Should().Contain("run");
        names.Should().Contain("stop");
        names.Should().Contain("token");
        names.Should().Contain("uninstall");
        names.Should().Contain("update");
    }
}
