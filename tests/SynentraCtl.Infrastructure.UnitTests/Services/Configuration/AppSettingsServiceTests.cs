using FluentAssertions;
using NSubstitute;
using VectraCtl.Core.Models.Configuration;
using VectraCtl.Core.Serialization;
using VectraCtl.Core.Services.Location;
using VectraCtl.Infrastructure.Services.Configuration;

namespace VectraCtl.Infrastructure.UnitTests.Services.Configuration;

public class AppSettingsServiceTests : IDisposable
{
    private readonly ILocation _location = Substitute.For<ILocation>();
    private readonly IJsonSerializer _serializer = Substitute.For<IJsonSerializer>();
    private readonly IJsonDeserializer _deserializer = Substitute.For<IJsonDeserializer>();
    private readonly string _tempDir;
    private readonly AppSettingsService _sut;

    public AppSettingsServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _location.RootLocation.Returns(_tempDir);
        _location.DefaultVectraDirectoryName.Returns(".vectra");
        _sut = new AppSettingsService(_location, _serializer, _deserializer);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
        GC.SuppressFinalize(this);
    }

    // --- Constructor guards ---

    [Fact]
    public void Constructor_NullLocation_ThrowsArgumentNullException()
    {
        var act = () => new AppSettingsService(null!, _serializer, _deserializer);
        act.Should().Throw<ArgumentNullException>().WithParameterName("location");
    }

    [Fact]
    public void Constructor_NullSerializer_ThrowsArgumentNullException()
    {
        var act = () => new AppSettingsService(_location, null!, _deserializer);
        act.Should().Throw<ArgumentNullException>().WithParameterName("serializer");
    }

    [Fact]
    public void Constructor_NullDeserializer_ThrowsArgumentNullException()
    {
        var act = () => new AppSettingsService(_location, _serializer, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("deserializer");
    }

    // --- GetSettingsPath ---

    [Fact]
    public void GetSettingsPath_ReturnsPathUnderRootLocation()
    {
        var path = _sut.GetSettingsPath();
        path.Should().Be(Path.Combine(_tempDir, "appsettings.json"));
    }

    // --- LoadAsync: file does not exist ---

    [Fact]
    public async Task LoadAsync_FileDoesNotExist_ReturnsDefaultSettings()
    {
        var settings = await _sut.LoadAsync();
        AssertDefaultSettings(settings);
    }

    // --- LoadAsync: file exists but empty ---

    [Fact]
    public async Task LoadAsync_FileExistsButEmpty_ReturnsDefaultSettings()
    {
        await File.WriteAllTextAsync(_sut.GetSettingsPath(), "   ");

        var settings = await _sut.LoadAsync();
        AssertDefaultSettings(settings);
    }

    // --- LoadAsync: deserializer throws ---

    [Fact]
    public async Task LoadAsync_DeserializerThrows_ReturnsDefaultSettings()
    {
        var json = "{\"deploymentMode\":\"Docker\"}";
        await File.WriteAllTextAsync(_sut.GetSettingsPath(), json);
        _deserializer.Deserialize<AppSettings>(Arg.Any<string>())
            .Returns(_ => throw new Exception("boom"));

        var settings = await _sut.LoadAsync();
        AssertDefaultSettings(settings);
    }

    // --- LoadAsync: valid file ---

    [Fact]
    public async Task LoadAsync_ValidFile_ReturnsParsedSettings()
    {
        var json = "{\"deploymentMode\":\"Docker\"}";
        var expected = new AppSettings { DeploymentMode = DeploymentMode.Docker };
        await File.WriteAllTextAsync(_sut.GetSettingsPath(), json);
        _deserializer.Deserialize<AppSettings>(json).Returns(expected);

        var result = await _sut.LoadAsync();

        result.DeploymentMode.Should().Be(DeploymentMode.Docker);
        result.Docker.Should().NotBeNull();
        result.Binary.Should().NotBeNull();
    }

    // --- SaveAsync ---

    [Fact]
    public async Task SaveAsync_NullSettings_ThrowsArgumentNullException()
    {
        var act = () => _sut.SaveAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("settings");
    }

    [Fact]
    public async Task SaveAsync_ValidSettings_WritesToFile()
    {
        var settings = new AppSettings { DeploymentMode = DeploymentMode.Docker };
        _serializer.Serialize(settings, Arg.Any<JsonSerializationConfiguration>()).Returns("{\"indented\":true}");

        await _sut.SaveAsync(settings);

        File.Exists(_sut.GetSettingsPath()).Should().BeTrue();
        var written = await File.ReadAllTextAsync(_sut.GetSettingsPath());
        written.Should().Be("{\"indented\":true}");
    }

    [Fact]
    public async Task SaveAsync_CreatesDirectoryIfMissing()
    {
        Directory.Delete(_tempDir, recursive: true);
        var settings = new AppSettings();
        _serializer.Serialize(settings, Arg.Any<JsonSerializationConfiguration>()).Returns("{}");

        await _sut.SaveAsync(settings);

        Directory.Exists(_tempDir).Should().BeTrue();
    }

    // --- LoadAsync: Normalize fills in null sub-objects ---

    [Fact]
    public async Task LoadAsync_ValidFile_NullDocker_NormalizesDockerToDefault()
    {
        var json = "{\"deploymentMode\":\"Binary\"}";
        var deserialized = new AppSettings { DeploymentMode = DeploymentMode.Binary, Docker = null!, Binary = null! };
        await File.WriteAllTextAsync(_sut.GetSettingsPath(), json);
        _deserializer.Deserialize<AppSettings>(json).Returns(deserialized);

        var result = await _sut.LoadAsync();

        result.Docker.Should().NotBeNull();
        result.Binary.Should().NotBeNull();
    }

    // --- Helpers ---

    private static void AssertDefaultSettings(AppSettings settings)
    {
        settings.DeploymentMode.Should().Be(DeploymentMode.Binary);
        settings.Docker.Should().NotBeNull();
        settings.Docker.ImageName.Should().Be("cortexiumlabs/vectra");
        settings.Docker.ContainerName.Should().Be("vectra-gateway");
        settings.Docker.Port.Should().Be(7080);
        settings.Binary.Should().NotBeNull();
    }
}
