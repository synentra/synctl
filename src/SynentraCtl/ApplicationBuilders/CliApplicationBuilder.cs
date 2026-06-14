using SynentraCtl.Commands;

namespace SynentraCtl.ApplicationBuilders;

public class CliApplicationBuilder(IServiceProvider serviceProvider) : ICliApplicationBuilder
{
    private readonly IServiceProvider _serviceProvider = serviceProvider 
        ?? throw new ArgumentNullException(nameof(serviceProvider));

    public async Task<int> RunAsync(string[] args)
    {
        var rootCommand = SynentraCommandLine.Create(_serviceProvider, args);
        return await rootCommand.Parse(args).InvokeAsync();
    }
}