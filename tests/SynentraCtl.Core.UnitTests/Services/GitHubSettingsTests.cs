using System.Runtime.InteropServices;
using VectraCtl.Core.Services.Github;
using VectraCtl.Core.Services.Logger;

namespace VectraCtl.Core.UnitTests.Services;

public class GitHubSettingsTests
{
    [Fact]
    public void GitHubSettings_Organization_IsCortexiumlabs()
    {
        GitHubSettings.Organization.Should().Be("cortexiumlabs");
    }

    [Fact]
    public void GitHubSettings_VectraRepository_IsVectra()
    {
        GitHubSettings.VectraRepository.Should().Be("vectra");
    }

    [Fact]
    public void GitHubSettings_VectraCtlRepository_IsVectractl()
    {
        GitHubSettings.VectraCtlRepository.Should().Be("vectractl");
    }

    [Fact]
    public void GitHubSettings_VectraArchiveFileName_StartsWithVectraPrefix()
    {
        GitHubSettings.VectraArchiveFileName.Should().StartWith("vectra-");
    }

    [Fact]
    public void GitHubSettings_VectraArchiveFileName_IsLowerCase()
    {
        GitHubSettings.VectraArchiveFileName.Should().Be(GitHubSettings.VectraArchiveFileName.ToLower());
    }

    [Fact]
    public void GitHubSettings_VectraArchiveHashFileName_StartsWithVectraPrefix()
    {
        GitHubSettings.VectraArchiveHashFileName.Should().StartWith("vectra-");
    }

    [Fact]
    public void GitHubSettings_VectraArchiveHashFileName_EndsWithSha256()
    {
        GitHubSettings.VectraArchiveHashFileName.Should().EndWith(".sha256");
    }

    [Fact]
    public void GitHubSettings_VectraCtlArchiveFileName_StartsWithVectrактlPrefix()
    {
        GitHubSettings.VectraCtlArchiveFileName.Should().StartWith("vectractl-");
    }

    [Fact]
    public void GitHubSettings_VectraCtlArchiveFileName_IsLowerCase()
    {
        GitHubSettings.VectraCtlArchiveFileName.Should().Be(GitHubSettings.VectraCtlArchiveFileName.ToLower());
    }

    [Fact]
    public void GitHubSettings_VectraCtlArchiveHashFileName_StartsWithVectraCtlPrefix()
    {
        GitHubSettings.VectraCtlArchiveHashFileName.Should().StartWith("vectractl-");
    }

    [Fact]
    public void GitHubSettings_VectraCtlArchiveHashFileName_EndsWithSha256()
    {
        GitHubSettings.VectraCtlArchiveHashFileName.Should().EndWith(".sha256");
    }

    [Fact]
    public void GitHubSettings_VectraArchiveFileName_ContainsExpectedExtension()
    {
        var expected = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".zip" : ".tar.gz";
        GitHubSettings.VectraArchiveFileName.Should().Contain(expected);
    }

    [Fact]
    public void GitHubSettings_VectraArchiveTemporaryFileName_StartsWithVectraPrefix()
    {
        GitHubSettings.VectraArchiveTemporaryFileName.Should().StartWith("vectra-");
    }

    [Fact]
    public void GitHubSettings_VectraArchiveTemporaryFileName_IsLowerCase()
    {
        var fileName = GitHubSettings.VectraArchiveTemporaryFileName;
        fileName.Should().Be(fileName.ToLower());
    }

    [Fact]
    public void GitHubSettings_VectraArchiveTemporaryHashFileName_StartsWithVectraPrefix()
    {
        GitHubSettings.VectraArchiveTemporaryHashFileName.Should().StartWith("vectra-");
    }

    [Fact]
    public void GitHubSettings_VectraArchiveTemporaryHashFileName_EndsWithSha256()
    {
        GitHubSettings.VectraArchiveTemporaryHashFileName.Should().EndWith(".sha256");
    }

    [Fact]
    public void GitHubSettings_TwoCallsToTemporaryFileName_ReturnDifferentValues()
    {
        var first = GitHubSettings.VectraArchiveTemporaryFileName;
        var second = GitHubSettings.VectraArchiveTemporaryFileName;

        first.Should().NotBe(second);
    }

    [Fact]
    public void GitHubSettings_TwoCallsToTemporaryHashFileName_ReturnDifferentValues()
    {
        var first = GitHubSettings.VectraArchiveTemporaryHashFileName;
        var second = GitHubSettings.VectraArchiveTemporaryHashFileName;

        first.Should().NotBe(second);
    }
}

public class OutputTypeTests
{
    [Fact]
    public void OutputType_Json_HasValueZero()
    {
        ((int)OutputType.Json).Should().Be(0);
    }

    [Fact]
    public void OutputType_Xml_HasValueOne()
    {
        ((int)OutputType.Xml).Should().Be(1);
    }

    [Fact]
    public void OutputType_Yaml_HasValueTwo()
    {
        ((int)OutputType.Yaml).Should().Be(2);
    }

    [Fact]
    public void OutputType_Table_HasValueThree()
    {
        ((int)OutputType.Table).Should().Be(3);
    }

    [Fact]
    public void OutputType_Values_ContainsAllExpectedMembers()
    {
        var values = Enum.GetValues<OutputType>();

        values.Should().Contain(OutputType.Json);
        values.Should().Contain(OutputType.Xml);
        values.Should().Contain(OutputType.Yaml);
        values.Should().Contain(OutputType.Table);
    }
}
