﻿#nullable disable
using WizBot.Common.Configs;
using WizBot.Modules.Games.Common;

namespace WizBot.Modules.Games.Services;

public sealed class GamesConfigService : ConfigServiceBase<GamesConfig>
{
    private const string FILE_PATH = "data/games.yml";
    private static readonly TypedKey<GamesConfig> _changeKey = new("config.games.updated");
    public override string Name { get; } = "games";

    public GamesConfigService(IConfigSeria serializer, IPubSub pubSub)
        : base(FILE_PATH, serializer, pubSub, _changeKey)
    {
        AddParsedProp("trivia.min_win_req",
            gs => gs.Trivia.MinimumWinReq,
            int.TryParse,
            ConfigPrinters.ToString,
            val => val > 0);
        AddParsedProp("trivia.currency_reward",
            gs => gs.Trivia.CurrencyReward,
            long.TryParse,
            ConfigPrinters.ToString,
            val => val >= 0);
        AddParsedProp("hangman.currency_reward",
            gs => gs.Hangman.CurrencyReward,
            long.TryParse,
            ConfigPrinters.ToString,
            val => val >= 0);
        AddParsedProp("chatbot",
            gs => gs.ChatBot,
            ConfigParsers.InsensitiveEnum,
            ConfigPrinters.ToString);
        AddParsedProp("gpt.modelName",
            gs => gs.ChatGpt.ModelName,
            ConfigParsers.InsensitiveEnum,
            ConfigPrinters.ToString);
        AddParsedProp("gpt.personality",
            gs => gs.ChatGpt.PersonalityPrompt,
            ConfigParsers.String,
            ConfigPrinters.ToString);
        AddParsedProp("gpt.chathistory",
            gs => gs.ChatGpt.ChatHistory,
            int.TryParse,
            ConfigPrinters.ToString,
            val => val > 0);
        AddParsedProp("gpt.max_tokens",
            gs => gs.ChatGpt.MaxTokens,
            int.TryParse,
            ConfigPrinters.ToString,
            val => val > 0);
        AddParsedProp("gpt.min_tokens",
            gs => gs.ChatGpt.MinTokens,
            int.TryParse,
            ConfigPrinters.ToString,
            val => val > 0);

        Migrate();
    }

    private void Migrate()
    {
        if (data.Version < 1)
        {
            ModifyConfig(c =>
            {
                c.Version = 1;
                c.Hangman = new()
                {
                    CurrencyReward = 0
                };
            });
        }
        
        if (data.Version < 2)
        {
            ModifyConfig(c =>
            {
                c.Version = 2;
                c.ChatBot = ChatBotImplementation.Cleverbot;
            });
        }

        if (data.Version < 3)
        {
            ModifyConfig(c =>
            {
                c.Version = 3;
                c.ChatGpt.ModelName = ChatGptModel.Gpt35Turbo;
            });
        }
    }
}