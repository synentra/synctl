using FluentAssertions;
using System.Reflection;

namespace SynentraCtl.UnitTests;

public class ProgramEntryPointTests
{
    [Fact]
    public async Task Program_Main_WithHelpArgs_ReturnsSuccessCode()
    {
        var appAssembly = typeof(SynentraCtl.Commands.RunCommand).Assembly;
        var programType = appAssembly.GetType("Program", throwOnError: true)!;

        var mainMethod = programType
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            .FirstOrDefault(m =>
            {
                var ps = m.GetParameters();
                return ps.Length == 1 && ps[0].ParameterType == typeof(string[]);
            });

        mainMethod.Should().NotBeNull("top-level Program entry method should exist");

        var invocationResult = mainMethod!.Invoke(null, [new[] { "--help" }]);

        var exitCode = invocationResult switch
        {
            Task<int> task => await task,
            int code => code,
            _ => throw new InvalidOperationException("Unexpected Program entrypoint return type")
        };

        exitCode.Should().BeGreaterThanOrEqualTo(0);
    }
}