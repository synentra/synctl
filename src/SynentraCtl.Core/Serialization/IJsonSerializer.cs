namespace VectraCtl.Core.Serialization;

public interface IJsonSerializer
{
    string Serialize(object? input);
    string Serialize(object? input, JsonSerializationConfiguration configuration);
}