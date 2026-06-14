using System.Runtime.InteropServices;
using SynentraCtl.Core.Services.Github;
using SynentraCtl.Core.Services.Logger;

namespace SynentraCtl.Core.UnitTests.Services;

public class GitHubSettingsTests
{
    [Fact]
    public void GitHubSettings_Organization_IsSynentra()
    {
        GitHubSettings.Organization.Should().Be("synentra");
    }

    [Fact]
    public void GitHubSettings_SynentraRepository_IsSynentra()
    {
        GitHubSettings.SynentraRepository.Should().Be("synentra");
    }

    [Fact]
    public void GitHubSettings_SynentraCtlRepository_IsSynentraCtl()
    {
        GitHubSettings.SynentraCtlRepository.Should().Be("synctl");
    }

    [Fact]
    public void GitHubSettings_SynentraArchiveFileName_StartsWithSynentraPrefix()
    {
        GitHubSettings.SynentraArchiveFileName.Should().StartWith("synentra-");
    }

    [Fact]
    public void GitHubSettings_SynentraArchiveFileName_IsLowerCase()
    {
        GitHubSettings.SynentraArchiveFileName.Should().Be(GitHubSettings.SynentraArchiveFileName.ToLower());
    }

    [Fact]
    public void GitHubSettings_SynentraArchiveHashFileName_StartsWithSynentraPrefix()
    {
        GitHubSettings.SynentraArchiveHashFileName.Should().StartWith("synentra-");
    }

    [Fact]
    public void GitHubSettings_SynentraArchiveHashFileName_EndsWithSha256()
    {
        GitHubSettings.SynentraArchiveHashFileName.Should().EndWith(".sha256");
    }

    [Fact]
    public void GitHubSettings_SynentraCtlArchiveFileName_StartsWithSynCtlPrefix()
    {
        GitHubSettings.SynentraCtlArchiveFileName.Should().StartWith("synctl-");
    }

    [Fact]
    public void GitHubSettings_SynentraCtlArchiveFileName_IsLowerCase()
    {
        GitHubSettings.SynentraCtlArchiveFileName.Should().Be(GitHubSettings.SynentraCtlArchiveFileName.ToLower());
    }

    [Fact]
    public void GitHubSettings_SynentraCtlArchiveHashFileName_StartsWithSynCtlPrefix()
    {
        GitHubSettings.SynentraCtlArchiveHashFileName.Should().StartWith("synctl-");
    }

    [Fact]
    public void GitHubSettings_SynentraCtlArchiveHashFileName_EndsWithSha256()
    {
        GitHubSettings.SynentraCtlArchiveHashFileName.Should().EndWith(".sha256");
    }

    [Fact]
    public void GitHubSettings_SynentraArchiveFileName_ContainsExpectedExtension()
    {
        var expected = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".zip" : ".tar.gz";
        GitHubSettings.SynentraArchiveFileName.Should().Contain(expected);
    }

    [Fact]
    public void GitHubSettings_SynentraArchiveTemporaryFileName_StartsWithSynentraPrefix()
    {
        GitHubSettings.SynentraArchiveTemporaryFileName.Should().StartWith("synentra-");
    }

    [Fact]
    public void GitHubSettings_SynentraArchiveTemporaryFileName_IsLowerCase()
    {
        var fileName = GitHubSettings.SynentraArchiveTemporaryFileName;
        fileName.Should().Be(fileName.ToLower());
    }

    [Fact]
    public void GitHubSettings_SynentraArchiveTemporaryHashFileName_StartsWithSynentraPrefix()
    {
        GitHubSettings.SynentraArchiveTemporaryHashFileName.Should().StartWith("synentra-");
    }

    [Fact]
    public void GitHubSettings_SynentraArchiveTemporaryHashFileName_EndsWithSha256()
    {
        GitHubSettings.SynentraArchiveTemporaryHashFileName.Should().EndWith(".sha256");
    }

    [Fact]
    public void GitHubSettings_TwoCallsToTemporaryFileName_ReturnDifferentValues()
    {
        var first = GitHubSettings.SynentraArchiveTemporaryFileName;
        var second = GitHubSettings.SynentraArchiveTemporaryFileName;

        first.Should().NotBe(second);
    }

    [Fact]
    public void GitHubSettings_TwoCallsToTemporaryHashFileName_ReturnDifferentValues()
    {
        var first = GitHubSettings.SynentraArchiveTemporaryHashFileName;
        var second = GitHubSettings.SynentraArchiveTemporaryHashFileName;

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
