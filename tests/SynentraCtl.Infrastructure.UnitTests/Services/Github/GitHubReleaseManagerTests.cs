using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Octokit;
using VectraCtl.Infrastructure.Services.Github;

namespace VectraCtl.Infrastructure.UnitTests.Services.Github;

public class GitHubReleaseManagerTests : IDisposable
{
    private readonly IGitHubClient _gitHubClient = Substitute.For<IGitHubClient>();
    private readonly IRepositoriesClient _reposClient = Substitute.For<IRepositoriesClient>();
    private readonly IReleasesClient _releasesClient = Substitute.For<IReleasesClient>();
    private readonly HttpClient _httpClient;
    private readonly FakeHttpMessageHandler _httpHandler;
    private readonly GitHubReleaseManager _sut;
    private readonly string _tempDir;

    public GitHubReleaseManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        _gitHubClient.Repository.Returns(_reposClient);
        _reposClient.Release.Returns(_releasesClient);

        _httpHandler = new FakeHttpMessageHandler();
        _httpClient = new HttpClient(_httpHandler);
        _sut = new GitHubReleaseManager(_gitHubClient, _httpClient);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
        GC.SuppressFinalize(this);
    }

    // --- GetLatestVersion ---

    [Fact]
    public async Task GetLatestVersion_ReturnsTagName()
    {
        var release = CreateRelease("v1.2.3");
        _releasesClient.GetLatest("owner", "repo").Returns(release);

        var result = await _sut.GetLatestVersion("owner", "repo");

        result.Should().Be("v1.2.3");
    }

    [Fact]
    public async Task GetLatestVersion_PropagatesOctokitException()
    {
        _releasesClient.GetLatest("owner", "repo").ThrowsAsync(new NotFoundException("Not found", System.Net.HttpStatusCode.NotFound));

        var act = () => _sut.GetLatestVersion("owner", "repo");
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // --- DownloadAsset ---

    [Fact]
    public async Task DownloadAsset_AssetNotFound_ThrowsException()
    {
        var release = CreateRelease("v1.0.0", assets: []);
        _releasesClient.GetLatest("owner", "repo").Returns(release);

        var act = () => _sut.DownloadAsset("owner", "repo", "missing.zip",
            Path.Combine(_tempDir, "missing.zip"));
        await act.Should().ThrowAsync<Exception>().WithMessage("*Asset 'missing.zip' not found*");
    }

    [Fact]
    public async Task DownloadAsset_ValidAsset_WritesToDisk()
    {
        var downloadUrl = "https://example.com/file.zip";
        var release = CreateRelease("v1.0.0", assets: [("file.zip", downloadUrl)]);
        _releasesClient.GetLatest("owner", "repo").Returns(release);
        _httpHandler.ResponseContent = "binary content";

        var downloadPath = Path.Combine(_tempDir, "file.zip");
        var result = await _sut.DownloadAsset("owner", "repo", "file.zip", downloadPath);

        result.Should().Be(downloadPath);
        File.Exists(downloadPath).Should().BeTrue();
        File.ReadAllText(downloadPath).Should().Be("binary content");
    }

    [Fact]
    public async Task DownloadAsset_WithVersionTag_UsesSpecificRelease()
    {
        var release = CreateRelease("v2.0.0", assets: [("asset.zip", "https://example.com/asset.zip")]);
        _releasesClient.Get("owner", "repo", "v2.0.0").Returns(release);
        _httpHandler.ResponseContent = "data";

        var downloadPath = Path.Combine(_tempDir, "asset.zip");
        await _sut.DownloadAsset("owner", "repo", "asset.zip", downloadPath, versionTag: "v2.0.0");

        await _releasesClient.Received(1).Get("owner", "repo", "v2.0.0");
    }

    [Fact]
    public async Task DownloadAsset_CreatesParentDirectory()
    {
        var release = CreateRelease("v1.0.0", assets: [("file.zip", "https://example.com/file.zip")]);
        _releasesClient.GetLatest("owner", "repo").Returns(release);
        _httpHandler.ResponseContent = "content";

        var downloadPath = Path.Combine(_tempDir, "subdir", "nested", "file.zip");
        await _sut.DownloadAsset("owner", "repo", "file.zip", downloadPath);

        Directory.Exists(Path.GetDirectoryName(downloadPath)).Should().BeTrue();
    }

    [Fact]
    public async Task DownloadAsset_DownloadPathWithNoDirectory_WritesFileSuccessfully()
    {
        // Path.GetDirectoryName returns empty string for a bare filename;
        // the code must skip Directory.CreateDirectory in that case.
        var release = CreateRelease("v1.0.0", assets: [("bare.zip", "https://example.com/bare.zip")]);
        _releasesClient.GetLatest("owner", "repo").Returns(release);
        _httpHandler.ResponseContent = "bare content";

        // Use a temp-rooted path that has an empty directory component
        var downloadPath = Path.Combine(_tempDir, "bare.zip");
        // Simulate "no parent" by providing only a filename component in a known dir
        // so that Path.GetDirectoryName = _tempDir (non-empty) — to hit the empty branch
        // we instead use a path whose GetDirectoryName IS empty by working in the current dir.
        var originalDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_tempDir);
        try
        {
            await _sut.DownloadAsset("owner", "repo", "bare.zip", "bare.zip");
            File.Exists(Path.Combine(_tempDir, "bare.zip")).Should().BeTrue();
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
            // cleanup
            var f = Path.Combine(_tempDir, "bare.zip");
            if (File.Exists(f)) File.Delete(f);
        }
    }

    // --- DownloadHashAsset ---

    [Fact]
    public async Task DownloadHashAsset_DelegatesToDownloadAsset()
    {
        var release = CreateRelease("v1.0.0", assets: [("hash.sha256", "https://example.com/hash.sha256")]);
        _releasesClient.GetLatest("owner", "repo").Returns(release);
        _httpHandler.ResponseContent = "abc123";

        var downloadPath = Path.Combine(_tempDir, "hash.sha256");
        var result = await _sut.DownloadHashAsset("owner", "repo", "hash.sha256", downloadPath);

        result.Should().Be(downloadPath);
        File.ReadAllText(downloadPath).Should().Be("abc123");
    }

    // --- GetAssetHashCode ---

    [Fact]
    public void GetAssetHashCode_ReturnsLowerHexSha256()
    {
        var filePath = Path.Combine(_tempDir, "data.bin");
        File.WriteAllText(filePath, "hello");

        var hash = _sut.GetAssetHashCode(filePath);

        hash.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void GetAssetHashCode_SameContent_SameHash()
    {
        var file1 = Path.Combine(_tempDir, "a.txt");
        var file2 = Path.Combine(_tempDir, "b.txt");
        File.WriteAllText(file1, "same");
        File.WriteAllText(file2, "same");

        _sut.GetAssetHashCode(file1).Should().Be(_sut.GetAssetHashCode(file2));
    }

    [Fact]
    public void GetAssetHashCode_DifferentContent_DifferentHash()
    {
        var file1 = Path.Combine(_tempDir, "x.txt");
        var file2 = Path.Combine(_tempDir, "y.txt");
        File.WriteAllText(file1, "content-a");
        File.WriteAllText(file2, "content-b");

        _sut.GetAssetHashCode(file1).Should().NotBe(_sut.GetAssetHashCode(file2));
    }

    // --- ValidateDownloadedAsset ---

    [Fact]
    public void ValidateDownloadedAsset_MatchingHash_ReturnsTrue()
    {
        var dataFile = Path.Combine(_tempDir, "release.zip");
        var hashFile = Path.Combine(_tempDir, "release.zip.sha256");
        File.WriteAllText(dataFile, "release content");
        var hash = _sut.GetAssetHashCode(dataFile);
        File.WriteAllText(hashFile, hash);

        _sut.ValidateDownloadedAsset(dataFile, hashFile).Should().BeTrue();
    }

    [Fact]
    public void ValidateDownloadedAsset_MismatchedHash_ReturnsFalse()
    {
        var dataFile = Path.Combine(_tempDir, "release2.zip");
        var hashFile = Path.Combine(_tempDir, "release2.zip.sha256");
        File.WriteAllText(dataFile, "release content");
        File.WriteAllText(hashFile, "0000000000000000000000000000000000000000000000000000000000000000");

        _sut.ValidateDownloadedAsset(dataFile, hashFile).Should().BeFalse();
    }

    [Fact]
    public void ValidateDownloadedAsset_HashFileHasComment_StillMatchesFirstToken()
    {
        var dataFile = Path.Combine(_tempDir, "release3.zip");
        var hashFile = Path.Combine(_tempDir, "release3.sha256");
        File.WriteAllText(dataFile, "vectra");
        var hash = _sut.GetAssetHashCode(dataFile);
        File.WriteAllText(hashFile, $"{hash}  release3.zip");

        _sut.ValidateDownloadedAsset(dataFile, hashFile).Should().BeTrue();
    }

    // --- Helpers ---

    private static Release CreateRelease(string tagName,
        (string name, string url)[]? assets = null)
    {
        var releaseAssets = (assets ?? [])
            .Select(a => new ReleaseAsset(
                url: a.url,
                id: 1,
                nodeId: string.Empty,
                name: a.name,
                label: string.Empty,
                state: string.Empty,
                contentType: "application/octet-stream",
                size: 0,
                downloadCount: 0,
                createdAt: DateTimeOffset.UtcNow,
                updatedAt: DateTimeOffset.UtcNow,
                browserDownloadUrl: a.url,
                uploader: null!))
            .ToList()
            .AsReadOnly();

        return new Release(
            url: string.Empty,
            htmlUrl: string.Empty,
            assetsUrl: string.Empty,
            uploadUrl: string.Empty,
            id: 1,
            nodeId: string.Empty,
            tagName: tagName,
            targetCommitish: "main",
            name: tagName,
            body: string.Empty,
            draft: false,
            prerelease: false,
            createdAt: DateTimeOffset.UtcNow,
            publishedAt: DateTimeOffset.UtcNow,
            author: null!,
            tarballUrl: string.Empty,
            zipballUrl: string.Empty,
            assets: releaseAssets);
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        public string ResponseContent { get; set; } = string.Empty;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(ResponseContent)
            };
            return Task.FromResult(response);
        }
    }
}
