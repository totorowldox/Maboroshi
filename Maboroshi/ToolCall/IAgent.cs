namespace Maboroshi.ToolCall;

public interface IAgent
{
    public bool Enabled { get; }
    public List<ToolWithFunction> Tools { get; }
    public string Name { get; }
    public string Description { get; }
    public string SystemPrompt { get; }
}