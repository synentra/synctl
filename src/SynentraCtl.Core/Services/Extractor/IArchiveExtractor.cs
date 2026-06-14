namespace VectraCtl.Core.Services.Extractor;

public interface IArchiveExtractor
{
    void ExtractArchive(string archivePath, string extractToFolder);
}
