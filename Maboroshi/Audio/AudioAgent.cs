using System.Text.Json;
using Maboroshi.Audio.Providers;
using Maboroshi.Bot;
using Maboroshi.ToolCall;
using OpenAI.Chat;

namespace Maboroshi.Audio;

public class AudioAgent(MaboroshiBot bot, ISpeechProvider speech) : IAgent
{
    private static readonly ChatTool SpeakDefinition = ChatTool.CreateFunctionTool("speak",
        "Use it tool when you think you should send a voice message to the user. " +
        "Returns OK",
        BinaryData.FromBytes("""
                             {
                                 "type": "object",
                                 "properties": {
                                     "text": {
                                         "type": "string",
                                         "description": "The text to be spoken. USE ONLY IN JAPANESE"
                                     }
                                 },
                                 "required": [ "text" ]
                             }
                             """u8.ToArray()));

    private ToolWithFunction SpeakTool => new(SpeakDefinition, Speak, false);

    private async Task<string> Speak(string jsonInput, CancellationToken token)
    {
        var hasText = JsonDocument.Parse(jsonInput).RootElement.TryGetProperty("text", out var text);
        if (!hasText)
        {
            return "text argument is required";
        }
        try
        {
            var msg = text.GetString()!;
            var fileName = Path.GetTempFileName();
            await speech.SynthesisAndSaveTo(msg, fileName);
            await bot.SendToUser(msg, fileName);
            
            // Voice input is also message!
            bot.AppendAssistantHistory(msg);
        }
        catch
        {
            Console.WriteLine("[MABOROSHI-DEBUG] Audio synthesis failed");
        }
        return "OK";
    }

    public bool Enabled => bot.BotConfig.Voice.Enable;
    public List<ToolWithFunction> Tools => [SpeakTool];
    public string Name => "Audio";
    public string Description => "Enable AI to speak";
    public string SystemPrompt => "Use the `speak` tool when you think you should send a voice message to the user.";
}