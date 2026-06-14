using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using VectraCtl.Core.Exceptions;
using VectraCtl.Core.Serialization;
using VectraCtl.Infrastructure.Serialization;
using InfraJsonSerializer = VectraCtl.Infrastructure.Serialization.JsonSerializer;

namespace VectraCtl.Infrastructure.UnitTests.Serialization;

public class JsonSerializerTests
{
    private readonly InfraJsonSerializer _sut = new();

    // --- ContentMineType ---

    [Fact]
    public void ContentMineType_ReturnsApplicationJson()
    {
        InfraJsonSerializer.ContentMineType.Should().Be("application/json");
    }

    // --- Serialize(object?) ---

    [Fact]
    public void Serialize_WithNull_ThrowsVectraCtlException()
    {
        var act = () => _sut.Serialize(null);
        act.Should().Throw<VectraCtlException>().WithMessage("*null*");
    }

    [Fact]
    public void Serialize_SimpleObject_ReturnsValidJson()
    {
        var result = _sut.Serialize(new { name = "test", value = 42 });
        result.Should().Contain("\"name\"").And.Contain("\"test\"");
    }

    [Fact]
    public void Serialize_DefaultConfig_UsesPascalCase()
    {
        // Default config has NameCaseInsensitive=true which disables camelCase policy
        var result = _sut.Serialize(new SampleModel { FirstName = "Alice" });
        result.Should().Contain("\"FirstName\"");
    }

    [Fact]
    public void Serialize_DefaultConfig_IsNotIndented()
    {
        var result = _sut.Serialize(new SampleModel { FirstName = "Alice" });
        result.Should().NotContain("\n");
    }

    // --- Serialize(object?, JsonSerializationConfiguration) ---

    [Fact]
    public void Serialize_WithIndented_ProducesFormattedOutput()
    {
        var result = _sut.Serialize(new SampleModel { FirstName = "Alice" },
            new JsonSerializationConfiguration { Indented = true });
        result.Should().Contain("\n");
    }

    [Fact]
    public void Serialize_WithNameCaseInsensitive_PreservesOriginalCasing()
    {
        var result = _sut.Serialize(new SampleModel { FirstName = "Alice" },
            new JsonSerializationConfiguration { NameCaseInsensitive = true });
        result.Should().Contain("\"FirstName\"");
    }

    [Fact]
    public void Serialize_WithNullInput_ThrowsVectraCtlException()
    {
        var act = () => _sut.Serialize(null, new JsonSerializationConfiguration());
        act.Should().Throw<VectraCtlException>();
    }

    [Fact]
    public void Serialize_RoundTrip_ProducesDeserializableOutput()
    {
        var original = new SampleModel { FirstName = "Bob" };
        var json = _sut.Serialize(original);
        var deserializer = new JsonDeserializer();
        var result = deserializer.Deserialize<SampleModel>(json);
        result.FirstName.Should().Be("Bob");
    }

    [Fact]
    public void Serialize_WithConverters_UsesConverter()
    {
        var config = new JsonSerializationConfiguration
        {
            Converters = [new AlwaysQuotedIntConverter()]
        };
        var result = _sut.Serialize(new { count = 99 }, config);
        // The custom converter wraps int values in quotes
        result.Should().Contain("\"quoted:99\"");
    }

    private class SampleModel
    {
        public string FirstName { get; set; } = string.Empty;
    }

    /// <summary>Custom converter used to exercise the Converters loop.</summary>
    private class AlwaysQuotedIntConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => int.Parse(reader.GetString()!.Replace("quoted:", ""));

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
            => writer.WriteStringValue($"quoted:{value}");
    }
}
