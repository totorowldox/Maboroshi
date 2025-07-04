using System.ClientModel;
using System.Text.Json;
using Maboroshi.Bot;
using Maboroshi.Util;
using OpenAI;
using OpenAI.Chat;

namespace Maboroshi.Memory;

public class MemorySummary(MaboroshiBot bot, VectorizationUtil vectorizationUtil)
{
    private readonly ChatClient _summaryClient = new(bot.BotConfig.SummarizeModel,
        new ApiKeyCredential(bot.BotConfig.ApiKey),
        new OpenAIClientOptions() {Endpoint = new Uri(bot.BotConfig.ApiEndpoint)});

    private async Task<string> GenerateText(string prompt)
    {
        var retries = 0;
        var response = "";
        while (retries++ <= 3)
        {
            try
            {
                response = (await _summaryClient.CompleteChatAsync(prompt)).Value.Content[0].Text;
                break;
            }
            catch
            {
                Log.Warning("Summarize failed, try cutting length", "MEMORY");
                var cut = 1800 - 200 * retries;
                prompt = prompt.TakeLast(cut).ToString();
            }
        }

        if (response == "")
        {
            Log.Error("Summarize failed", "MEMORY");
        }

        return response;
    }

    public async Task GenerateAndSaveMemory(ChatMessage conv)
    {
        var prompt =
            $$"""
              Analyze if this statement should be stored in long-term memory, considering:
              1. Contains personal details (identity, relationships, preferences, life events)
              2. Non-trivial with lasting significance
              3. Useful for future conversation continuity
              4. No sensitive/private information
              
              For qualifying statements:
              - Create a concise 3-8 word summary for vectorization
              - Maintain core meaning while removing temporal/transient elements
              
              Statement: "{{(conv is AssistantChatMessage ? bot.BotConfig.BotName : "user")}} : {{conv.Content[0].Text}}"
              
              Output JSON:
              {
                  "store": <boolean>,
                  "summary": "<compact phrase | null>",
                  "reason": "<criteria-based explanation>"
              }
              
              Examples:
              1. "My sister just moved to Paris for her MBA"
              → {
                  "store": true,
                  "summary": "Sister pursuing MBA in Paris",
                  "reason": "Family event with geographical significance"
                 }
              
              2. "I'm eating toast right now"
              → {
                  "store": false,
                  "summary": null,
                  "reason": "Temporary action without lasting relevance"
                 }
              
              3. "I prefer tea over coffee every morning"
              → {
                  "store": true,
                  "summary": "Prefers tea to coffee",
                  "reason": "Recurring beverage preference"
                 }
                 
              Output JSON only, without markdown formatting.
              """;

        var respond = await GenerateText(prompt);
        try
        {
            using var document = JsonDocument.Parse(respond);
            var root = document.RootElement;

            var store = root.GetProperty("store").GetBoolean();
            var summary = root.GetProperty("summary").GetString();
            var reason = root.GetProperty("reason").GetString();

            if (store)
            {
                Log.Debug($"Store memory ({summary}) for \"{reason}\"", "MEMORY");
                var vectors = await vectorizationUtil.VectorizeText(summary);
                bot.VectorDatabase.AddText(summary, vectors);
            }
            else
            {
                Log.Debug("Not to memorize", "MEMORY");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Not able to deserialize json: {respond}", "MEMORY");
            Log.Exception(ex);
        }
    }

    public async Task<string> SummarizeContent(List<Conversation> conversations)
    {
        var name = bot.BotConfig.UserProfile.Name;

        var prompt =
            """
            Please summarize the following dialogue as concisely as possible, extracting the main themes and key information. 
            If there are multiple key events, you may summarize them separately.
            Remember, write it as brief as you can but keep **preciseness**!!
            Format:
            ```
            Theme: [Summarization]
            Information: [Summarization]
            ```
            
            [Dialogue Content]:
            
            """;

        foreach (var conversation in conversations)
        {
            switch (conversation.Role)
            {
                case ChatMessageRole.Assistant:
                    prompt += $"\n{bot.BotConfig.BotName}: {conversation.Content}";
                    break;
                case ChatMessageRole.User:
                    prompt += $"\n{name}: {conversation.Content}";
                    break;
            }
        }
        
        prompt += "Summarization: ";
        return await GenerateText(prompt);
    }
    
    public async Task<string> SummarizePersonality(List<Conversation> conversations)
    {
        var name = bot.BotConfig.UserProfile.Name;

        var prompt =
            """
            Based on the following dialogue, please summarize {name}'s personality traits and emotions, 
            and devise response strategies based on your speculation. 
            Remember, write it as brief as you can but keep **preciseness**!!
            Ignore all dialogues triggered by a SYSTEM PROMPT, 
            additionally, if the user's message in the given dialogues only contains SYSTEM PROMPT, 
            return empty response.
            
            Format:
            ```
            User personality traits: [Summarization]
            Response strategies: [Summarization]
            ```
            
            [Dialogue Content]:
            
            """;

        foreach (var conversation in conversations)
        {
            switch (conversation.Role)
            {
                case ChatMessageRole.Assistant:
                    prompt += $"\n{bot.BotConfig.BotName}: {conversation.Content}";
                    break;
                case ChatMessageRole.User:
                    prompt += $"\n{name}: {conversation.Content}";
                    break;
            }
        }
        
        prompt += "Summarization: ";
        return await GenerateText(prompt);
    }

    public async Task<string> SummarizeOverallHistory(History history)
    {
        var prompt =
            $"""
            Please provide a highly concise summary of the following event, 
            capturing the essential key information as succinctly as possible.
            Please notice that the assistant's name is {bot.BotConfig.BotName},
            
            Summarize the event:
            """;

        foreach (var day in history.Summary)
        {
            prompt += $"At {day.Key}, the analysis shows {day.Value}";
        }
        
        prompt += "Summarization: ";
        return await GenerateText(prompt);
    }
    
    public async Task<string> SummarizeOverallPersonality(History history)
    {
        var prompt =
            $"""
             The following are the user's exhibited personality traits and emotions throughout multiple dialogues, 
             along with appropriate response strategies for the current situation.
             Please notice that the assistant's name is {bot.BotConfig.BotName},
             """;

        foreach (var day in history.Personality)
        {
            prompt += $"At {day.Key}, the analysis shows {day.Value}";
        }
        
        prompt += "Please provide a highly concise and general summary of the user's personality " +
                  "and the most appropriate response strategy for the AI, summarized as:";
        return await GenerateText(prompt);
    }
}