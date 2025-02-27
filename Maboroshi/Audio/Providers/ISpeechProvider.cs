namespace Maboroshi.Audio.Providers;

public interface ISpeechProvider
{
    public Task<Stream> Synthesis(string text);

    public Task SynthesisAndSaveTo(string text, string outputFile);
}