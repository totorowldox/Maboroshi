﻿using System.Text;
using System.Text.RegularExpressions;
using Maboroshi.Config;
using Maboroshi.Util;

namespace Maboroshi.Prompt;

public static partial class PromptRenderer
{

    private const string ChainOfThoughtPrompt =
        """
        ## Chain Of Thought:
        When responding to users, **strictly follow these steps**:

        0. Special: **Function Calling**
           In this situation, think about what functions do you have, 
           and if the function suits the case, then call it directly.
           If you decide not to use any of the functions, follow the rest of the steps.

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
        """;
    
    public static string RenderInitialSystemPrompt(BotConfig botConfig)
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
             You should split your response into individual sentences using \, 
             and there should be no periods or commas beside it,
             e.g. Hey\It's clear outside\Wanna hang out?
             """);
        if (botConfig.UseCot)
        {
            sb.AppendLine(ChainOfThoughtPrompt);
        }
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

        // sb.AppendLine("--------------");
        // foreach (var agent in bot.Container.GetInstance<ToolCallManager>().GetAvailableAgents())
        // {
        //     sb.AppendLine(agent.SystemPrompt);
        // }
        sb.AppendLine("--------------");
        sb.AppendLine(botConfig.InitialSystemPrompt);
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
            throw new Exception($"Not a valid message: {msgWithCot}");
        }
        
        Console.WriteLine($"[MABOROSHI-DEBUG] Cot Content: {match.Groups[1].Value}");
        return match.Groups[2].Value.Trim();
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