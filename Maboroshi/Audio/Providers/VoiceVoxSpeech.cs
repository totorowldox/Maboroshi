using Maboroshi.Bot;
using Maboroshi.Serialization;
using Maboroshi.Util;

namespace Maboroshi.Audio.Providers;

public class VoiceVoxSpeech(MaboroshiBot bot, ITextSerializer serializer) : ISpeechProvider
{
    private static readonly HttpClient Client = new() { BaseAddress = new Uri("http://127.0.0.1:50021") };
    
    private async Task<dynamic> GetSpeakers()
    {
        var response = await Client.GetAsync("/speakers");
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        return serializer.Deserialize<dynamic>(responseBody);
    }

    private async Task<dynamic> GenerateAudioQuery(string text, string speakerId)
    {
        var response = await Client.PostAsync($"/audio_query?text={text}&speaker={speakerId}", null);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        return serializer.Deserialize<dynamic>(responseBody);
    }

    private async Task<Stream> SynthesizeAudio(dynamic query, string speakerId)
    {
        var content = new StringContent(serializer.Serialize(query), System.Text.Encoding.UTF8, "application/json");
        var response = await Client.PostAsync($"/synthesis?speaker={speakerId}", content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync();
    }

    public async Task<Stream> Synthesis(string text)
    {
        var speakerId = bot.BotConfig.Voice.SpeakerId;

        // Get list of speakers
        // var speakers = await GetSpeakers();
        // Console.WriteLine("Available speakers: " + serializer.Serialize(speakers));

        // Generate audio query
        var query = await GenerateAudioQuery(text, speakerId);
        Log.Debug("Audio query generated: " + serializer.Serialize(query), "VOICEVOX-SPEECH");

        // Synthesize audio
        return await SynthesizeAudio(query, speakerId);
    }
    
    public async Task SynthesisAndSaveTo(string text, string outputFile)
    {
        await using var fs = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
        await (await Synthesis(text)).CopyToAsync(fs);
        Log.Debug($"Audio synthesized and saved to {outputFile}", "VOICEVOX-SPEECH");
    }
}