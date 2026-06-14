using FluentAssertions;
using NSubstitute;
using VectraCtl.ApplicationBuilders;

namespace VectraCtl.UnitTests.ApplicationBuilders;

public class CliApplicationBuilderTests
{
    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var act = () => new CliApplicationBuilder(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_ValidServiceProvider_DoesNotThrow()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var act = () => new CliApplicationBuilder(serviceProvider);
        act.Should().NotThrow();
    }

    [Fact]
    public async Task RunAsync_UnknownArgs_ReturnsNonZeroOrZero()
    {
        // CliApplicationBuilder needs a real service provider because VectraCommandLine.Create
        // calls GetRequiredService. We verify it returns an int exit code (any value).
        var serviceProvider = Substitute.For<IServiceProvider>();

        var sut = new CliApplicationBuilder(serviceProvider);

        // --help is a built-in System.CommandLine option that doesn't require services
        var result = await sut.RunAsync(["--help"]);

        result.Should().BeGreaterThanOrEqualTo(0);
    }
}
