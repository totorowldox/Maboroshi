using OpenAI.Chat;

namespace Maboroshi.ToolCall;

public delegate Task<string> ToolFunction(string jsonInput, CancellationToken token);

public class ToolWithFunction(ChatTool def, ToolFunction func, bool requiresAction = true)
{
    public ChatTool Tool { get; } = def;
    public ToolFunction Function { get; } = func;
    public bool RequiresAction { get; } = requiresAction;
}