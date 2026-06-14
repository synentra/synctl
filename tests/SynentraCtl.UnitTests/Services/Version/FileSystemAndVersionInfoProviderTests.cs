using FluentAssertions;
using VectraCtl.Services.Version;

namespace VectraCtl.UnitTests.Services.Version;

public class FileSystemWrapperTests
{
    private readonly FileSystemWrapper _sut = new();

    [Fact]
    public void FileExists_ExistingFile_ReturnsTrue()
    {
        var path = Path.GetTempFileName();
        try
        {
            _sut.FileExists(path).Should().BeTrue();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FileExists_NonExistingFile_ReturnsFalse()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".missing");
        _sut.FileExists(path).Should().BeFalse();
    }
}

public class VersionInfoProviderTests
{
    private readonly VersionInfoProvider _sut = new();

    [Fact]
    public void GetFileVersion_ValidAssembly_ReturnsNonNull()
    {
        // Use the current test assembly DLL which is guaranteed to be on disk
        var path = typeof(VersionInfoProviderTests).Assembly.Location;
        var version = _sut.GetFileVersion(path);
        version.Should().NotBeNull();
    }

    [Fact]
    public void GetFileVersion_FileWithNoVersionResource_ReturnsEmptyString()
    {
        // Create a temp file that is not a PE binary
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "not a binary");
        try
        {
            var version = _sut.GetFileVersion(path);
            version.Should().BeEmpty();
        }
        finally
        {
            File.Delete(path);
        }
    }
}
