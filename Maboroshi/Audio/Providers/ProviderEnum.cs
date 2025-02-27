namespace Maboroshi.Audio.Providers;

public enum ProviderEnum
{
    Aivis,
    VoiceVox
}

public static class Providers
{
    public static Dictionary<ProviderEnum, Type> ProviderList = new()
    {
        {ProviderEnum.Aivis, typeof(AivisSpeech)},
        {ProviderEnum.VoiceVox, typeof(VoiceVoxSpeech)},
    };
}