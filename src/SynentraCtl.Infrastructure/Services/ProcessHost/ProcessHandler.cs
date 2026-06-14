using VectraCtl.Core.Services.Logger;
using VectraCtl.Core.Services.ProcessHost;

namespace VectraCtl.Infrastructure.Services.ProcessHost;

public class ProcessHandler : IProcessHandler
{
    private readonly IVectraCtlLogger _flowCtlLogger;
    private readonly IProcessProvider _processProvider;

    public ProcessHandler(IVectraCtlLogger flowCtlLogger, IProcessProvider processProvider)
    {
        _flowCtlLogger = flowCtlLogger;
        _processProvider = processProvider;
    }

    public bool IsRunning(string processName, string machineAddress)
    {
        var processes = _processProvider.GetProcessesByName(processName, machineAddress);
        return processes.Length != 0;
    }

    public void Terminate(string processName, string machineAddress)
    {
        var processes = _processProvider.GetProcessesByName(processName, machineAddress);
        if (processes.Length == 0) return;

        foreach (var process in processes)
        {
            _flowCtlLogger.Write($"Process '{process.ProcessName}' killed.");
            process.Kill();
        }
    }

    public bool IsStopped(string processName, string machineAddress, bool force)
    {
        if (!IsRunning(processName, machineAddress))
            return true;

        if (!force)
            return false;

        Terminate(processName, machineAddress);
        return true;
    }
}