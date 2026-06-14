using VectraCtl.Core.Models.Docker;

namespace VectraCtl.Core.UnitTests.Models;

public class DockerCommandResultTests
{
    [Fact]
    public void DockerCommandResult_DefaultExitCode_IsZero()
    {
        var result = new DockerCommandResult();

        result.ExitCode.Should().Be(0);
    }

    [Fact]
    public void DockerCommandResult_DefaultOutput_IsEmpty()
    {
        var result = new DockerCommandResult();

        result.Output.Should().BeEmpty();
    }

    [Fact]
    public void DockerCommandResult_DefaultError_IsEmpty()
    {
        var result = new DockerCommandResult();

        result.Error.Should().BeEmpty();
    }

    [Fact]
    public void DockerCommandResult_DefaultSuccess_IsTrue()
    {
        var result = new DockerCommandResult();

        result.Success.Should().BeTrue();
    }

    [Fact]
    public void DockerCommandResult_ExitCodeZero_SuccessIsTrue()
    {
        var result = new DockerCommandResult { ExitCode = 0 };

        result.Success.Should().BeTrue();
    }

    [Fact]
    public void DockerCommandResult_ExitCodeNonZero_SuccessIsFalse()
    {
        var result = new DockerCommandResult { ExitCode = 1 };

        result.Success.Should().BeFalse();
    }

    [Fact]
    public void DockerCommandResult_ExitCodeNegative_SuccessIsFalse()
    {
        var result = new DockerCommandResult { ExitCode = -1 };

        result.Success.Should().BeFalse();
    }

    [Fact]
    public void DockerCommandResult_SetOutput_ReturnsSetValue()
    {
        var result = new DockerCommandResult { Output = "some output" };

        result.Output.Should().Be("some output");
    }

    [Fact]
    public void DockerCommandResult_SetError_ReturnsSetValue()
    {
        var result = new DockerCommandResult { Error = "some error" };

        result.Error.Should().Be("some error");
    }

    [Fact]
    public void DockerCommandResult_SetAllProperties_ReturnsExpectedValues()
    {
        var result = new DockerCommandResult
        {
            ExitCode = 127,
            Output = "command output",
            Error = "command error"
        };

        result.ExitCode.Should().Be(127);
        result.Output.Should().Be("command output");
        result.Error.Should().Be("command error");
        result.Success.Should().BeFalse();
    }
}
