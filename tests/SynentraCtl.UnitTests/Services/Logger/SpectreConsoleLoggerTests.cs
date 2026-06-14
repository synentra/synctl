using FluentAssertions;
using NSubstitute;
using Spectre.Console;
using Spectre.Console.Rendering;
using VectraCtl.Core.Serialization;
using VectraCtl.Core.Services.Logger;
using VectraCtl.Infrastructure.Serialization;
using VectraCtl.Services.Logger;

namespace VectraCtl.UnitTests.Services.Logger;

public class SpectreConsoleLoggerTests
{
    private readonly IAnsiConsole _console;
    private readonly IJsonSerializer _serializer;
    private readonly SpectreConsoleLogger _sut;

    public SpectreConsoleLoggerTests()
    {
        _console = Substitute.For<IAnsiConsole>();
        _serializer = new JsonSerializer();
        _sut = new SpectreConsoleLogger(_console, _serializer);
    }

    [Fact]
    public void Constructor_NullConsole_ThrowsArgumentNullException()
    {
        var act = () => new SpectreConsoleLogger(null!, _serializer);
        act.Should().Throw<ArgumentNullException>().WithParameterName("console");
    }

    [Fact]
    public void Constructor_NullSerializer_ThrowsArgumentNullException()
    {
        var act = () => new SpectreConsoleLogger(_console, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("serializer");
    }

    // GenerateXml

    [Fact]
    public void GenerateXml_NullData_ReturnsEmpty()
    {
        SpectreConsoleLogger.GenerateXml(null).Should().BeEmpty();
    }

    [Fact]
    public void GenerateXml_WhitespaceData_ReturnsEmpty()
    {
        SpectreConsoleLogger.GenerateXml("   ").Should().BeEmpty();
    }

    [Fact]
    public void GenerateXml_SimpleJsonObject_ReturnsXml()
    {
        var json = """{"name":"Alice","age":30}""";
        var result = SpectreConsoleLogger.GenerateXml(json);

        result.Should().StartWith("<");
        result.Should().Contain("Alice");
        result.Should().Contain("30");
    }

    [Fact]
    public void GenerateXml_JsonArray_ReturnsXmlWithItems()
    {
        var json = """[{"name":"Alice"},{"name":"Bob"}]""";
        var result = SpectreConsoleLogger.GenerateXml(json);

        result.Should().Contain("Alice");
        result.Should().Contain("Bob");
    }

    // GenerateYaml

    [Fact]
    public void GenerateYaml_NullData_ReturnsEmpty()
    {
        SpectreConsoleLogger.GenerateYaml(null).Should().BeEmpty();
    }

    [Fact]
    public void GenerateYaml_WhitespaceData_ReturnsEmpty()
    {
        SpectreConsoleLogger.GenerateYaml("   ").Should().BeEmpty();
    }

    [Fact]
    public void GenerateYaml_SimpleJsonObject_ReturnsYaml()
    {
        var json = """{"name":"Alice","age":30}""";
        var result = SpectreConsoleLogger.GenerateYaml(json);

        result.Should().Contain("Alice");
        result.Should().Contain("30");
    }

    [Fact]
    public void GenerateYaml_JsonArray_ReturnsYamlList()
    {
        var json = """[{"name":"Alice"},{"name":"Bob"}]""";
        var result = SpectreConsoleLogger.GenerateYaml(json);

        result.Should().Contain("Alice");
        result.Should().Contain("Bob");
    }

    // GenerateJson

    [Fact]
    public void GenerateJson_NullData_ReturnsEmpty()
    {
        SpectreConsoleLogger.GenerateJson(null).Should().BeEmpty();
    }

    [Fact]
    public void GenerateJson_ValidString_ReturnsSameString()
    {
        const string json = """{"key":"value"}""";
        SpectreConsoleLogger.GenerateJson(json).Should().Be(json);
    }

    // Write (string)

    [Fact]
    public void Write_String_CallsConsoleMarkupLine()
    {
        _sut.Write("hello world");
        _console.Received(1).Write(Arg.Any<Renderable>());
    }

    // WriteError (string)

    [Fact]
    public void WriteError_String_CallsConsoleMarkupLine()
    {
        _sut.WriteError("something went wrong");
        _console.Received(1).Write(Arg.Any<Renderable>());
    }

    // Write (object, OutputType)

    [Fact]
    public void Write_Object_JsonOutput_CallsConsole()
    {
        _sut.Write(new { key = "value" }, OutputType.Json);
        _console.Received(1).Write(Arg.Any<Renderable>());
    }

    [Fact]
    public void Write_Object_XmlOutput_CallsConsole()
    {
        _sut.Write(new { key = "value" }, OutputType.Xml);
        _console.Received(1).Write(Arg.Any<Renderable>());
    }

    [Fact]
    public void Write_Object_YamlOutput_CallsConsole()
    {
        _sut.Write(new { key = "value" }, OutputType.Yaml);
        _console.Received(1).Write(Arg.Any<Renderable>());
    }

    [Fact]
    public void Write_Object_TableOutput_CallsConsole()
    {
        _sut.Write(new[] { new { name = "Alice", age = 30 } }, OutputType.Table);
        _console.Received(1).Write(Arg.Any<Renderable>());
    }
}
