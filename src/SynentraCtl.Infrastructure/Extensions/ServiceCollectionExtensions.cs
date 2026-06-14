using Microsoft.Extensions.DependencyInjection;
using Octokit;
using SynentraCtl.Core.Services.Configuration;
using SynentraCtl.Core.Services.Docker;
using SynentraCtl.Core.Services.Extractor;
using SynentraCtl.Core.Services.Github;
using SynentraCtl.Core.Services.ProcessHost;
using SynentraCtl.Infrastructure.Services.Configuration;
using SynentraCtl.Infrastructure.Services.Docker;
using SynentraCtl.Infrastructure.Services.Extractor;
using SynentraCtl.Infrastructure.Services.Github;
using SynentraCtl.Infrastructure.Services.ProcessHost;

namespace SynentraCtl.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services
                .AddSingleton<IGitHubClient>(new GitHubClient(new ProductHeaderValue("synentra")))
                .AddSingleton<IGitHubReleaseManager, GitHubReleaseManager>()
                .AddScoped<IProcessProvider, DefaultProcessProvider>()
                .AddScoped<IProcessHandler, ProcessHandler>()
                .AddScoped<IArchiveExtractor, ArchiveExtractor>()
                .AddScoped<IAppSettingsService, AppSettingsService>()
                .AddScoped<IDockerProcessRunner, SystemDockerProcessRunner>()
                .AddScoped<IDockerService, DockerService>();

        return services;
    }
}
