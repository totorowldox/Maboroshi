using OpenAI.Chat;

namespace Maboroshi.Memory;

public class History
{
    public string Name { get; set; } = "";
    public Dictionary<string, string> Summary { get; set; } = new();
    public Dictionary<string, string> Personality { get; set; } = new();
    public string OverallHistory { get; set; } = "";
    public string OverallPersonality { get; set; } = "";
    public Dictionary<string, List<Conversation>> Conversations { get; set; } = new();
}

public class Conversation(ChatMessageRole role, string content)
{
    public ChatMessageRole Role { get; set; } = role;
    public string Content { get; set; } = content;
}