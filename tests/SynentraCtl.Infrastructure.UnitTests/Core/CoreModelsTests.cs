using FluentAssertions;
using SynentraCtl.Core.Exceptions;
using SynentraCtl.Core.Models.Configuration;
using SynentraCtl.Core.Models.Docker;
using SynentraCtl.Core.Services.Github;

namespace SynentraCtl.Infrastructure.UnitTests.Core;

public class CoreModelsTests
{
    // --- SynentraCtlException ---

    [Fact]
    public void SynentraCtlException_MessageConstructor_SetsMessage()
    {
        var ex = new SynentraCtlException("something went wrong");
        ex.Message.Should().Be("something went wrong");
    }

    [Fact]
    public void SynentraCtlException_InnerExceptionConstructor_SetsMessageAndInner()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new SynentraCtlException("outer", inner);
        ex.Message.Should().Be("outer");
        ex.InnerException.Should().BeSameAs(inner);
    }

    // --- DockerCommandResult ---

    [Fact]
    public void DockerCommandResult_ExitCode0_SuccessIsTrue()
    {
        var result = new DockerCommandResult { ExitCode = 0, Output = "ok", Error = string.Empty };
        result.Success.Should().BeTrue();
        result.Output.Should().Be("ok");
    }

    [Fact]
    public void DockerCommandResult_NonZeroExitCode_SuccessIsFalse()
    {
        var result = new DockerCommandResult { ExitCode = 1, Error = "fail" };
        result.Success.Should().BeFalse();
        result.Error.Should().Be("fail");
    }

    // --- DockerRunOptions ---

    [Fact]
    public void DockerRunOptions_DefaultValues_AreCorrect()
    {
        var opts = new DockerRunOptions();
        opts.ImageName.Should().BeEmpty();
        opts.Tag.Should().BeEmpty();
        opts.ContainerName.Should().BeEmpty();
        opts.ContainerPort.Should().Be(7080);
        opts.Detached.Should().BeTrue();
        opts.AdditionalArguments.Should().BeEmpty();
    }

    [Fact]
    public void DockerRunOptions_CustomValues_AreStored()
    {
        var opts = new DockerRunOptions
        {
            ImageName = "myimage",
            Tag = "v2",
            ContainerName = "mycontainer",
            HostPort = 8080,
            ContainerPort = 9090,
            HostDataPath = "/host",
            ContainerDataPath = "/container",
            Detached = false,
            AdditionalArguments = "--rm"
        };
        opts.ImageName.Should().Be("myimage");
        opts.Tag.Should().Be("v2");
        opts.ContainerName.Should().Be("mycontainer");
        opts.HostPort.Should().Be(8080);
        opts.ContainerPort.Should().Be(9090);
        opts.HostDataPath.Should().Be("/host");
        opts.ContainerDataPath.Should().Be("/container");
        opts.Detached.Should().BeFalse();
        opts.AdditionalArguments.Should().Be("--rm");
    }

    // --- BinarySettings ---

    [Fact]
    public void BinarySettings_DefaultVersion_IsNull()
    {
        var settings = new BinarySettings();
        settings.Version.Should().BeNull();
    }

    [Fact]
    public void BinarySettings_SetVersion_IsStored()
    {
        var settings = new BinarySettings { Version = "1.2.3" };
        settings.Version.Should().Be("1.2.3");
    }

    // --- GitHubSettings (static class) ---

    [Fact]
    public void GitHubSettings_Constants_HaveExpectedValues()
    {
        GitHubSettings.Organization.Should().Be("synentra");
        GitHubSettings.SynentraRepository.Should().Be("synentra");
        GitHubSettings.SynentraCtlRepository.Should().Be("synctl");
    }

    [Fact]
    public void GitHubSettings_SynentraArchiveFileName_IsNotEmpty()
    {
        GitHubSettings.SynentraArchiveFileName.Should().NotBeNullOrEmpty();
        GitHubSettings.SynentraArchiveFileName.Should().StartWith("synentra-");
    }

    [Fact]
    public void GitHubSettings_SynentraArchiveHashFileName_EndsWithSha256()
    {
        GitHubSettings.SynentraArchiveHashFileName.Should().EndWith(".sha256");
    }

    [Fact]
    public void GitHubSettings_SynentraCtlArchiveFileName_StartsWithSynCtl()
    {
        GitHubSettings.SynentraCtlArchiveFileName.Should().StartWith("synctl-");
    }

    [Fact]
    public void GitHubSettings_SynentraCtlArchiveHashFileName_EndsWithSha256()
    {
        GitHubSettings.SynentraCtlArchiveHashFileName.Should().EndWith(".sha256");
    }

    [Fact]
    public void GitHubSettings_TemporaryFileNames_AreUnique()
    {
        var name1 = GitHubSettings.SynentraArchiveTemporaryFileName;
        var name2 = GitHubSettings.SynentraArchiveTemporaryFileName;
        name1.Should().NotBe(name2);
    }

    [Fact]
    public void GitHubSettings_SynentraArchiveTemporaryHashFileName_EndsWithSha256()
    {
        GitHubSettings.SynentraArchiveTemporaryHashFileName.Should().EndWith(".sha256");
    }
}
