using System.Diagnostics;
using Maboroshi.Serialization;
using Maboroshi.Util;
using OpenAI.Chat;

// ReSharper disable RedundantCast

namespace Maboroshi.Memory;

public record History(ChatMessageRole Role, string Content)
{
    public ChatMessageRole Role { get; set; } = Role;
    public string Content { get; set; } = Content;
}

public class HistoryManager(ITextSerializer serializer)
{
    private List<History> Histories { get; set; } = [];
    private string _savePath = string.Empty;

    public List<ChatMessage> GetRecentHistory(int topK) => Histories.TakeLast(topK).ToList().Select(msg =>
    {
        return msg.Role switch
        {
            ChatMessageRole.User => ChatMessage.CreateUserMessage(msg.Content) as ChatMessage,
            ChatMessageRole.Assistant => ChatMessage.CreateAssistantMessage(msg.Content) as ChatMessage,
            _ => throw new UnreachableException()
        };
    }).ToList();
    
    public void AddMessage(ChatMessage msg) => Histories.Add(msg switch
    {
        UserChatMessage userChatMessage => new History(ChatMessageRole.User, userChatMessage.Content[0].Text),
        AssistantChatMessage assistantChatMessage => new History(ChatMessageRole.Assistant, assistantChatMessage.Content[0].Text),
        _ => throw new UnreachableException()
    });

    public void Save()
    {
        File.WriteAllText(_savePath, serializer.Serialize(Histories));
        Log.Info("History file saved.");
    }

    public void Load(string savePath)
    {
        _savePath = savePath;
        if (!File.Exists(savePath))
        {
            return;
        }
        var content = File.ReadAllText(savePath);
        Histories = serializer.Deserialize<List<History>>(content) ?? [];
        Log.Info("History file loaded.");
    }
}