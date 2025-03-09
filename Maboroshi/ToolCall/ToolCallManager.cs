using Maboroshi.Audio;
using Maboroshi.Bot;
using Maboroshi.Memory;
using Maboroshi.Personification;
using Maboroshi.Util;
using OpenAI.Chat;

namespace Maboroshi.ToolCall;

public class ToolCallManager(MaboroshiBot bot)
{
    private List<IAgent> AvailableAgents { get; } =
    [
        bot.Container.GetInstance<PersonificationAgent>(),
        bot.Container.GetInstance<MemoryAgent>(),
        bot.Container.GetInstance<AudioAgent>()
    ];
    
    private List<ToolWithFunction> AvailableTools { get; } = new();

    public void Initiate()
    {
        foreach (var agent in AvailableAgents.Where(agent => agent.Enabled))
        {
            Log.Info($"Adding agent: {agent.Name} : {agent.Description}", "AGENT");
            AvailableTools.AddRange(agent.Tools);
        }
    }

    public void AppendAvailableToolCalls(IList<ChatTool> src)
    {
        AvailableTools.ForEach((t) => src.Add(t.Tool));
    }
    
    public IReadOnlyList<IAgent> GetAvailableAgents() => AvailableAgents;

    public async Task<bool> ResolveToolCall(List<ChatMessage> messages, IEnumerable<ChatToolCall> toolCalls, CancellationToken token)
    {
        var requiresAction = false;
        foreach (var call in toolCalls)
        {
            var tool = AvailableTools.FirstOrDefault((t) => t.Tool.FunctionName == call.FunctionName);
            if (tool == null)
            {
                messages.Add(new ToolChatMessage(call.Id, "Unknown tool call."));
            }
            else
            {
                requiresAction |= tool.RequiresAction;
                Log.Debug($"Tool call: {call.FunctionName} with arguments: {call.FunctionArguments}", "AGENT");
                var ret = await tool.Function(call.FunctionArguments.ToString(), token);
                messages.Add(new ToolChatMessage(call.Id, ret));
            }
        }
        return requiresAction;
    }
}