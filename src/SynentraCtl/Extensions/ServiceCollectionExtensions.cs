using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Synentra.Client.Extensions;
using VectraCtl.ApplicationBuilders;
using VectraCtl.Core.Serialization;
using VectraCtl.Core.Services.Location;
using VectraCtl.Core.Services.Logger;
using VectraCtl.Infrastructure.Extensions;
using VectraCtl.Infrastructure.Serialization;
using VectraCtl.Services.Location;
using VectraCtl.Services.Logger;
using VectraCtl.Services.Version;

namespace VectraCtl.Extensions;

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
            .AddScoped<IVectraCtlLogger, SpectreConsoleLogger>()
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