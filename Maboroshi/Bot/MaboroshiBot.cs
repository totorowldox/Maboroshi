using System.ClientModel;
using Maboroshi.Config;
using Maboroshi.Memory;
using Maboroshi.Personification;
using Maboroshi.Prompt;
using Maboroshi.Serialization;
using Maboroshi.ToolCall;
using Maboroshi.Util;
using OpenAI;
using OpenAI.Chat;
using SimpleInjector;

namespace Maboroshi.Bot;

public class MaboroshiBot : IDisposable
{
    public BotConfig BotConfig { get; }
    public VectorDatabase VectorDatabase { get; }
    public bool IsResponding { get; private set; }
    public Container Container { get; }
    
    public Func<string, string, Task> SendToUser { get; }

    private string _systemPrompt;
    private readonly ChatCompletionOptions _chatOption;
    private readonly ChatClient _client;
    private readonly ProactiveMessaging? _proactive;
    private readonly MemorySummarizationService _memorySummarizationService;
    private readonly HistoryManager _history;
    private readonly ToolCallManager _toolCallManager;
    private readonly VectorizationUtil _vectorizationUtil;
    private CancellationTokenSource _cts = new();
    private CancellationTokenSource _waitForUserCts = new();
    private string _userMessageQueue = string.Empty;
    private readonly List<ChatMessageContentPart> _images = [];
    private bool _isWaiting;

    public MaboroshiBot(BotConfig config, Func<string, string, Task> sendToUser, Container? container = null)
    {
        BotConfig = config;
        SendToUser = sendToUser;

        Container = container ?? DefaultContainer;
        
        // Deal with optional features
        if (BotConfig.Voice.Enable)
        {
            var type = Audio.Providers.Providers.ProviderList[BotConfig.Voice.Provider];
            Container.Register(typeof(Audio.Providers.ISpeechProvider), type, Lifestyle.Singleton);
        }
        
        Container.RegisterInstance(this);
        Container.Verify();
        _toolCallManager = Container.GetInstance<ToolCallManager>();
        _vectorizationUtil = Container.GetInstance<VectorizationUtil>();
        _history = Container.GetInstance<HistoryManager>();
        VectorDatabase = Container.GetInstance<VectorDatabase>();

        var api = new OpenAIClient(new ApiKeyCredential(BotConfig.ApiKey),
            new OpenAIClientOptions() {Endpoint = new Uri(BotConfig.ApiEndpoint)});
        _chatOption = new ChatCompletionOptions()
        {
            Temperature = BotConfig.Temperature,
            MaxOutputTokenCount = BotConfig.MaxOutputToken,
            ToolChoice = ChatToolChoice.CreateAutoChoice(),
            AllowParallelToolCalls = true
        };
        _client = api.GetChatClient(BotConfig.ApiModel);
        _history.Load(BotConfig.History.SavePath);

        //Deal with optional features
        VectorDatabase.Load(BotConfig.VectorDbFile);
        if (BotConfig.Proactive.Enable)
        {
            _proactive = Container.GetInstance<ProactiveMessaging>();
            _proactive.StartProactiveThread();
        }
        _toolCallManager.Initiate();

        if (BotConfig.UseTools)
        {
            _toolCallManager.AppendAvailableToolCalls(_chatOption.Tools);
        }

        _memorySummarizationService = Container.GetInstance<MemorySummarizationService>();
        _memorySummarizationService.StartSummarizationThread();

        RefreshSystemPrompt();
    }

    public void AppendImage(BinaryData image)
    {
        _images.Add(ChatMessageContentPart.CreateImagePart(image, "image/jpeg"));
    }
    
    public void AppendImage(Uri imageUri)
    {
        Log.Debug("Appending image to queue");
        _images.Add(ChatMessageContentPart.CreateImagePart(imageUri));
    }

    public async Task GetResponse(string message)
    {
        Log.Debug($"Receive user input: {message}");
        if (_isWaiting)
        {
            await _waitForUserCts.CancelAsync();
            _waitForUserCts.Dispose();
            _waitForUserCts = new CancellationTokenSource();
        }

        if (IsResponding)
        {
            await _cts.CancelAsync();
        }

        EnqueueUserMessage(message);
        if (await WaitForUserAsync())
        {
            return;
        }

        IsResponding = true;
        try
        {
            var messages = PrepareChatMessages();
            var response = await ProcessChatCompletionAsync(messages);
            if (!string.IsNullOrEmpty(response))
            {
                response = BotConfig.UseCot ? PromptRenderer.ExtractUserResponse(response) : response;
                var assistantMessage = ChatMessage.CreateAssistantMessage(response);
                messages.Add(assistantMessage);
                await SendFormattedResponseAsync(response);
                await _history.AddMessage(ChatMessage.CreateUserMessage(_userMessageQueue));
                await _history.AddMessage(assistantMessage);
            }
        }
        catch (OperationCanceledException)
        {
            ResetCancellationToken();
            Log.Debug("Response interrupted.");
        }
        catch (Exception ex)
        {
            Log.Exception(ex);
            throw;
        }
        finally
        {
            ResetState();
        }
    }

    public async Task AppendAssistantHistory(string message)
    {
        await _history.AddMessage(ChatMessage.CreateAssistantMessage(message));
    }
    
    #region Helper Methods

    private void EnqueueUserMessage(string message, bool prependNewLine = true)
    {
        var formattedMessage = PromptRenderer.FormatMessage(message);
        _userMessageQueue += prependNewLine ? '\n' + formattedMessage : formattedMessage;
    }

    /// <summary>
    /// Wait for user input
    /// </summary>
    /// <returns>Is cancelled</returns>
    private async Task<bool> WaitForUserAsync()
    {
        _isWaiting = true;
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(BotConfig.WaitForUser), _waitForUserCts.Token);
            return false;
        }
        catch (TaskCanceledException)
        {
            Log.Debug("WaitForUserAsync cancelled, restarting.");
        }
        finally
        {
            _isWaiting = false;
        }
        return true;
    }

    private List<ChatMessage> PrepareChatMessages()
    {
        var messages = _history.GetRecentHistory(BotConfig.History.BringToContext);
        var contentParts = new List<ChatMessageContentPart> {ChatMessageContentPart.CreateTextPart(_userMessageQueue)};
        contentParts.AddRange(_images);
        var userMessage = ChatMessage.CreateUserMessage(contentParts);
        messages.Add(userMessage);

        if (BotConfig.EnableVectorDb)
        {
            var memoryFacts = VectorDatabase
                .Query(_vectorizationUtil.VectorizeText(_userMessageQueue).Result, BotConfig.QueryResultTopK)
                .Select(fact => fact.Text);
            var persistentMemory =
                "\nYou have the following memories about the user: " + string.Join("\n", memoryFacts);
            var tempPrompt = _systemPrompt + persistentMemory;
            messages.Insert(0, ChatMessage.CreateSystemMessage(tempPrompt));
        }
        else
        {
            messages.Insert(0, ChatMessage.CreateSystemMessage(_systemPrompt));
        }
        //LogDebugMessages(messages);
        return messages;
    }

    private void LogDebugMessages(List<ChatMessage> messages)
    {
        var debugContent = string.Join("\n>>> ", messages.Select(m => m.Content.First().Text));
        Log.Debug($"Getting response, bringing with recent context: \n>>> {debugContent}");
    }

    private async Task<string> ProcessChatCompletionAsync(List<ChatMessage> messages)
    {
        var response = string.Empty;

        while (true)
        {
            var ret = await _client.CompleteChatAsync(messages, _chatOption, _cts.Token);

            switch (ret.Value.FinishReason)
            {
                case ChatFinishReason.Stop:
                    response = ret.Value.Content.First().Text.TrimEnd('\n');
                    return response;

                case ChatFinishReason.Length:
                    throw new Exception("Max token reached.");

                case ChatFinishReason.ContentFilter:
                    throw new InvalidOperationException("Content filter triggered unexpectedly.");

                case ChatFinishReason.ToolCalls:
                    messages.Add(ChatMessage.CreateAssistantMessage(ret.Value));
                    var requiresAction =
                        await _toolCallManager.ResolveToolCall(messages, ret.Value.ToolCalls, _cts.Token);
                    if (!requiresAction)
                    {
                        return response;
                    }

                    break;

                case ChatFinishReason.FunctionCall:
                    throw new InvalidOperationException("Function calls are deprecated in favor of tool calls.");

                default:
                    throw new InvalidOperationException("Unknown finish reason encountered.");
            }
        }
    }

    private async Task SendFormattedResponseAsync(string response)
    {
        foreach (var sentence in response.Split('\\'))
        {
            var msg = sentence.Trim();
            var delay = CalculateDelay(msg);
            await Task.Delay(TimeSpan.FromSeconds(delay), _cts.Token);
            await SendToUser(msg, "");
        }
    }

    private double CalculateDelay(string sentence)
    {
        return BotConfig.MinimumTime + sentence.Length * BotConfig.TimePerCharacter;
    }

    private void ResetCancellationToken()
    {
        _cts = new CancellationTokenSource();
    }

    private void ResetState()
    {
        _userMessageQueue = string.Empty;
        _images.Clear();
        IsResponding = false;
        _history.Save();
    }

    #endregion
    
    
    public void RefreshSystemPrompt()
    {
        _systemPrompt = PromptRenderer.RenderInitialSystemPrompt(BotConfig, _history);
        Log.Debug($"System prompt refreshed: \n{_systemPrompt}", "PROMPT");
    }
    
    public void Dispose()
    {
        Log.Info("Shutting down...");
        _proactive?.Dispose();
        _memorySummarizationService.Dispose();
        _cts.Cancel();
        _cts.Dispose();
        _waitForUserCts.Cancel();
        _waitForUserCts.Dispose();
        GC.SuppressFinalize(this);
    }

    private static Container DefaultContainer
    {
        get
        {
            var container = new Container();
            container.Register<ITextSerializer, JsonSerializer>(Lifestyle.Transient);
            container.Register<HistoryManager>(Lifestyle.Singleton);
            container.Register<PersonificationAgent>(Lifestyle.Singleton);
            container.Register<ProactiveMessaging>(Lifestyle.Singleton);
            container.Register<MemorySummarizationService>(Lifestyle.Singleton);
            container.Register<ToolCallManager>(Lifestyle.Singleton);
            container.Register<VectorDatabase>(Lifestyle.Singleton);
            container.Register<VectorizationUtil>(Lifestyle.Singleton);
            container.Register<Audio.AudioAgent>(Lifestyle.Singleton);
            container.Register<MemorySummary>(Lifestyle.Singleton);
            return container;
        }
    }
}