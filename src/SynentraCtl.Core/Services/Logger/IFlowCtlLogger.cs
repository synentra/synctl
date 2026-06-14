namespace VectraCtl.Core.Services.Logger;

public interface IVectraCtlLogger
{
    void WriteError(string message);
    void WriteError(object data);
    void Write(string message);
    void Write(object? data, OutputType outputType = OutputType.Json);
}