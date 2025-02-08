namespace Maboroshi.Config;

public class BotConfig
{
    public string ApiEndpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiModel { get; set; } = string.Empty;
    public float Temperature { get; set; } = 0.8f;
    public int MaxOutputToken { get; set; } = 2048;
    public bool EnableVectorDb { get; set; } = false;
    public string VectorDbFile { get; set; } = string.Empty;
    public int VectorDimension { get; set; } = 512;
    public int QueryResultTopK { get; set; } = 1;
    public string EmbeddingEndpoint { get; set; } = string.Empty;
    public string EmbeddingKey { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = string.Empty;
    
    public string InitialSystemPrompt { get; set; } = string.Empty;

    public float WaitForUser { get; set; } = 10;

    public float MinimumTime { get; set; } = 1;

    public float TimePerCharacter { get; set; } = 0.05f;
    
    public UserProfile UserProfile { get; set; } = new();

    public ProactiveSettings Proactive { get; set; } = new();
    
    public HistorySettings History { get; set; } = new();
}

public class UserProfile
{
    public string Name { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string[] Facts { get; set; } = [];
    public string[] Goals { get; set; } = [];
}

public class ProactiveSettings
{
    public bool Enable { get; set; } = false;
    public int Interval { get; set; } = 60;
    public double Probability { get; set; } = 0.003f;
    public string[] Prompts { get; set; } = [];
    public int[] DndHours { get; set; } = [];
}

public class HistorySettings
{
    public string SavePath { get; set; } = string.Empty;
    public int BringToContext { get; set; } = 10;
}