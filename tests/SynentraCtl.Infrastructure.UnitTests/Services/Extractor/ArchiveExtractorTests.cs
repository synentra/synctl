using System.IO.Compression;
using FluentAssertions;
using SynentraCtl.Infrastructure.Services.Extractor;

namespace SynentraCtl.Infrastructure.UnitTests.Services.Extractor;

public class ArchiveExtractorTests : IDisposable
{
    private readonly ArchiveExtractor _sut = new();
    private readonly string _tempDir;

    public ArchiveExtractorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
        GC.SuppressFinalize(this);
    }

    // --- ExtractArchive: unsupported extension ---

    [Fact]
    public void ExtractArchive_UnsupportedExtension_ThrowsNotSupportedException()
    {
        var act = () => _sut.ExtractArchive("file.rar", _tempDir);
        act.Should().Throw<NotSupportedException>().WithMessage("*Unsupported archive format*");
    }

    [Theory]
    [InlineData("archive.7z")]
    [InlineData("archive.bz2")]
    public void ExtractArchive_OtherUnsupportedExtensions_ThrowsNotSupportedException(string filename)
    {
        var act = () => _sut.ExtractArchive(filename, _tempDir);
        act.Should().Throw<NotSupportedException>();
    }

    // --- ExtractArchive: .zip ---

    [Fact]
    public void ExtractArchive_ValidZip_ExtractsFilesSuccessfully()
    {
        var zipPath = Path.Combine(_tempDir, "test.zip");
        var extractTo = Path.Combine(_tempDir, "extracted");

        CreateZipWithFile(zipPath, "hello.txt", "Hello World");

        _sut.ExtractArchive(zipPath, extractTo);

        var extractedFile = Path.Combine(extractTo, "hello.txt");
        File.Exists(extractedFile).Should().BeTrue();
        File.ReadAllText(extractedFile).Should().Be("Hello World");
    }

    [Fact]
    public void ExtractArchive_ValidZip_CreatesOutputDirectory()
    {
        var zipPath = Path.Combine(_tempDir, "test.zip");
        var extractTo = Path.Combine(_tempDir, "new_folder", "nested");

        CreateZipWithFile(zipPath, "file.txt", "content");

        _sut.ExtractArchive(zipPath, extractTo);

        Directory.Exists(extractTo).Should().BeTrue();
    }

    [Fact]
    public void ExtractArchive_ZipWithNestedPath_ExtractsFullPath()
    {
        var zipPath = Path.Combine(_tempDir, "nested.zip");
        var extractTo = Path.Combine(_tempDir, "out");

        CreateZipWithFile(zipPath, "sub/file.txt", "nested content");

        _sut.ExtractArchive(zipPath, extractTo);

        var extractedFile = Path.Combine(extractTo, "sub", "file.txt");
        File.Exists(extractedFile).Should().BeTrue();
    }

    // --- ExtractArchive: .tar.gz and .tgz ---

    [Fact]
    public void ExtractArchive_ValidTarGz_ExtractsFilesSuccessfully()
    {
        var tarGzPath = Path.Combine(_tempDir, "test.tar.gz");
        var extractTo = Path.Combine(_tempDir, "extracted_tar");

        CreateTarGz(tarGzPath, "readme.txt", "TarGz content");

        _sut.ExtractArchive(tarGzPath, extractTo);

        var extractedFile = Path.Combine(extractTo, "readme.txt");
        File.Exists(extractedFile).Should().BeTrue();
        File.ReadAllText(extractedFile).Should().Be("TarGz content");
    }

    [Fact]
    public void ExtractArchive_ValidTgz_ExtractsFilesSuccessfully()
    {
        var tgzPath = Path.Combine(_tempDir, "test.tgz");
        var extractTo = Path.Combine(_tempDir, "extracted_tgz");

        CreateTarGz(tgzPath, "notes.txt", "TGZ content");

        _sut.ExtractArchive(tgzPath, extractTo);

        var extractedFile = Path.Combine(extractTo, "notes.txt");
        File.Exists(extractedFile).Should().BeTrue();
    }

    [Fact]
    public void ExtractArchive_CaseInsensitiveExtension_WorksForZip()
    {
        var zipPath = Path.Combine(_tempDir, "test.ZIP");
        var extractTo = Path.Combine(_tempDir, "extracted_caps");

        CreateZipWithFile(zipPath, "caps.txt", "caps");

        _sut.ExtractArchive(zipPath, extractTo);

        File.Exists(Path.Combine(extractTo, "caps.txt")).Should().BeTrue();
    }

    // --- Helpers ---

    private static void CreateZipWithFile(string zipPath, string entryName, string content)
    {
        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        var entry = zip.CreateEntry(entryName);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
    }

    private static void CreateTarGz(string outputPath, string fileName, string content)
    {
        // Build a minimal tar.gz using SharpCompress writer approach via streams
        var contentBytes = System.Text.Encoding.UTF8.GetBytes(content);

        using var outputStream = File.Create(outputPath);
        using var gzipStream = new System.IO.Compression.GZipStream(outputStream, System.IO.Compression.CompressionMode.Compress);

        // Tar header is 512 bytes
        var header = new byte[512];
        var nameBytes = System.Text.Encoding.ASCII.GetBytes(fileName);
        Array.Copy(nameBytes, header, Math.Min(nameBytes.Length, 100));

        // File size in octal at offset 124
        var sizeOctal = System.Text.Encoding.ASCII.GetBytes(Convert.ToString(contentBytes.Length, 8).PadLeft(11, '0') + " ");
        Array.Copy(sizeOctal, 0, header, 124, sizeOctal.Length);

        // File type: '0' = regular file
        header[156] = (byte)'0';

        // Checksum placeholder
        for (int i = 148; i < 156; i++) header[i] = (byte)' ';

        int checksum = header.Sum(b => b);
        var checksumOctal = System.Text.Encoding.ASCII.GetBytes(Convert.ToString(checksum, 8).PadLeft(6, '0') + "\0 ");
        Array.Copy(checksumOctal, 0, header, 148, checksumOctal.Length);

        gzipStream.Write(header, 0, header.Length);
        gzipStream.Write(contentBytes, 0, contentBytes.Length);

        // Pad to 512-byte boundary
        int padding = (512 - contentBytes.Length % 512) % 512;
        if (padding > 0)
            gzipStream.Write(new byte[padding], 0, padding);

        // Two 512-byte zero blocks = end of archive
        gzipStream.Write(new byte[1024], 0, 1024);
    }
}
