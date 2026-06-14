using VectraCtl.Core.Serialization;

namespace VectraCtl.Core.UnitTests.Serialization;

public class JsonSerializationConfigurationTests
{
    [Fact]
    public void JsonSerializationConfiguration_DefaultIndented_IsFalse()
    {
        var config = new JsonSerializationConfiguration();

        config.Indented.Should().BeFalse();
    }

    [Fact]
    public void JsonSerializationConfiguration_DefaultNameCaseInsensitive_IsTrue()
    {
        var config = new JsonSerializationConfiguration();

        config.NameCaseInsensitive.Should().BeTrue();
    }

    [Fact]
    public void JsonSerializationConfiguration_DefaultConverters_IsNull()
    {
        var config = new JsonSerializationConfiguration();

        config.Converters.Should().BeNull();
    }

    [Fact]
    public void JsonSerializationConfiguration_SetIndentedTrue_ReturnsTrue()
    {
        var config = new JsonSerializationConfiguration { Indented = true };

        config.Indented.Should().BeTrue();
    }

    [Fact]
    public void JsonSerializationConfiguration_SetNameCaseInsensitiveFalse_ReturnsFalse()
    {
        var config = new JsonSerializationConfiguration { NameCaseInsensitive = false };

        config.NameCaseInsensitive.Should().BeFalse();
    }

    [Fact]
    public void JsonSerializationConfiguration_SetConverters_ReturnsSetList()
    {
        var converters = new List<object> { "converter1", 42 };
        var config = new JsonSerializationConfiguration { Converters = converters };

        config.Converters.Should().BeSameAs(converters);
        config.Converters.Should().HaveCount(2);
    }

    [Fact]
    public void JsonSerializationConfiguration_SetConvertersToEmpty_ReturnsEmptyList()
    {
        var config = new JsonSerializationConfiguration { Converters = [] };

        config.Converters.Should().NotBeNull();
        config.Converters.Should().BeEmpty();
    }
}
