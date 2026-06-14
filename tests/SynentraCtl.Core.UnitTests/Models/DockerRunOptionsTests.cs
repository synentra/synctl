using VectraCtl.Core.Models.Docker;

namespace VectraCtl.Core.UnitTests.Models;

public class DockerRunOptionsTests
{
    [Fact]
    public void DockerRunOptions_DefaultImageName_IsEmpty()
    {
        var options = new DockerRunOptions();

        options.ImageName.Should().BeEmpty();
    }

    [Fact]
    public void DockerRunOptions_DefaultTag_IsEmpty()
    {
        var options = new DockerRunOptions();

        options.Tag.Should().BeEmpty();
    }

    [Fact]
    public void DockerRunOptions_DefaultContainerName_IsEmpty()
    {
        var options = new DockerRunOptions();

        options.ContainerName.Should().BeEmpty();
    }

    [Fact]
    public void DockerRunOptions_DefaultHostPort_IsZero()
    {
        var options = new DockerRunOptions();

        options.HostPort.Should().Be(0);
    }

    [Fact]
    public void DockerRunOptions_DefaultContainerPort_Is7080()
    {
        var options = new DockerRunOptions();

        options.ContainerPort.Should().Be(7080);
    }

    [Fact]
    public void DockerRunOptions_DefaultHostDataPath_IsEmpty()
    {
        var options = new DockerRunOptions();

        options.HostDataPath.Should().BeEmpty();
    }

    [Fact]
    public void DockerRunOptions_DefaultContainerDataPath_IsEmpty()
    {
        var options = new DockerRunOptions();

        options.ContainerDataPath.Should().BeEmpty();
    }

    [Fact]
    public void DockerRunOptions_DefaultDetached_IsTrue()
    {
        var options = new DockerRunOptions();

        options.Detached.Should().BeTrue();
    }

    [Fact]
    public void DockerRunOptions_DefaultAdditionalArguments_IsEmpty()
    {
        var options = new DockerRunOptions();

        options.AdditionalArguments.Should().BeEmpty();
    }

    [Fact]
    public void DockerRunOptions_SetDetachedFalse_ReturnsFalse()
    {
        var options = new DockerRunOptions { Detached = false };

        options.Detached.Should().BeFalse();
    }

    [Fact]
    public void DockerRunOptions_SetAdditionalArgumentsNull_ReturnsNull()
    {
        var options = new DockerRunOptions { AdditionalArguments = null };

        options.AdditionalArguments.Should().BeNull();
    }

    [Fact]
    public void DockerRunOptions_SetAllProperties_ReturnsExpectedValues()
    {
        var options = new DockerRunOptions
        {
            ImageName = "cortexiumlabs/vectra",
            Tag = "1.0.0",
            ContainerName = "vectra",
            HostPort = 9090,
            ContainerPort = 7080,
            HostDataPath = "/data/host",
            ContainerDataPath = "/app/data",
            Detached = false,
            AdditionalArguments = "--env DEBUG=true"
        };

        options.ImageName.Should().Be("cortexiumlabs/vectra");
        options.Tag.Should().Be("1.0.0");
        options.ContainerName.Should().Be("vectra");
        options.HostPort.Should().Be(9090);
        options.ContainerPort.Should().Be(7080);
        options.HostDataPath.Should().Be("/data/host");
        options.ContainerDataPath.Should().Be("/app/data");
        options.Detached.Should().BeFalse();
        options.AdditionalArguments.Should().Be("--env DEBUG=true");
    }
}
