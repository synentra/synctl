using FluentAssertions;
using VectraCtl.Core.Exceptions;
using VectraCtl.Core.Models.Configuration;
using VectraCtl.Core.Models.Docker;
using VectraCtl.Core.Services.Github;

namespace VectraCtl.Infrastructure.UnitTests.Core;

public class CoreModelsTests
{
    // --- VectraCtlException ---

    [Fact]
    public void VectraCtlException_MessageConstructor_SetsMessage()
    {
        var ex = new VectraCtlException("something went wrong");
        ex.Message.Should().Be("something went wrong");
    }

    [Fact]
    public void VectraCtlException_InnerExceptionConstructor_SetsMessageAndInner()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new VectraCtlException("outer", inner);
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
        GitHubSettings.Organization.Should().Be("cortexiumlabs");
        GitHubSettings.VectraRepository.Should().Be("vectra");
        GitHubSettings.VectraCtlRepository.Should().Be("vectractl");
    }

    [Fact]
    public void GitHubSettings_VectraArchiveFileName_IsNotEmpty()
    {
        GitHubSettings.VectraArchiveFileName.Should().NotBeNullOrEmpty();
        GitHubSettings.VectraArchiveFileName.Should().StartWith("vectra-");
    }

    [Fact]
    public void GitHubSettings_VectraArchiveHashFileName_EndsWithSha256()
    {
        GitHubSettings.VectraArchiveHashFileName.Should().EndWith(".sha256");
    }

    [Fact]
    public void GitHubSettings_VectraCtlArchiveFileName_StartsWithVectractl()
    {
        GitHubSettings.VectraCtlArchiveFileName.Should().StartWith("vectractl-");
    }

    [Fact]
    public void GitHubSettings_VectraCtlArchiveHashFileName_EndsWithSha256()
    {
        GitHubSettings.VectraCtlArchiveHashFileName.Should().EndWith(".sha256");
    }

    [Fact]
    public void GitHubSettings_TemporaryFileNames_AreUnique()
    {
        var name1 = GitHubSettings.VectraArchiveTemporaryFileName;
        var name2 = GitHubSettings.VectraArchiveTemporaryFileName;
        name1.Should().NotBe(name2);
    }

    [Fact]
    public void GitHubSettings_VectraArchiveTemporaryHashFileName_EndsWithSha256()
    {
        GitHubSettings.VectraArchiveTemporaryHashFileName.Should().EndWith(".sha256");
    }
}
