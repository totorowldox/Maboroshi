using System.Runtime.Serialization;

namespace Maboroshi.Serialization;

public class JsonSerializer : ITextSerializer
{
    public T Deserialize<T>(string target)
    {
        return System.Text.Json.JsonSerializer.Deserialize<T>(target) ?? throw new SerializationException("Failed to deserialize.");
    }

    public string Serialize<T>(T source)
    {
        return System.Text.Json.JsonSerializer.Serialize(source);
    }
}