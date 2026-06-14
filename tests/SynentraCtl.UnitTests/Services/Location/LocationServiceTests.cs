using FluentAssertions;
using System.Runtime.InteropServices;
using VectraCtl.Services.Location;

namespace VectraCtl.UnitTests.Services.Location;

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
    public void DefaultVectraDirectoryName_ShouldContainDotVectra()
    {
        _sut.DefaultVectraDirectoryName.Should().EndWith(".vectra");
    }

    [Fact]
    public void DefaultVectraDirectoryName_ShouldBeUnderUserProfile()
    {
        _sut.DefaultVectraDirectoryName.Should()
            .StartWith(_sut.UserProfilePath);
    }

    [Fact]
    public void DefaultVectraBinaryDirectoryName_ShouldBeUnderVectraDirectory()
    {
        _sut.DefaultVectraBinaryDirectoryName.Should()
            .StartWith(_sut.DefaultVectraDirectoryName);
    }

    [Fact]
    public void DefaultVectraBinaryDirectoryName_ShouldContainGateway()
    {
        _sut.DefaultVectraBinaryDirectoryName.Should().EndWith("gateway");
    }

    [Fact]
    public void VectraBinaryName_ShouldBeVectra()
    {
        _sut.VectraBinaryName.Should().Be("vectra");
    }

    [Fact]
    public void LookupVectraBinaryFilePath_ShouldCombinePathWithBinaryName()
    {
        var path = Path.Combine("some", "path");
        var result = _sut.LookupVectraBinaryFilePath(path);

        result.Should().StartWith(path);
        result.Should().Contain("vectra");
    }

    [Fact]
    public void LookupVectraCtlBinaryFilePath_ShouldCombinePathWithBinaryName()
    {
        var path = Path.Combine("some", "path");
        var result = _sut.LookupVectraCtlBinaryFilePath(path);

        result.Should().StartWith(path);
        result.Should().Contain("vectractl");
    }

    [Fact]
    public void LookupVectraBinaryFilePath_OnWindows_ShouldHaveExeExtension()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        var result = _sut.LookupVectraBinaryFilePath("dir");
        result.Should().EndWith(".exe");
    }

    [Fact]
    public void LookupVectraBinaryFilePath_OnNonWindows_ShouldNotHaveExeExtension()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        var result = _sut.LookupVectraBinaryFilePath("dir");
        result.Should().NotEndWith(".exe");
    }
}
