#nullable disable
using Cloneable;
using WizBot.Common.Yml;

namespace WizBot.Modules.Games.Common;

[Cloneable]
public sealed partial class GamesConfig : ICloneable<GamesConfig>
{
    [Comment("DO NOT CHANGE")]
    public int Version { get; set; } = 2;

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
'ada001' - cheapest and fastest 
'babbage001' - 2nd option
'curie001' - 3rd option
'davinci003' - Most expensive, slowest")]
    public Gpt3Model Model { get; set; } = Gpt3Model.Ada001;

    [Comment(@"The maximum number of tokens to use per GPT-3 API call")]
    public int MaxTokens { get; set; } = 100;
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

public enum Gpt3Model
{
    Ada001,
    Babbage001,
    Curie001,
    Davinci003
}