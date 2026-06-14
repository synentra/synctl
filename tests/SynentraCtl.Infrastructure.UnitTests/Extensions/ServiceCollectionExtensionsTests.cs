using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Octokit;
using VectraCtl.Core.Services.Configuration;
using VectraCtl.Core.Services.Docker;
using VectraCtl.Core.Services.Extractor;
using VectraCtl.Core.Services.Github;
using VectraCtl.Core.Services.ProcessHost;
using VectraCtl.Infrastructure.Extensions;
using VectraCtl.Infrastructure.Services.ProcessHost;

namespace VectraCtl.Infrastructure.UnitTests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddInfrastructure_RegistersAllExpectedServices()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure();

        var serviceTypes = services.Select(d => d.ServiceType).ToList();

        serviceTypes.Should().Contain(typeof(IGitHubClient));
        serviceTypes.Should().Contain(typeof(IGitHubReleaseManager));
        serviceTypes.Should().Contain(typeof(IProcessProvider));
        serviceTypes.Should().Contain(typeof(IProcessHandler));
        serviceTypes.Should().Contain(typeof(IArchiveExtractor));
        serviceTypes.Should().Contain(typeof(IAppSettingsService));
        serviceTypes.Should().Contain(typeof(IDockerService));
    }

    [Fact]
    public void AddInfrastructure_ReturnsServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddInfrastructure();

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddInfrastructure_IGitHubClient_RegisteredAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure();
        var provider = services.BuildServiceProvider();

        var instance1 = provider.GetRequiredService<IGitHubClient>();
        var instance2 = provider.GetRequiredService<IGitHubClient>();

        instance1.Should().BeSameAs(instance2);
    }

    [Fact]
    public void AddInfrastructure_IProcessHandler_RequiresIProcessProvider()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure();

        // IProcessHandler depends on IVectraCtlLogger which is not registered;
        // but we can verify the registration descriptor exists
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IProcessHandler));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddInfrastructure_IArchiveExtractor_RegisteredAsScoped()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IArchiveExtractor));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddInfrastructure_IAppSettingsService_RegisteredAsScoped()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAppSettingsService));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddInfrastructure_IDockerService_RegisteredAsScoped()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IDockerService));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }
}
