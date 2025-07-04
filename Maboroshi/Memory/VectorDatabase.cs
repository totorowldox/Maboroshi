using Maboroshi.Bot;
using Maboroshi.Serialization;
using Maboroshi.Util;

namespace Maboroshi.Memory;

public class VectorEntry
{
    public string Text { get; init; } = "";
    public float[] Vector { get; init; } = [];
    
    public DateTime Date { get; init; } = DateTime.Now;
}

public class QueryResult
{
    public string Text { get; init; } = "";
    public float SimilarityScore { get; init; }
}

public class VectorDatabase(MaboroshiBot bot)
{
    private List<VectorEntry> _entries = [];
    private readonly Lock _lock = new();

    public void AddText(string text, float[] vector)
    {
        lock (_lock)
        {
            _entries.Add(new VectorEntry { Text = text, Vector = vector, Date = DateTime.Now });
            Save(bot.BotConfig.VectorDbFile);
        }
    }

    public IEnumerable<QueryResult> Query(float[] queryVector, int topK)
    {

        lock (_lock)
        {
            var results = new List<QueryResult>();

            foreach (var entry in _entries)
            {
                var similarity = ComputeCosineSimilarity(queryVector, entry.Vector);
                results.Add(new QueryResult
                {
                    Text = entry.Text,
                    SimilarityScore = similarity
                });
            }

            return results.OrderByDescending(r => r.SimilarityScore).Take(topK);
        }
    }

    public bool Delete(string text)
    {

        lock (_lock)
        {
            var deleted = _entries.RemoveAll(e => e.Text == text) > 0;
            Save(bot.BotConfig.VectorDbFile);
            return deleted;
        }
    }

    private void Save(string filePath)
    {

        lock (_lock)
        {
            var content = bot.Container.GetInstance<ITextSerializer>().Serialize(_entries);
            File.WriteAllText(filePath, content);
        }
    }

    public void Load(string filePath)
    {

        lock (_lock)
        {
            if (!File.Exists(filePath))
            {
                Log.Debug("No vector database found, creating a new one.", "MEMORY");
                _entries = [];
                return;
            }

            var content = File.ReadAllText(filePath);
            _entries = bot.Container.GetInstance<ITextSerializer>().Deserialize<List<VectorEntry>>(content) ?? [];
        }
    }

    /// <summary>
    /// Assuming two vector are the same size and are normalized
    /// </summary>
    /// <param name="vectorA"></param>
    /// <param name="vectorB"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private static float ComputeCosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length)
            throw new ArgumentException("Vectors must be of the same length");

        return vectorA.Select((t, i) => t * vectorB[i]).Sum();
    }
}