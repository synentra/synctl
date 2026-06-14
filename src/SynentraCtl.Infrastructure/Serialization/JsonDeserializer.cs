using System.Text.Json;
using System.Text.Json.Serialization;
using SynentraCtl.Core.Exceptions;
using SynentraCtl.Core.Serialization;

namespace SynentraCtl.Infrastructure.Serialization;

public class JsonDeserializer : IJsonDeserializer
{
    public T Deserialize<T>(string? input)
    {
        return Deserialize<T>(input, new JsonSerializationConfiguration());
    }

    public T Deserialize<T>(string? input, JsonSerializationConfiguration configuration)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new SynentraCtlException("Input value can't be empty or null.");
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = configuration.NameCaseInsensitive,
                WriteIndented = configuration.Indented,
                PropertyNamingPolicy = configuration.NameCaseInsensitive
                    ? null
                    : JsonNamingPolicy.CamelCase
            };

            if (configuration.Converters is not null)
            {
                foreach (var converter in configuration.Converters)
                {
                    options.Converters.Add((JsonConverter)converter);
                }
            }

            var result = System.Text.Json.JsonSerializer.Deserialize<T>(input, options);

            return result is null ? throw new SynentraCtlException("Deserialization returned null.") : result;
        }
        catch (Exception ex)
        {
            throw new SynentraCtlException(ex.Message);
        }
    }
}