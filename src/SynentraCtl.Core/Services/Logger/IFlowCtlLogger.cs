namespace SynentraCtl.Core.Services.Logger;

public interface ISynentraCtlLogger
{
    void WriteError(string message);
    void WriteError(object data);
    void Write(string message);
    void Write(object? data, OutputType outputType = OutputType.Json);
}