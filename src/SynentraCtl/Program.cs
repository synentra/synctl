using Microsoft.Extensions.DependencyInjection;
using SynentraCtl.ApplicationBuilders;
using SynentraCtl.Core.Services.Logger;
using SynentraCtl.Extensions;

IServiceProvider serviceProvider = default!;

try
{
    IServiceCollection services = new ServiceCollection();

    services
        .AddCancellationTokenSource()
        .AddApplication()
        .AddCommands()
        .AddHttpClient();

    serviceProvider = services.BuildServiceProvider();

    var cli = serviceProvider.GetService<ICliApplicationBuilder>();

    return cli == null 
        ? throw new InvalidOperationException("Something wrong happen during execute the application") 
        : await cli.RunAsync(args);
}
catch (Exception ex)
{
    if (serviceProvider != null)
    {
        var formatter = serviceProvider.GetService<ISynentraCtlLogger>();
        formatter?.WriteError(ex.Message);
    }
    else
    {
        Console.WriteLine(ex.Message);
    }
    return 0;
}