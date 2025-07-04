using Maboroshi.Memory;
using Maboroshi.Util;

namespace Maboroshi.Bot;

public class MemorySummarizationService(HistoryManager history) : IDisposable
{
    private readonly CancellationTokenSource _cts = new();

    public void StartSummarizationThread(bool refreshOnStartup = false)
    {
        SummarizationThread(refreshOnStartup, _cts.Token).ConfigureAwait(false);
    }

    private async Task SummarizationThread(bool refreshOnStartup = false, CancellationToken token = default)
    {
        try
        {
            if (refreshOnStartup)
            {
                await Summarize();
            }
            
            while (!token.IsCancellationRequested)
            {
                // Calculate the time until the next midnight.
                var now = DateTime.Now;
                var nextMidnight = now.AddDays(1).Date; // Get the start of the next day
                var timeUntilMidnight = nextMidnight - now;

                Log.Debug($"Next memory summarization scheduled in {timeUntilMidnight.TotalHours} hours.", "MEMORY");

                await Task.Delay(timeUntilMidnight, token);

                // Perform the summarization.
                await Summarize();
            }
        }
        catch (OperationCanceledException)
        {
            Log.Info("Memory summarization thread cancelled.", "MEMORY");
        }
        catch (Exception ex)
        {
            Log.Error($"Memory summarization thread encountered an unhandled exception: {ex.Message}", "MEMORY");
            Log.Exception(ex);
        }
    }

    private async Task Summarize()
    {
        Log.Info("Starting daily memory summarization.", "MEMORY");
        try
        {
            await history.SummarizeHistory();
        }
        catch (Exception ex)
        {
            Log.Error($"Error during memory summarization: {ex.Message}", "MEMORY");
            Log.Exception(ex);
        }
        Log.Info("Daily memory summarization complete.", "MEMORY");
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
