using Spectre.Console;
using System.Dynamic;
using System.Text.Json;
using System.Xml.Linq;
using VectraCtl.Core.Serialization;
using VectraCtl.Core.Services.Logger;

namespace VectraCtl.Services.Logger;

public class SpectreConsoleLogger : IVectraCtlLogger
{
    private readonly IAnsiConsole _console;
    private readonly IJsonSerializer _serializer;

    public SpectreConsoleLogger(
        IAnsiConsole console,
        IJsonSerializer serializer)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    public void WriteError(string message) => WriteError(new { message });

    public void WriteError(object data)
    {
        var json = SerializeIndented(data);
        _console.MarkupLineInterpolated($"[red]{json.EscapeMarkup()}[/]");
    }

    public void Write(string message) =>
        _console.MarkupLineInterpolated($"{message}");

    public void Write(object? data, OutputType outputType = OutputType.Json)
    {
        var json = SerializeIndented(data);

        switch (outputType)
        {
            case OutputType.Table:
                _console.Write(GenerateTable(json));
                break;

            case OutputType.Xml:
                Write(GenerateXml(json));
                break;

            case OutputType.Yaml:
                Write(GenerateYaml(json));
                break;

            default:
                Write(json);
                break;
        }
    }

    private string SerializeIndented(object? data) =>
        _serializer.Serialize(data,
            new JsonSerializationConfiguration
            {
                Indented = true
            });

    private static Table GenerateTable(string? json)
    {
        var table = new Table
        {
            Border = TableBorder.Simple
        };

        if (string.IsNullOrWhiteSpace(json))
            return table;

        var expandoList = DeserializeToExpandoList(json);

        if (expandoList.Count == 0)
            return table;

        var headers = (IDictionary<string, object?>)expandoList[0];

        foreach (var header in headers.Keys)
        {
            table.AddColumn(header);
        }

        foreach (var item in expandoList)
        {
            var row = ((IDictionary<string, object?>)item)
                .Select(kv => FormatValue(kv.Value))
                .ToArray();

            table.AddRow(row);
        }

        return table;
    }

    private static List<ExpandoObject> DeserializeToExpandoList(string json)
    {
        using var document = JsonDocument.Parse(json);

        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            var list = JsonSerializer.Deserialize<List<ExpandoObject>>(
                json,
                CreateJsonOptions());

            return list
                   ?? throw new InvalidOperationException("Data conversion encountered an error. The system couldn't process the data format.");
        }

        var single = JsonSerializer.Deserialize<ExpandoObject>(
            json,
            CreateJsonOptions());

        return single != null
            ? new List<ExpandoObject> { single }
            : throw new InvalidOperationException("Data conversion encountered an error. The system couldn't process the data format.");
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        options.Converters.Add(new ObjectToInferredTypesConverter());

        return options;
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            ExpandoObject exp => FormatExpando(exp),
            null => string.Empty,
            _ => value.ToString()?.EscapeMarkup() ?? string.Empty
        };
    }

    private static string FormatExpando(ExpandoObject expando)
    {
        return string.Join(
            Environment.NewLine,
            from kv in (IDictionary<string, object?>)expando
            let val = kv.Value?.ToString()?.EscapeMarkup() ?? string.Empty
            select $"{kv.Key}={val}");
    }

    public static string GenerateXml(string? data)
    {
        if (string.IsNullOrWhiteSpace(data))
            return string.Empty;

        using var jsonDocument = JsonDocument.Parse(data);

        var root = ConvertJsonToXml(jsonDocument.RootElement, "item");

        return new XDocument(
            new XElement("root", root))
            .ToString();
    }

    private static XElement ConvertJsonToXml(JsonElement element, string name)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => new XElement(
                name,
                element.EnumerateObject()
                    .Select(p => ConvertJsonToXml(p.Value, p.Name))),

            JsonValueKind.Array => new XElement(
                name,
                element.EnumerateArray()
                    .Select(x => ConvertJsonToXml(x, "item"))),

            _ => new XElement(name, element.ToString())
        };
    }

    public static string GenerateYaml(string? data)
    {
        if (string.IsNullOrWhiteSpace(data))
            return string.Empty;

        var yamlSerializer = new YamlDotNet.Serialization.SerializerBuilder()
            .WithNamingConvention(
                YamlDotNet.Serialization.NamingConventions
                    .CamelCaseNamingConvention.Instance)
            .Build();

        var expandoObjects = DeserializeToExpandoList(data);

        return yamlSerializer.Serialize(
            expandoObjects.Count == 1
                ? (object)expandoObjects[0]
                : expandoObjects);
    }

    public static string GenerateJson(string? data) =>
        data ?? string.Empty;
}

/// <summary>
/// Converts JsonElement values into inferred CLR types.
/// </summary>
public sealed class ObjectToInferredTypesConverter : System.Text.Json.Serialization.JsonConverter<object>
{
    public override object? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,

            JsonTokenType.Number when reader.TryGetInt64(out var l) => l,
            JsonTokenType.Number => reader.GetDouble(),

            JsonTokenType.String when reader.TryGetDateTime(out var dt) => dt,
            JsonTokenType.String => reader.GetString(),

            JsonTokenType.StartObject =>
                JsonSerializer.Deserialize<ExpandoObject>(
                    ref reader,
                    options),

            JsonTokenType.StartArray =>
                JsonSerializer.Deserialize<List<object>>(
                    ref reader,
                    options),

            JsonTokenType.Null => null,

            _ => JsonDocument.ParseValue(ref reader)
                .RootElement
                .Clone()
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        object value,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}