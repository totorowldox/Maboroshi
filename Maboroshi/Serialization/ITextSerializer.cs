namespace Maboroshi.Serialization;

public interface ITextSerializer
{
    public T Deserialize<T>(string target);

    public string Serialize<T>(T source);
}