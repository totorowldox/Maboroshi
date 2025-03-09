using Maboroshi.Prompt;

namespace Maboroshi.Bot;

public class ProactiveMessaging(MaboroshiBot bot) : IDisposable
{
    private readonly CancellationTokenSource _cts = new();

    public void StartProactiveThread()
    {
        ProactiveThread(_cts.Token).ConfigureAwait(false);
    }

    private async Task ProactiveThread(CancellationToken token = default)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(bot.BotConfig.Proactive.Interval), token);
                if (IsInDndTime || bot.IsResponding)
                {
                    continue;
                }

                var sendOrNot = Random.Shared.NextDouble() < bot.BotConfig.Proactive.Probability;
                if (!sendOrNot)
                {
                    continue;
                }

                var randomChoice =
                    bot.BotConfig.Proactive.Prompts[
                        Random.Shared.Next(bot.BotConfig.Proactive.Prompts.Length)];

                Console.WriteLine($"\n[MABOROSHI-DEBUG] Triggering a proactive message, content: {randomChoice}");

                await bot.GetResponse(PromptRenderer.RenderProactiveMessagePrompt(randomChoice));
            }
        }
        catch (OperationCanceledException)
        {
            // Disposed
        }
        catch (Exception ex)
        {
            // Request failed
        }
    }
    
    private bool IsInDndTime => bot.BotConfig.Proactive.DndHours.Contains(DateTime.Now.Hour);

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}