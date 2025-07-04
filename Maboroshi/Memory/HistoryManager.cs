using System.Diagnostics;
using Maboroshi.Bot;
using Maboroshi.Serialization;
using Maboroshi.Util;
using OpenAI.Chat;

// ReSharper disable RedundantCast

namespace Maboroshi.Memory;

public class HistoryManager(MaboroshiBot bot, MemorySummary memorySummary)
{
    private History History { get; set; } = new ();
    private string _savePath = string.Empty;

    public List<ChatMessage> GetRecentHistory(int topK)
    {
        var allConversations = History.Conversations.Values
            .SelectMany(conversations => conversations)
            .ToList();
        var recentConversations = allConversations.TakeLast(topK).ToList();
        return recentConversations.Select(msg =>
        {
            return msg.Role switch
            {
                ChatMessageRole.User => ChatMessage.CreateUserMessage(msg.Content) as ChatMessage,
                ChatMessageRole.Assistant => ChatMessage.CreateAssistantMessage(msg.Content) as ChatMessage,
                _ => throw new UnreachableException()
            };
        }).ToList();
    }
    
    public async Task AddMessage(ChatMessage msg)
    {
        var currentDate = TimeUtil.CurrentDate;

        if (!History.Conversations.TryGetValue(currentDate, out var conversations))
        {
            conversations = [];
            History.Conversations[currentDate] = conversations;
        }

        var conversation = msg switch
        {
            UserChatMessage userChatMessage => new Conversation(ChatMessageRole.User, userChatMessage.Content[0].Text),
            AssistantChatMessage assistantChatMessage => new Conversation(ChatMessageRole.Assistant, assistantChatMessage.Content[0].Text),
            _ => throw new UnreachableException()
        };
        conversations.Add(conversation);

        // Try to summarize memory from the conversation
        // TODO: 这真不是个办法 到底怎么几把整长期记忆啊！！！！！！！！！
        // await memorySummary.GenerateAndSaveMemory(msg);
    }

    public async Task SummarizeHistory()
    {
        foreach (var date in History.Conversations.Keys.ToList()) // Iterate through each day
        {
            if (History.Summary.ContainsKey(date))
            {
                continue;
            }
            var conversations = History.Conversations[date];
            var contentSummary = await memorySummary.SummarizeContent(conversations);
            History.Summary[date] = contentSummary;
            Log.Info($"Summarized content for {date}: {contentSummary}", "MEMORY");
            var personalitySummary = await memorySummary.SummarizePersonality(conversations);
            History.Personality[date] = personalitySummary;
            Log.Info($"Summarized personality for {date}: {personalitySummary}", "MEMORY");
        }
        // Summarize overall history
        History.OverallHistory = await memorySummary.SummarizeOverallHistory(History);
        Log.Info($"Summarized overall history: {History.OverallHistory}", "MEMORY");
        // Summarize overall personality
        History.OverallPersonality = await memorySummary.SummarizeOverallPersonality(History);
        Log.Info($"Summarized overall personality: {History.OverallPersonality}", "MEMORY");
        
        // Refresh system prompt since we have updated the summarization
        bot.RefreshSystemPrompt();
        Save();
    }

    public string GetSummary()
    {
        if (string.IsNullOrEmpty(History.OverallHistory))
        {
            return "";
        }
        return "Here is a summary of your conversation history:\n" +
               $"Your history: {History.OverallHistory}\n" +
               $"User's personality: {History.OverallPersonality}";
    }

    public void Save()
    {
        File.WriteAllText(_savePath, bot.Container.GetInstance<ITextSerializer>().Serialize(History));
        Log.Info("History file saved.", "MEMORY");
    }

    public void Load(string savePath)
    {
        _savePath = savePath;
        if (!File.Exists(savePath))
        {
            History = new History
            {
                Name = bot.BotConfig.UserProfile.Name
            };
            return;
        }
        var content = File.ReadAllText(savePath);
        History = bot.Container.GetInstance<ITextSerializer>().Deserialize<History>(content);
        Log.Info("History file loaded.", "MEMORY");
    }
}