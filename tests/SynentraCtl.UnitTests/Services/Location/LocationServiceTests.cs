using FluentAssertions;
using System.Runtime.InteropServices;
using SynentraCtl.Services.Location;

namespace SynentraCtl.UnitTests.Services.Location;

public class LocationServiceTests
{
    private readonly LocationService _sut;

    public LocationServiceTests()
    {
        _sut = new LocationService();
    }

    [Fact]
    public void RootLocation_ShouldNotBeNullOrEmpty()
    {
        _sut.RootLocation.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void UserProfilePath_ShouldNotBeNullOrEmpty()
    {
        _sut.UserProfilePath.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void DefaultSynentraDirectoryName_ShouldContainDotSynentra()
    {
        _sut.DefaultSynentraDirectoryName.Should().EndWith(".synentra");
    }

    [Fact]
    public void DefaultSynentraDirectoryName_ShouldBeUnderUserProfile()
    {
        _sut.DefaultSynentraDirectoryName.Should()
            .StartWith(_sut.UserProfilePath);
    }

    [Fact]
    public void DefaultSynentraBinaryDirectoryName_ShouldBeUnderSynentraDirectory()
    {
        _sut.DefaultSynentraBinaryDirectoryName.Should()
            .StartWith(_sut.DefaultSynentraDirectoryName);
    }

    [Fact]
    public void DefaultSynentraBinaryDirectoryName_ShouldContainGateway()
    {
        _sut.DefaultSynentraBinaryDirectoryName.Should().EndWith("gateway");
    }

    [Fact]
    public void SynentraBinaryName_ShouldBeSynentra()
    {
        _sut.SynentraBinaryName.Should().Be("synentra");
    }

    [Fact]
    public void LookupSynentraBinaryFilePath_ShouldCombinePathWithBinaryName()
    {
        var path = Path.Combine("some", "path");
        var result = _sut.LookupSynentraBinaryFilePath(path);

        result.Should().StartWith(path);
        result.Should().Contain("synentra");
    }

    [Fact]
    public void LookupSynentraCtlBinaryFilePath_ShouldCombinePathWithBinaryName()
    {
        var path = Path.Combine("some", "path");
        var result = _sut.LookupSynentraCtlBinaryFilePath(path);

        result.Should().StartWith(path);
        result.Should().Contain("synctl");
    }

    [Fact]
    public void LookupSynentraBinaryFilePath_OnWindows_ShouldHaveExeExtension()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        var result = _sut.LookupSynentraBinaryFilePath("dir");
        result.Should().EndWith(".exe");
    }

    [Fact]
    public void LookupSynentraBinaryFilePath_OnNonWindows_ShouldNotHaveExeExtension()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        var result = _sut.LookupSynentraBinaryFilePath("dir");
        result.Should().NotEndWith(".exe");
    }
}
