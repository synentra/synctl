using Microsoft.Extensions.DependencyInjection;
using Octokit;
using VectraCtl.Core.Services.Configuration;
using VectraCtl.Core.Services.Docker;
using VectraCtl.Core.Services.Extractor;
using VectraCtl.Core.Services.Github;
using VectraCtl.Core.Services.ProcessHost;
using VectraCtl.Infrastructure.Services.Configuration;
using VectraCtl.Infrastructure.Services.Docker;
using VectraCtl.Infrastructure.Services.Extractor;
using VectraCtl.Infrastructure.Services.Github;
using VectraCtl.Infrastructure.Services.ProcessHost;

namespace VectraCtl.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services
                .AddSingleton<IGitHubClient>(new GitHubClient(new ProductHeaderValue("vectra")))
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
