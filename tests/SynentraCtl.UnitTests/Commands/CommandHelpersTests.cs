using FluentAssertions;
using NSubstitute;
using SynentraCtl.Commands;
using SynentraCtl.Core.Services.Docker;
using SynentraCtl.Core.Services.Extractor;
using SynentraCtl.Core.Services.Location;

namespace SynentraCtl.UnitTests.Commands;

public class CommandHelpersTests
{
    [Fact]
    public void IsNewerVersion_ParsedVersions_ReturnsExpectedResult()
    {
        CommandHelpers.IsNewerVersion("v2.0.0", "1.9.9").Should().BeTrue();
        CommandHelpers.IsNewerVersion("v1.0.0", "1.0.0").Should().BeFalse();
    }

    [Fact]
    public void IsNewerVersion_FallbackComparison_HandlesNonParsableSegments()
    {
        CommandHelpers.IsNewerVersion("v2.0.beta", "v1.9.alpha").Should().BeTrue();
        CommandHelpers.IsNewerVersion("v1.0.alpha", "v1.1.beta").Should().BeFalse();
    }

    [Fact]
    public void StripAndNormalizeVersion_WorksForCommonInputs()
    {
        CommandHelpers.StripVersionPrefix("v1.2.3-beta").Should().Be("1.2.3");
        CommandHelpers.NormalizeVersion("1.2.3").Should().Be("v1.2.3");
        CommandHelpers.NormalizeDockerVersion("v1.2.3-beta").Should().Be("1.2.3");
    }

    [Fact]
    public async Task GetPlatformSuffixAsync_ReturnsWindowsAndLinuxSuffixes()
    {
        var docker = Substitute.For<IDockerService>();
        docker.GetDockerModeAsync(Arg.Any<CancellationToken>()).Returns("Windows", "linux");

        var windows = await CommandHelpers.GetPlatformSuffixAsync(docker, CancellationToken.None);
        var linux = await CommandHelpers.GetPlatformSuffixAsync(docker, CancellationToken.None);

        windows.Should().Be("windows-ltsc2022-amd64");
        linux.Should().Be("linux-amd64");
    }

    [Fact]
    public void CopyFilesRecursively_CopiesDirectoriesAndFiles()
    {
        var source = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var destination = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(Path.Combine(source, "sub"));
            var sourceFile = Path.Combine(source, "sub", "file.txt");
            File.WriteAllText(sourceFile, "content");

            CommandHelpers.CopyFilesRecursively(source, destination, CancellationToken.None);

            File.Exists(Path.Combine(destination, "sub", "file.txt")).Should().BeTrue();
            File.ReadAllText(Path.Combine(destination, "sub", "file.txt")).Should().Be("content");
        }
        finally
        {
            if (Directory.Exists(source)) Directory.Delete(source, true);
            if (Directory.Exists(destination)) Directory.Delete(destination, true);
        }
    }

    [Fact]
    public void CopyFilesRecursively_WhenCancelled_DoesNotCopyFiles()
    {
        var source = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var destination = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(Path.Combine(source, "sub"));
            File.WriteAllText(Path.Combine(source, "sub", "file.txt"), "content");

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            CommandHelpers.CopyFilesRecursively(source, destination, cts.Token);

            var copiedFile = Path.Combine(destination, "sub", "file.txt");
            File.Exists(copiedFile).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(source)) Directory.Delete(source, true);
            if (Directory.Exists(destination)) Directory.Delete(destination, true);
        }
    }

    [Fact]
    public void ExtractAsset_ExtractsToVersionedDestinationAndDeletesStaging()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            var extractor = Substitute.For<IArchiveExtractor>();
            extractor.When(x => x.ExtractArchive(Arg.Any<string>(), Arg.Any<string>()))
                .Do(ci =>
                {
                    var extractionDir = ci.ArgAt<string>(1);
                    Directory.CreateDirectory(extractionDir);
                    File.WriteAllText(Path.Combine(extractionDir, "synentra"), "binary");
                });

            var location = Substitute.For<ILocation>();
            location.DefaultSynentraBinaryDirectoryName.Returns(root);

            CommandHelpers.ExtractAsset(extractor, location, "archive.zip", "v1.0.0", CancellationToken.None);

            File.Exists(Path.Combine(root, "v1.0.0", "synentra")).Should().BeTrue();
            Directory.Exists(Path.Combine(root, "v1.0.0", "downloadedFiles")).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void ExtractAssetToRoot_ExtractsToRootAndDeletesStaging()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            var extractor = Substitute.For<IArchiveExtractor>();
            extractor.When(x => x.ExtractArchive(Arg.Any<string>(), Arg.Any<string>()))
                .Do(ci =>
                {
                    var extractionDir = ci.ArgAt<string>(1);
                    Directory.CreateDirectory(extractionDir);
                    File.WriteAllText(Path.Combine(extractionDir, "synentra"), "binary");
                });

            var location = Substitute.For<ILocation>();
            location.DefaultSynentraBinaryDirectoryName.Returns(root);

            CommandHelpers.ExtractAssetToRoot(extractor, location, "archive.zip", CancellationToken.None);

            File.Exists(Path.Combine(root, "synentra")).Should().BeTrue();
            Directory.Exists(Path.Combine(root, "downloadedFiles")).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}