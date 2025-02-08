using System.Text;
using Maboroshi.Bot;
using Maboroshi.Config;
using Maboroshi.ToolCall;
using Maboroshi.Util;

namespace Maboroshi.Prompt;

public static class PromptRenderer
{
    public static string RenderInitialSystemPrompt(MaboroshiBot bot, BotConfig botConfig)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            $"""
             The user's name is {botConfig.UserProfile.Name}.
             You MUST respond user in {botConfig.UserProfile.Language}.
             Use second person pronouns or the user's name to call the user.
             User's message will be like [TIME] Content.
             You MUST consider the time when responding.
             You do not need to add [TIME] in your response.
             You should split your response using \, e.g. "Hello, how are you?\I'm fine\what about you?"
             """);
        sb.AppendLine("Here's some facts about the user: ");
        foreach (var fact in botConfig.UserProfile.Facts)
        {
            sb.AppendLine($"- {fact}");
        }
        sb.AppendLine("Here's the user's goal, you should try to fulfill it: ");
        foreach(var goal in botConfig.UserProfile.Goals)
        {
            sb.AppendLine($"- {goal}");
        }

        sb.AppendLine("--------------");
        foreach (var agent in bot.Container.GetInstance<ToolCallManager>().GetAvailableAgents())
        {
            sb.AppendLine(agent.SystemPrompt);
        }
        sb.AppendLine("--------------");
        sb.AppendLine(botConfig.InitialSystemPrompt);
        return sb.ToString();
    }
    
    public static string FormatMessage(string msg)
    {
        return $"[{TimeUtil.CurrentTime}] {msg}";
    }

    public static string RenderProactiveMessagePrompt(string choice)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"SYSTEM PROMPT: (It's now {TimeUtil.CurrentTime})");
        sb.AppendLine($"Why not try to {choice}?");
        return sb.ToString();
    }
}