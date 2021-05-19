﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using WizBot.Extensions;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WizBot.Common.Attributes;
using WizBot.Modules.Games.Common;
using WizBot.Modules.Games.Services;
using WizBot.Core.Common;

namespace WizBot.Modules.Games
{
    public partial class Games
    {
        [Group]
        public class SpeedTypingCommands : WizBotSubmodule<GamesService>
        {
            private readonly GamesService _games;
            private readonly DiscordSocketClient _client;

            public SpeedTypingCommands(DiscordSocketClient client, GamesService games)
            {
                _games = games;
                _client = client;
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [WizBotOptionsAttribute(typeof(TypingGame.Options))]
            public async Task TypeStart(params string[] args)
            {
                var (options, _) = OptionsParser.ParseFrom(new TypingGame.Options(), args);
                var channel = (ITextChannel)ctx.Channel;

                var game = _service.RunningContests.GetOrAdd(channel.Guild.Id, id => new TypingGame(_games, _client, channel, Prefix, options));

                if (game.IsActive)
                {
                    await channel.SendErrorAsync(
                            $"Contest already running in " +
                            $"{game.Channel.Mention} channel.")
                                .ConfigureAwait(false);
                }
                else
                {
                    await game.Start().ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task TypeStop()
            {
                var channel = (ITextChannel)ctx.Channel;
                if (_service.RunningContests.TryRemove(channel.Guild.Id, out TypingGame game))
                {
                    await game.Stop().ConfigureAwait(false);
                    return;
                }
                await channel.SendErrorAsync("No contest to stop on this channel.").ConfigureAwait(false);
            }


            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [AdminOnly]
            public async Task Typeadd([Leftover] string text)
            {
                var channel = (ITextChannel)ctx.Channel;
                if (string.IsNullOrWhiteSpace(text))
                    return;

                _games.AddTypingArticle(ctx.User, text);                

                await channel.SendConfirmAsync("Added new article for typing game.").ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Typelist(int page = 1)
            {
                var channel = (ITextChannel)ctx.Channel;

                if (page < 1)
                    return;

                var articles = _games.TypingArticles.Skip((page - 1) * 15).Take(15).ToArray();

                if (!articles.Any())
                {
                    await channel.SendErrorAsync($"{ctx.User.Mention} `No articles found on that page.`").ConfigureAwait(false);
                    return;
                }
                var i = (page - 1) * 15;
                await channel.SendConfirmAsync("List of articles for Type Race", string.Join("\n", articles.Select(a => $"`#{++i}` - {a.Text.TrimTo(50)}")))
                             .ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [AdminOnly]
            public async Task Typedel(int index)
            {
                var removed = _service.RemoveTypingArticle(--index);
                
                if (removed is null)
                {
                    return;
                }

                var embed = new EmbedBuilder()
                    .WithTitle($"Removed typing article #{index + 1}")
                    .WithDescription(removed.Text.TrimTo(50))
                    .WithOkColor();

                await Context.Channel.EmbedAsync(embed);
            }
        }
    }
}