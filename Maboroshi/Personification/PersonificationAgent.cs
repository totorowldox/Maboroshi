using System.Text.Json;
using Maboroshi.ToolCall;
using OpenAI.Chat;

namespace Maboroshi.Personification;

public class PersonificationAgent : IAgent
{
    private static readonly ChatTool DelayDefinition = ChatTool.CreateFunctionTool("delay",
        "Use it when you think you should remain silent for a specific time. " +
        "Returns OK if user doesn't interrupt during the delay, otherwise, Interrupted is returned.",
        BinaryData.FromBytes("""
                             {
                                 "type": "object",
                                 "properties": {
                                     "seconds": {
                                         "type": "number",
                                         "description": "The delay duration, in seconds. Must be integer."
                                     }
                                 },
                                 "required": [ "seconds" ]
                             }
                             """u8.ToArray()));

    private static readonly ChatTool RemainSilentDefinition = ChatTool.CreateFunctionTool("remain_silent",
        "Use it when you think you should remain silent and don't answer the user. Always returns OK.");

    private static ToolWithFunction DelayTool => new(DelayDefinition, Delay);
    private static ToolWithFunction RemainSilentTool => new(RemainSilentDefinition, RemainSilent, false);

    private static async Task<string> Delay(string jsonInput, CancellationToken token)
    {
        var hasSeconds = JsonDocument.Parse(jsonInput).RootElement.TryGetProperty("seconds", out var seconds);
        if (!hasSeconds)
        {
            return "seconds argument is required";
        }
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds.GetInt32()), token);
        }
        catch (OperationCanceledException)
        {
            return "Interrupted";
        }
        return "OK";
    }
    
    private static Task<string> RemainSilent(string jsonInput, CancellationToken token)
    {
        return Task.FromResult("OK");
    }

    public bool Enabled => true;
    public List<ToolWithFunction> Tools => [DelayTool, RemainSilentTool];
    public string Name => "Personification";
    public string Description => "Enable AI to delay a message or remain silent.";
    public string SystemPrompt => """
                                  Use the `delay` tool when you think you should remain silent for a specific time,
                                  useful when you want to delay a message or complete a scheduled task.
                                  Use the `remain_silent` tool when you think you should remain silent and don't answer the user, 
                                  useful when you think you have nothing to say or express feelings like fury.
                                  """;
}