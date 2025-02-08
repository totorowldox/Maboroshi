using System.ClientModel;
using Maboroshi.Bot;
using OpenAI;
using OpenAI.Embeddings;

namespace Maboroshi.Util;

public class VectorizationUtil(MaboroshiBot bot)
{
    private readonly EmbeddingClient _embeddingClient = new(bot.BotConfig.EmbeddingModel,
        new ApiKeyCredential(bot.BotConfig.EmbeddingKey),
        new OpenAIClientOptions() {Endpoint = new Uri(bot.BotConfig.EmbeddingEndpoint)});

    public async Task<float[]> VectorizeText(string text)
    {
        var res = await _embeddingClient.GenerateEmbeddingAsync(text, new EmbeddingGenerationOptions()
        {
            Dimensions = bot.BotConfig.VectorDimension
        });
        //Normalize it first, reduce performance cost
        var vectors = NormalizeVector(res.Value.ToFloats().ToArray());
        
        return vectors;
    }
    
    private static float[] NormalizeVector(float[] vector)
    {
        const float epsilon = 1e-7f;
        
        var squaredSum = vector.Sum(num => num * num);

        var magnitude = (float)Math.Sqrt(squaredSum);

        if (magnitude <= epsilon)
        {
            return new float[vector.Length];
        }

        var normalized = new float[vector.Length];
        for (var i = 0; i < vector.Length; i++)
        {
            normalized[i] = vector[i] / magnitude;
        }

        return normalized;
    }
}