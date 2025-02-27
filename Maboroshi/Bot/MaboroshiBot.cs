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

    private readonly ChatCompletionOptions _chatOption;
    private readonly string _systemPrompt;
    private readonly ChatClient _client;
    private readonly ProactiveMessaging? _proactive;
    private readonly HistoryManager _history;
    private readonly ToolCallManager _toolCallManager;
    private readonly VectorizationUtil _vectorizationUtil;
    private CancellationTokenSource _cts = new();
    private string _userMessageQueue = string.Empty;
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
        
        _systemPrompt = PromptRenderer.RenderInitialSystemPrompt(this, BotConfig);
    }

    public async Task GetResponse(string message)
    {
        if (_isWaiting)
        {
            EnqueueUserMessage(message, prependNewLine: true);
            return;
        }

        if (IsResponding)
        {
            await _cts.CancelAsync();
        }

        EnqueueUserMessage(message);
        await WaitForUserAsync();

        IsResponding = true;
        try
        {
            var messages = PrepareChatMessages();
            var response = await ProcessChatCompletionAsync(messages);
            if (!string.IsNullOrEmpty(response))
            {
                await SendFormattedResponseAsync(response);
            }
        }
        catch (OperationCanceledException)
        {
            ResetCancellationToken();
            Console.WriteLine("[MABOROSHI-DEBUG] Response is interrupted.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MABOROSHI-ERROR] {ex.Message}");
        }
        finally
        {
            ResetState();
        }
    }

    public void AppendAssistantHistory(string message)
    {
        _history.AddMessage(ChatMessage.CreateAssistantMessage(message));
    }
    
    #region Helper Methods

    private void EnqueueUserMessage(string message, bool prependNewLine = false)
    {
        var formattedMessage = PromptRenderer.FormatMessage(message);
        _userMessageQueue += prependNewLine ? '\n' + formattedMessage : formattedMessage;
    }

    private async Task WaitForUserAsync()
    {
        _isWaiting = true;
        await Task.Delay(TimeSpan.FromSeconds(BotConfig.WaitForUser));
        _isWaiting = false;
    }

    private List<ChatMessage> PrepareChatMessages()
    {
        var messages = _history.GetRecentHistory(BotConfig.History.BringToContext);
        var userMessage = ChatMessage.CreateUserMessage(_userMessageQueue);
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

        _history.AddMessage(userMessage);
        //LogDebugMessages(messages);
        return messages;
    }

    private void LogDebugMessages(List<ChatMessage> messages)
    {
        var debugContent = string.Join("\n>>> ", messages.Select(m => m.Content.First().Text));
        Console.WriteLine($"[MABOROSHI-DEBUG] Getting response, bringing with recent context: \n>>> {debugContent}");
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
                    var assistantMessage = ChatMessage.CreateAssistantMessage(response);
                    messages.Add(assistantMessage);
                    _history.AddMessage(assistantMessage);
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
            await SendToUser(sentence, "");
            var delay = CalculateDelay(sentence);
            await Task.Delay(TimeSpan.FromSeconds(delay), _cts.Token);
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
        IsResponding = false;
        _history.Save();
    }

    #endregion
    
    public void Dispose()
    {
        _cts.Cancel();
        _proactive?.Dispose();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }

    private static Container DefaultContainer
    {
        get
        {
            var container = new Container();
            container.Register<ITextSerializer, JsonSerializer>(Lifestyle.Singleton);
            container.Register<HistoryManager>(Lifestyle.Singleton);
            container.Register<MemoryAgent>(Lifestyle.Singleton);
            container.Register<PersonificationAgent>(Lifestyle.Singleton);
            container.Register<ProactiveMessaging>(Lifestyle.Singleton);
            container.Register<ToolCallManager>(Lifestyle.Singleton);
            container.Register<VectorDatabase>(Lifestyle.Singleton);
            container.Register<VectorizationUtil>(Lifestyle.Singleton);
            container.Register<Audio.AudioAgent>(Lifestyle.Singleton);
            return container;
        }
    }
}