using System.Diagnostics;

namespace SynentraCtl.Infrastructure.Services.ProcessHost;

public interface IProcessProvider
{
    IProcessWrapper[] GetProcessesByName(string processName, string machineAddress);
}