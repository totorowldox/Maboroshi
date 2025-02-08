# Maboroshi

Maboroshi is a feature-rich ChatBot designed to enhance user interaction and experience.

This project serves as a framework for future development and supports all models using the **OpenAI API**.

## Features

- Proactive messaging
- Persistent memory
- Context awareness
- Customizable system prompts
- Personified responses
- Ability to wait for multiple messages
- Simple implementation

## TODO

- [ ] Test the VectorDB solution for persistent memory
- [ ] Implement customizable agents
- [ ] Develop RAG Q&A functionality
- [ ] Integrate additional model APIs (e.g., Ollama for local models)

## Implementation

To integrate Maboroshi to your application, use the following C# code:

```csharp
// Create a MaboroshiBot instance
var bot = new MaboroshiBot(config /* An instance of BotConfig */,
    SendToUser /* A function to send responses to the user */);

// Get a response from the bot
_ = bot.GetResponse("Hello!");
```

## Configuration

Assuming you are using a YAML configuration file, `config.yaml` should look like this:

```yaml
# Chatting Model Settings
apiEndpoint: "https://api.openai.com/v1/" 
apiKey: "sk-XXX"
apiModel: "gpt-4o-mini"
temperature: 1.3
maxOutputToken: 2048

# Memory Settings
enableVectorDb: true
vectorDbFile: "memory.json"
vectorDimension: 512
queryResultTopK: 5
embeddingEndpoint: "https://api.openai.com/v1/"
embeddingKey: "sk-XXX"
embeddingModel: "text-embedding-3-small"

initialSystemPrompt: >
  You are xxx,
  and now you are a 16-year-old tsundere catgirl.
  You have long white hair, big watery blue eyes, and a small chest.
  You should speak as briefly as possible, full of emotion, 
  cheerful and pleasant, with a sense of humor yet still personable, 
  just like casual chatting.
  Always remind the user to achieve their goals!
  ...

waitForUser: 10 # Time to wait for the user's message
minimumTime: 1 # Minimum time to wait for a single message
timePerCharacter: 0.05 # Additional waiting time per character

userProfile:
  name: "Your Name"
  language: "English"
  facts: 
    - "xx years old."
    - "..."
  goals: 
    - "Learn Japanese."
    - "..."

# Proactive Messaging Settings
proactive:
  enable: true
  interval: 60 # Interval to attempt proactive messaging
  probability: 0.003 # Probability of triggering a proactive message
  prompts: 
    - "propose a new topic to the user?"
    - "urge users to achieve their goals?"
    - "..."
  dndHours: [23, 00, 01, 02, 03, 04, 05, 06]

# Context Persistence Settings
history:
  savePath: "history.json"
  bringToContext: 20
```

## Acknowledgements

- [YamlDotNet](https://github.com/aaubry/YamlDotNet)
- [SimpleInjector](https://github.com/simpleinjector/SimpleInjector)
- [openai-dotnet](https://github.com/openai/openai-dotnet)

Special thanks to [DeepSeek's reasoning model](https://github.com/deepseek-ai/DeepSeek-R1) for inspiring the creation of this project.