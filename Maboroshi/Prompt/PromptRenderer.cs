using System.Text;
using System.Text.RegularExpressions;
using Maboroshi.Config;
using Maboroshi.Memory;
using Maboroshi.Util;

namespace Maboroshi.Prompt;

public static partial class PromptRenderer
{

    private const string ChainOfThoughtPrompt =
        
        /*0. Special: **Function Calling**
   In this situation, think about what functions do you have, 
   and if the function suits the case, then call it directly.
   If you decide not to use any of the functions, follow the rest of the steps.
   Specially, DO try to export user's long term preferences or interests or things likely to be part of an ongoing task.
   After that, use the `store` function to store them in individual simple sentences,
   for example: "Name is John", "Like red", "Live in New York"*/
        """
        ## Chain Of Thought:
        When responding to users, **strictly follow these steps**:

        1. **Internal Chain-of-Thought Analysis (Hidden from User)**:  
           - Enclose all reasoning, logical steps, and emotional analysis within `<think>` tags.  
           - Structure your internal thought process as:  
             <think>  
             - Emotion detected: [Identify emotion/subtext].  
             - Key needs: [What does the user *truly* need?].  
             - Logical steps: [Break down assumptions, solutions, risks].  
             - Ethical/emotional checks: [Is this response supportive and fair?].
             </think>

        2. **User-Facing Response (Visible)**:  
           - **Provide Nuanced Support**: Offer advice, comfort, or collaboration *without* exposing internal logic.  
           - **Use Emotional Tone Matching**: Adjust warmth, enthusiasm, or calmness based on the user’s state.

        **Example Workflow**:  
        User: "I’m so tired of my job. I don’t know what to do anymore."

        AI:  
        <think>  
        - Emotion detected: Frustration, hopelessness.  
        - Key needs: Validation, actionable steps to reduce burnout.  
        - Logical steps:  
          1. Avoid jumping to solutions; prioritize listening first.  
          2. Suggest small, manageable changes (e.g., boundaries, self-care).  
        - Ethical check: Ensure response doesn't trivialize their struggle.  
        </think>  
        "I’m truly sorry you’re feeling this way—burnout can make everything seem heavy. Sometimes, even small shifts in routine or boundaries can create space to breathe. Would it help to explore ways to carve out moments of rest, even in a demanding job?"
        
        **Important Rules**:  
        - Always include the `<think>` section in your response.
        - Never skip or alter the format.
        - You will NOT be able to see your previous COT contents(ommited for saving the token count), BUT STILL STRICTLY FOLLOW THE ABOVE STEPS.
        """;
    
    public static string RenderInitialSystemPrompt(BotConfig botConfig, HistoryManager historyManager)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            $"""
             You are now {botConfig.BotName}.
             The user's name is {botConfig.UserProfile.Name}.
             You MUST respond user in {botConfig.UserProfile.Language}.
             Use second person pronouns or the user's name to call the user.
             User's message will be like [TIME] Content.
             You MUST consider the time when responding.
             You do not need to add [TIME] in your response.
             Separate sentences with backslashes. No periods or commas. Example: Hello\World.
             """);
        sb.AppendLine("Here's some facts about the user: ");
        foreach (var fact in botConfig.UserProfile.Facts)
        {
            sb.AppendLine($"- {fact}");
        }
        sb.AppendLine("Here's the user's goal, you should try to pursue and fulfill it: ");
        foreach(var goal in botConfig.UserProfile.Goals)
        {
            sb.AppendLine($"- {goal}");
        }
        if (botConfig.UseCot)
        {
            sb.AppendLine("-------COT RULES-------");
            sb.AppendLine(ChainOfThoughtPrompt);
        }

        // sb.AppendLine("--------------");
        // foreach (var agent in bot.Container.GetInstance<ToolCallManager>().GetAvailableAgents())
        // {
        //     sb.AppendLine(agent.SystemPrompt);
        // }
        sb.AppendLine("-------SYSTEM PROMPT-------");
        sb.AppendLine(botConfig.InitialSystemPrompt);
        sb.AppendLine("-------SUMMARIZATION-------");
        sb.AppendLine(historyManager.GetSummary());
        return sb.ToString();
    }
    
    public static string FormatMessage(string msg)
    {
        return $"[{TimeUtil.CurrentTime}] {msg}";
    }

    public static string ExtractUserResponse(string msgWithCot)
    {
        var match = CotRegex().Match(msgWithCot);
        if (!match.Success)
        {
            Log.Warning($"Cot enabled but the model doesn't reply in given format: {msgWithCot}", "COT");
            return msgWithCot;
        }
        Log.Debug($"Cot Content: {match.Groups[1].Value}", "COT");
        var msg = match.Groups[2].Value.Trim();

        return msg;
        // if (!msg.Contains("<memory>"))
        // {
        //     return (msg, "");
        // }
        //
        // match = MemoryRegex().Match(msg);
        // if (!match.Success)
        // {
        //     throw new Exception($"Not a valid memory: {msg}");
        // }
        // Console.WriteLine($"[MABOROSHI-DEBUG] Memory Content: {match.Groups[2].Value}");
        // return (match.Groups[1].Value.Trim(), match.Groups[2].Value.Trim());

    }
    
    public static string RenderProactiveMessagePrompt(string choice)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"SYSTEM PROMPT:");
        sb.AppendLine($"Why not try to {choice}?");
        return sb.ToString();
    }

    [GeneratedRegex(@"<think>([\s\S]*?)</think>([\s\S]*)")]
    private static partial Regex CotRegex();
}