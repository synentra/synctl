using System.Text.Json;
using System.Text.Json.Serialization;
using SynentraCtl.Core.Exceptions;
using SynentraCtl.Core.Serialization;

namespace SynentraCtl.Infrastructure.Serialization;

public class JsonSerializer : IJsonSerializer
{
    /// <summary>
    /// Static JSON content type reference that callers can reuse without instantiating the serializer.
    /// </summary>
    public static string ContentMineType => "application/json";

    public string Serialize(object? input)
    {
        return Serialize(input, new JsonSerializationConfiguration
        {
            Indented = false
        });
    }

    public string Serialize(object? input, JsonSerializationConfiguration configuration)
    {
        try
        {
            if (input is null)
            {
                throw new SynentraCtlException("Input value can't be empty or null.");
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = configuration.Indented,
                PropertyNameCaseInsensitive = configuration.NameCaseInsensitive,
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

            return System.Text.Json.JsonSerializer.Serialize(input, options);
        }
        catch (Exception ex)
        {
            throw new SynentraCtlException(ex.Message);
        }
    }
}