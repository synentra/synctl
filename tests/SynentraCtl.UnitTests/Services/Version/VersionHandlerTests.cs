using FluentAssertions;
using NSubstitute;
using VectraCtl.Services.Version;

namespace VectraCtl.UnitTests.Services.Version;

public class VersionHandlerTests
{
    private readonly IFileSystem _fileSystem;
    private readonly IVersionInfoProvider _versionInfoProvider;
    private readonly VersionHandler _sut;

    public VersionHandlerTests()
    {
        _fileSystem = Substitute.For<IFileSystem>();
        _versionInfoProvider = Substitute.For<IVersionInfoProvider>();
        _sut = new VersionHandler(_fileSystem, _versionInfoProvider);
    }

    [Fact]
    public void VectraCtlVersion_ShouldReturnStringValue()
    {
        // The assembly may or may not have the attribute; just verify it returns a string
        _sut.VectraCtlVersion.Should().NotBeNull();
    }

    [Fact]
    public void GetVersionFromPath_NullPath_ReturnsEmpty()
    {
        var result = _sut.GetVersionFromPath(null!);
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetVersionFromPath_EmptyPath_ReturnsEmpty()
    {
        var result = _sut.GetVersionFromPath(string.Empty);
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetVersionFromPath_FileDoesNotExist_ReturnsEmpty()
    {
        _fileSystem.FileExists("nonexistent.exe").Returns(false);

        var result = _sut.GetVersionFromPath("nonexistent.exe");

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetVersionFromPath_FileExists_ReturnsVersionFromProvider()
    {
        const string path = "some/path/binary.exe";
        const string expectedVersion = "1.2.3";

        _fileSystem.FileExists(path).Returns(true);
        _versionInfoProvider.GetFileVersion(path).Returns(expectedVersion);

        var result = _sut.GetVersionFromPath(path);

        result.Should().Be(expectedVersion);
    }

    [Fact]
    public void GetVersionFromPath_FileExists_ProviderReturnsNull_ReturnsEmpty()
    {
        const string path = "some/path/binary.exe";

        _fileSystem.FileExists(path).Returns(true);
        _versionInfoProvider.GetFileVersion(path).Returns((string?)null);

        var result = _sut.GetVersionFromPath(path);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetVersionFromPath_FileExists_CallsProviderWithCorrectPath()
    {
        const string path = "some/path/binary.exe";

        _fileSystem.FileExists(path).Returns(true);
        _versionInfoProvider.GetFileVersion(path).Returns("2.0.0");

        _sut.GetVersionFromPath(path);

        _versionInfoProvider.Received(1).GetFileVersion(path);
    }

    [Fact]
    public void GetVersionFromPath_FileDoesNotExist_NeverCallsProvider()
    {
        const string path = "missing.exe";
        _fileSystem.FileExists(path).Returns(false);

        _sut.GetVersionFromPath(path);

        _versionInfoProvider.DidNotReceive().GetFileVersion(Arg.Any<string>());
    }
}
