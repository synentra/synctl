using System.Diagnostics;

namespace VectraCtl.Infrastructure.Services.ProcessHost;

public interface IProcessProvider
{
    IProcessWrapper[] GetProcessesByName(string processName, string machineAddress);
}