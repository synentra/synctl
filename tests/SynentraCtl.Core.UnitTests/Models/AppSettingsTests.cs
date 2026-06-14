using VectraCtl.Core.Models.Configuration;

namespace VectraCtl.Core.UnitTests.Models;

public class AppSettingsTests
{
    [Fact]
    public void AppSettings_DefaultDeploymentMode_IsBinary()
    {
        var settings = new AppSettings();

        settings.DeploymentMode.Should().Be(DeploymentMode.Binary);
    }

    [Fact]
    public void AppSettings_DefaultDocker_IsNotNull()
    {
        var settings = new AppSettings();

        settings.Docker.Should().NotBeNull();
    }

    [Fact]
    public void AppSettings_DefaultBinary_IsNotNull()
    {
        var settings = new AppSettings();

        settings.Binary.Should().NotBeNull();
    }

    [Fact]
    public void AppSettings_SetDeploymentModeToDocker_ReturnsDocker()
    {
        var settings = new AppSettings { DeploymentMode = DeploymentMode.Docker };

        settings.DeploymentMode.Should().Be(DeploymentMode.Docker);
    }

    [Fact]
    public void AppSettings_SetDockerSettings_ReturnsSameInstance()
    {
        var docker = new DockerSettings { ImageName = "myimage" };
        var settings = new AppSettings { Docker = docker };

        settings.Docker.Should().BeSameAs(docker);
    }

    [Fact]
    public void AppSettings_SetBinarySettings_ReturnsSameInstance()
    {
        var binary = new BinarySettings { Version = "1.0.0" };
        var settings = new AppSettings { Binary = binary };

        settings.Binary.Should().BeSameAs(binary);
    }
}

public class DockerSettingsTests
{
    [Fact]
    public void DockerSettings_DefaultImageName_IsEmpty()
    {
        var settings = new DockerSettings();

        settings.ImageName.Should().BeEmpty();
    }

    [Fact]
    public void DockerSettings_DefaultTag_IsEmpty()
    {
        var settings = new DockerSettings();

        settings.Tag.Should().BeEmpty();
    }

    [Fact]
    public void DockerSettings_DefaultContainerName_IsEmpty()
    {
        var settings = new DockerSettings();

        settings.ContainerName.Should().BeEmpty();
    }

    [Fact]
    public void DockerSettings_DefaultPort_Is7080()
    {
        var settings = new DockerSettings();

        settings.Port.Should().Be(7080);
    }

    [Fact]
    public void DockerSettings_DefaultHostDataPath_IsEmpty()
    {
        var settings = new DockerSettings();

        settings.HostDataPath.Should().BeEmpty();
    }

    [Fact]
    public void DockerSettings_DefaultContainerDataPath_IsAppData()
    {
        var settings = new DockerSettings();

        settings.ContainerDataPath.Should().Be("/app/data");
    }

    [Fact]
    public void DockerSettings_SetProperties_ReturnSetValues()
    {
        var settings = new DockerSettings
        {
            ImageName = "myimage",
            Tag = "latest",
            ContainerName = "mycontainer",
            Port = 8080,
            HostDataPath = "/host/data",
            ContainerDataPath = "/container/data"
        };

        settings.ImageName.Should().Be("myimage");
        settings.Tag.Should().Be("latest");
        settings.ContainerName.Should().Be("mycontainer");
        settings.Port.Should().Be(8080);
        settings.HostDataPath.Should().Be("/host/data");
        settings.ContainerDataPath.Should().Be("/container/data");
    }
}

public class BinarySettingsTests
{
    [Fact]
    public void BinarySettings_DefaultVersion_IsNull()
    {
        var settings = new BinarySettings();

        settings.Version.Should().BeNull();
    }

    [Fact]
    public void BinarySettings_SetVersion_ReturnsSetValue()
    {
        var settings = new BinarySettings { Version = "2.3.1" };

        settings.Version.Should().Be("2.3.1");
    }
}

public class DeploymentModeTests
{
    [Fact]
    public void DeploymentMode_Binary_HasValueZero()
    {
        ((int)DeploymentMode.Binary).Should().Be(0);
    }

    [Fact]
    public void DeploymentMode_Docker_HasValueOne()
    {
        ((int)DeploymentMode.Docker).Should().Be(1);
    }

    [Fact]
    public void DeploymentMode_Values_ContainsBinaryAndDocker()
    {
        var values = Enum.GetValues<DeploymentMode>();

        values.Should().Contain(DeploymentMode.Binary);
        values.Should().Contain(DeploymentMode.Docker);
    }
}
