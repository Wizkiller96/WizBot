#nullable disable
using Cloneable;
using NadekoBot.Common.Yml;

namespace NadekoBot.Modules.Games.Common;

[Cloneable]
public sealed partial class GamesConfig : ICloneable<GamesConfig>
{
    [Comment("DO NOT CHANGE")]
    public int Version { get; set; } = 3;

    [Comment("Hangman related settings (.hangman command)")]
    public HangmanConfig Hangman { get; set; } = new()
    {
        CurrencyReward = 0
    };

    [Comment("Trivia related settings (.t command)")]
    public TriviaConfig Trivia { get; set; } = new()
    {
        CurrencyReward = 0,
        MinimumWinReq = 1
    };

    [Comment("List of responses for the .8ball command. A random one will be selected every time")]
    public List<string> EightBallResponses { get; set; } = new()
    {
        "Most definitely yes.",
        "For sure.",
        "Totally!",
        "Of course!",
        "As I see it, yes.",
        "My sources say yes.",
        "Yes.",
        "Most likely.",
        "Perhaps...",
        "Maybe...",
        "Hm, not sure.",
        "It is uncertain.",
        "Ask me again later.",
        "Don't count on it.",
        "Probably not.",
        "Very doubtful.",
        "Most likely no.",
        "Nope.",
        "No.",
        "My sources say no.",
        "Don't even think about it.",
        "Definitely no.",
        "NO - It may cause disease contraction!"
    };

    [Comment("List of animals which will be used for the animal race game (.race)")]
    public List<RaceAnimal> RaceAnimals { get; set; } = new()
    {
        new()
        {
            Icon = "🐼",
            Name = "Panda"
        },
        new()
        {
            Icon = "🐻",
            Name = "Bear"
        },
        new()
        {
            Icon = "🐧",
            Name = "Pengu"
        },
        new()
        {
            Icon = "🐨",
            Name = "Koala"
        },
        new()
        {
            Icon = "🐬",
            Name = "Dolphin"
        },
        new()
        {
            Icon = "🐞",
            Name = "Ladybird"
        },
        new()
        {
            Icon = "🦀",
            Name = "Crab"
        },
        new()
        {
            Icon = "🦄",
            Name = "Unicorn"
        }
    };

    [Comment(@"Which chatbot API should bot use.
'cleverbot' - bot will use Cleverbot API. 
'gpt3' - bot will use GPT-3 API")]
    public ChatBotImplementation ChatBot { get; set; } = ChatBotImplementation.Gpt3;

    public ChatGptConfig ChatGpt { get; set; } = new();
}

[Cloneable]
public sealed partial class ChatGptConfig
{
    [Comment(@"Which GPT-3 Model should bot use.
    gpt35turbo - cheapest
    gpt4 - 30x more expensive, higher quality
    gp432k - same model as above, but with a 32k token limit")]
    public ChatGptModel ModelName { get; set; } = ChatGptModel.Gpt35Turbo;

    [Comment(@"How should the chat bot behave, what's its personality? (Usage of this counts towards the max tokens)")]
    public string PersonalityPrompt { get; set; } = "You are a chat bot willing to have a conversation with anyone about anything.";

    [Comment(@"The maximum number of messages in a conversation that can be remembered. (This will increase the number of tokens used)")]
    public int ChatHistory { get; set; } = 5;

    [Comment(@"The maximum number of tokens to use per GPT-3 API call")]
    public int MaxTokens { get; set; } = 100;

    [Comment(@"The minimum number of tokens to use per GPT-3 API call, such that chat history is removed to make room.")]
    public int MinTokens { get; set; } = 30;
}

[Cloneable]
public sealed partial class HangmanConfig
{
    [Comment("The amount of currency awarded to the winner of a hangman game")]
    public long CurrencyReward { get; set; }
}

[Cloneable]
public sealed partial class TriviaConfig
{
    [Comment("The amount of currency awarded to the winner of the trivia game.")]
    public long CurrencyReward { get; set; }

    [Comment(@"Users won't be able to start trivia games which have 
a smaller win requirement than the one specified by this setting.")]
    public int MinimumWinReq { get; set; } = 1;
}

[Cloneable]
public sealed partial class RaceAnimal
{
    public string Icon { get; set; }
    public string Name { get; set; }
}

public enum ChatBotImplementation
{
    Cleverbot,
    Gpt3
}

public enum ChatGptModel
{
    Gpt35Turbo,
    Gpt4,
    Gpt432k
}