namespace VectraCtl.Infrastructure.Services.ProcessHost;

public interface IProcessWrapper
{
    string ProcessName { get; }
    void Kill();
}