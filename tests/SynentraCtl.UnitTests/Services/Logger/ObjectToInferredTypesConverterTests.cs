using FluentAssertions;
using System.Text.Json;
using VectraCtl.Services.Logger;

namespace VectraCtl.UnitTests.Services.Logger;

public class ObjectToInferredTypesConverterTests
{
    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new ObjectToInferredTypesConverter());
        return options;
    }

    [Fact]
    public void Read_TrueToken_ReturnsTrue()
    {
        var result = JsonSerializer.Deserialize<object>("true", CreateOptions());
        result.Should().Be(true);
    }

    [Fact]
    public void Read_FalseToken_ReturnsFalse()
    {
        var result = JsonSerializer.Deserialize<object>("false", CreateOptions());
        result.Should().Be(false);
    }

    [Fact]
    public void Read_IntegerNumber_ReturnsInt64()
    {
        var result = JsonSerializer.Deserialize<object>("42", CreateOptions());
        result.Should().Be(42L);
    }

    [Fact]
    public void Read_FloatNumber_ReturnsDouble()
    {
        var result = JsonSerializer.Deserialize<object>("3.14", CreateOptions());
        result.Should().BeOfType<double>().Which.Should().BeApproximately(3.14, 0.001);
    }

    [Fact]
    public void Read_StringToken_ReturnsString()
    {
        var result = JsonSerializer.Deserialize<object>("""
            "hello"
            """, CreateOptions());
        result.Should().Be("hello");
    }

    [Fact]
    public void Read_NullToken_ReturnsNull()
    {
        var result = JsonSerializer.Deserialize<object>("null", CreateOptions());
        result.Should().BeNull();
    }

    [Fact]
    public void Read_DateTimeString_ReturnsDateTime()
    {
        var result = JsonSerializer.Deserialize<object>(""""
            "2023-01-15T12:00:00"
            """", CreateOptions());
        result.Should().BeOfType<DateTime>();
    }

    [Fact]
    public void Write_Value_SerializesCorrectly()
    {
        var options = CreateOptions();
        var serialized = JsonSerializer.Serialize<object>(42L, options);
        serialized.Should().Be("42");
    }

    [Fact]
    public void Read_NestedObject_ReturnsExpandoObject()
    {
        var json = """{"key":"value"}""";
        var result = JsonSerializer.Deserialize<object>(json, CreateOptions());
        result.Should().NotBeNull();
    }

    [Fact]
    public void Read_Array_ReturnsList()
    {
        var json = """[1,2,3]""";
        var result = JsonSerializer.Deserialize<object>(json, CreateOptions());
        result.Should().BeOfType<List<object>>();
    }
}
