using System.Text.Json;
using Maboroshi.Bot;
using Maboroshi.ToolCall;
using Maboroshi.Util;
using OpenAI.Chat;

namespace Maboroshi.Memory;

public class MemoryTools(MaboroshiBot bot, VectorizationUtil vectorizationUtil) : IAgent
{
    private static readonly ChatTool StoreDefinition = ChatTool.CreateFunctionTool("store",
        "Use it when you think you should memorize something persistently, usually vital facts of the user, " +
        "should not be AMBIGUOUS. Use simple declarative sentences." +
        "Example: The user’s name is John.\nReturns OK.",
        BinaryData.FromBytes("""
                             {
                                 "type": "object",
                                 "properties": {
                                     "text": {
                                         "type": "string",
                                         "description": "The text to save. USE ENGLISH."
                                     }
                                 },
                                 "required": [ "text" ]
                             }
                             """u8.ToArray()));

    // private static readonly ChatTool QueryDefinition = ChatTool.CreateFunctionTool("query",
    //     "Use it when you need to query something you memorized about user, " +
    //     "Example: What's user's name?\nReturns a list of similar memories.",
    //     BinaryData.FromBytes("""
    //                          {
    //                              "type": "object",
    //                              "properties": {
    //                                  "text": {
    //                                      "type": "string",
    //                                      "description": "The text to query. USE ENGLISH."
    //                                  }
    //                              },
    //                              "required": [ "text" ]
    //                          }
    //                          """u8.ToArray()));
    
    private static readonly ChatTool DeleteDefinition = ChatTool.CreateFunctionTool("delete",
        "Use it when you find something conflicting in the memories and the user's speech, " +
        "you should give out the similar text stored in memory. USE WITH CAUTION." +
        "Example as: The user’s name is Mike.",
        BinaryData.FromBytes("""
                             {
                                 "type": "object",
                                 "properties": {
                                     "text": {
                                         "type": "string",
                                         "description": "The similar text to delete. USE ENGLISH."
                                     }
                                 },
                                 "required": [ "text" ]
                             }
                             """u8.ToArray()));

    private ToolWithFunction StoreTool => new(StoreDefinition, Store);
    
    // public static ToolWithFunction QueryTool => new(QueryDefinition, Query);

    private ToolWithFunction DeleteTool => new(DeleteDefinition, Delete);

    private async Task<string> Store(string jsonInput, CancellationToken token)
    {
        var hasText = JsonDocument.Parse(jsonInput).RootElement.TryGetProperty("text", out var text);
        if (!hasText)
        {
            return "text argument is required";
        }

        var s = text.GetString()!;
        var vectors = await vectorizationUtil.VectorizeText(s);
        bot.VectorDatabase.AddText(s, vectors);
        return "OK";
    }

    // private static async Task<string> Query(string jsonInput, CancellationToken token)
    // {
    //     var hasText = JsonDocument.Parse(jsonInput).RootElement.TryGetProperty("text", out var text);
    //     if (!hasText)
    //     {
    //         return "text argument is required";
    //     }
    //
    //     var s = text.GetString()!;
    //     var vectors = await VectorizationUtil.VectorizeText(s);
    //
    //     var res = MaboroshiBot.VectorDatabase.Query(vectors, MaboroshiBot.BotConfig.QueryResultTopK);
    //
    //     var sb = new StringBuilder();
    //     foreach (var fact in res)
    //     {
    //         Console.WriteLine($"{fact.SimilarityScore} - {fact.Text}");
    //         sb.AppendLine(fact.Text);
    //     }
    //
    //     return sb.ToString();
    // }

    private async Task<string> Delete(string jsonInput, CancellationToken token)
    {
        var hasText = JsonDocument.Parse(jsonInput).RootElement.TryGetProperty("text", out var text);
        if (!hasText)
        {
            return "text argument is required";
        }

        var s = text.GetString()!;
        var vectors = await vectorizationUtil.VectorizeText(s);

        var res = bot.VectorDatabase.Query(vectors, 1);

        return bot.VectorDatabase.Delete(res.First().Text) ? "OK" : "Empty memory";
    }

    public bool Enabled => bot.BotConfig.EnableVectorDb;
    public List<ToolWithFunction> Tools => [StoreTool, DeleteTool];
    public string Name => "Memory";
    public string Description => "Use a vector database to store and query memories.";
    public string SystemPrompt => "Use `store` and `delete` tools to manipulate your memories! " +
                                  "Please make more use of it. You don't have to save memories in the system prompt.";
}