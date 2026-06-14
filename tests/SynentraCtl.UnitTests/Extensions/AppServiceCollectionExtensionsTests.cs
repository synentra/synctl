using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using SynentraCtl.ApplicationBuilders;
using SynentraCtl.Core.Serialization;
using SynentraCtl.Core.Services.Location;
using SynentraCtl.Core.Services.Logger;
using SynentraCtl.Extensions;
using SynentraCtl.Services.Version;

namespace SynentraCtl.UnitTests.Extensions;

public class AppServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCancellationTokenSource_RegistersCancellationTokenSource()
    {
        var services = new ServiceCollection();
        var result = services.AddCancellationTokenSource();

        result.Should().BeSameAs(services);
        services.Select(d => d.ServiceType).Should().Contain(typeof(CancellationTokenSource));
    }

    [Fact]
    public void AddCommands_ReturnsServiceCollection()
    {
        var services = new ServiceCollection();
        var result = services.AddCommands();
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddApplication_RegistersExpectedServices()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddApplication();

        var types = services.Select(d => d.ServiceType).ToList();

        types.Should().Contain(typeof(ILocation));
        types.Should().Contain(typeof(ISynentraCtlLogger));
        types.Should().Contain(typeof(IJsonSerializer));
        types.Should().Contain(typeof(IJsonDeserializer));
        types.Should().Contain(typeof(ICliApplicationBuilder));
        types.Should().Contain(typeof(IVersion));
        types.Should().Contain(typeof(IVersionInfoProvider));
        types.Should().Contain(typeof(IFileSystem));
        types.Should().Contain(typeof(IAnsiConsole));
    }
}
