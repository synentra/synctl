using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Synentra.Client.Extensions;
using SynentraCtl.ApplicationBuilders;
using SynentraCtl.Core.Serialization;
using SynentraCtl.Core.Services.Location;
using SynentraCtl.Core.Services.Logger;
using SynentraCtl.Infrastructure.Extensions;
using SynentraCtl.Infrastructure.Serialization;
using SynentraCtl.Services.Location;
using SynentraCtl.Services.Logger;
using SynentraCtl.Services.Version;

namespace SynentraCtl.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCancellationTokenSource(this IServiceCollection services)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        services.AddSingleton(cancellationTokenSource);
        return services;
    }

    public static IServiceCollection AddCommands(this IServiceCollection services)
    {
        return services;
    }

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services
            .AddInfrastructure()
            .AddLogging(c => c.ClearProviders())
            .AddScoped<ILocation, LocationService>()
            .AddScoped<ISynentraCtlLogger, SpectreConsoleLogger>()
            .AddScoped<IFileSystem, FileSystemWrapper>()
            .AddScoped<IVersionInfoProvider, VersionInfoProvider>()
            .AddTransient<IVersion, VersionHandler>()
            .AddScoped<IJsonSerializer, JsonSerializer>()
            .AddScoped<IJsonDeserializer, JsonDeserializer>()
            .AddTransient<ICliApplicationBuilder, CliApplicationBuilder>()
            .AddSingleton(AnsiConsole.Console)
            .AddSynentraClient(options =>
            {
                options.BaseUrl = "http://localhost:7080";
            });

        return services;
    }
}