using System.Net;
using System.Text;
using NadekoBot.Modules.Games.Common.Trivia;
using NadekoBot.Modules.Games.Services;

namespace NadekoBot.Modules.Games;

public partial class Games
{
    [Group]
    public partial class TriviaCommands : NadekoModule<TriviaGamesService>
    {
        private readonly ILocalDataCache _cache;
        private readonly ICurrencyService _cs;
        private readonly GamesConfigService _gamesConfig;
        private readonly DiscordSocketClient _client;

        public TriviaCommands(
            DiscordSocketClient client,
            ILocalDataCache cache,
            ICurrencyService cs,
            GamesConfigService gamesConfig)
        {
            _cache = cache;
            _cs = cs;
            _gamesConfig = gamesConfig;
            _client = client;
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        [NadekoOptions(typeof(TriviaOptions))]
        public async partial Task Trivia(params string[] args)
        {
            var (opts, _) = OptionsParser.ParseFrom(new TriviaOptions(), args);

            var config = _gamesConfig.Data;
            if (config.Trivia.MinimumWinReq > 0 && config.Trivia.MinimumWinReq > opts.WinRequirement)
                return;

            var trivia = new TriviaGame(opts, _cache);
            if (_service.RunningTrivias.TryAdd(ctx.Guild.Id, trivia))
            {
                RegisterEvents(trivia);
                await trivia.RunAsync();
                return;
            }

            if (_service.RunningTrivias.TryGetValue(ctx.Guild.Id, out var tg))
            {
                await SendErrorAsync(GetText(strs.trivia_already_running));
                await tg.TriggerQuestionAsync();
            }
        }
        
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async partial Task Tl()
        {
            if (_service.RunningTrivias.TryGetValue(ctx.Guild.Id, out var trivia))
            {
                await trivia.TriggerStatsAsync();
                return;
            }

            await ReplyErrorLocalizedAsync(strs.trivia_none);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async partial Task Tq()
        {
            var channel = (ITextChannel)ctx.Channel;

            if (_service.RunningTrivias.TryGetValue(channel.Guild.Id, out var trivia))
            {
                if (trivia.Stop())
                {
                    try
                    {
                        await ctx.Channel.SendConfirmAsync(_eb,
                            GetText(strs.trivia_game),
                            GetText(strs.trivia_stopping));
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Error sending trivia stopping message");
                    }
                }

                return;
            }

            await ReplyErrorLocalizedAsync(strs.trivia_none);
        }
        

        private void RegisterEvents(TriviaGame trivia)
        {
            IEmbedBuilder? questionEmbed = null;
            IUserMessage? questionMessage = null;
            var showHowToQuit = false;

            trivia.OnQuestion += async (_, question) =>
            {
                try
                {
                    questionEmbed = _eb.Create()
                        .WithOkColor()
                        .WithTitle(GetText(strs.trivia_game))
                        .AddField(GetText(strs.category), question.Category)
                        .AddField(GetText(strs.question), question.Question);
                    
                    showHowToQuit = !showHowToQuit;
                    if (showHowToQuit)
                        questionEmbed.WithFooter(GetText(strs.trivia_quit($"{prefix}tq")));

                    if (Uri.IsWellFormedUriString(question.ImageUrl, UriKind.Absolute))
                        questionEmbed.WithImageUrl(question.ImageUrl);

                    questionMessage = await ctx.Channel.EmbedAsync(questionEmbed);
                }
                catch (HttpException ex) when (ex.HttpCode is HttpStatusCode.NotFound
                                                   or HttpStatusCode.Forbidden
                                                   or HttpStatusCode.BadRequest)
                {
                    Log.Warning("Unable to send trivia questions. Stopping immediately");
                    trivia.Stop();
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error sending trivia embed");
                    await Task.Delay(2000);
                }
            };

            trivia.OnHint += async (_, question) =>
            {
                try
                {
                    if (questionMessage is null)
                    {
                        trivia.Stop();
                        return;
                    }

                    if (questionEmbed is not null)
                        await questionMessage.ModifyAsync(m
                            => m.Embed = questionEmbed.WithFooter(question.GetHint()).Build());
                }
                catch (HttpException ex) when (ex.HttpCode is HttpStatusCode.NotFound
                                                   or HttpStatusCode.Forbidden)
                {
                    Log.Warning("Unable to edit message to show hint. Stopping trivia");
                    trivia.Stop();
                }
                catch (Exception ex) { Log.Warning(ex, "Error editing triva message"); }

            };

            trivia.OnGuess += async (_, user, question, isWin) =>
            {
                try
                {
                    var embed = _eb.Create()
                        .WithOkColor()
                        .WithTitle(GetText(strs.trivia_game))
                        .WithDescription(GetText(strs.trivia_win(user.Name,
                            Format.Bold(question.Answer))));

                    if (Uri.IsWellFormedUriString(question.AnswerImageUrl, UriKind.Absolute))
                        embed.WithImageUrl(question.AnswerImageUrl);


                    if (isWin)
                    {
                        await ctx.Channel.EmbedAsync(embed);

                        var reward = _gamesConfig.Data.Trivia.CurrencyReward;
                        if (reward > 0)
                            await _cs.AddAsync(user.Id, reward, new("trivia", "win"));
                        
                        return;
                    }

                    embed.WithDescription(GetText(strs.trivia_guess(user.Name,
                        Format.Bold(question.Answer))));
                    
                    await ctx.Channel.EmbedAsync(embed);

                }
                catch
                {
                    // ignored
                }
            };

            trivia.OnEnded += async (game) =>
            {
                try
                {
                    await ctx.Channel.EmbedAsync(_eb.Create(ctx)
                        .WithOkColor()
                        .WithAuthor(GetText(strs.trivia_ended))
                        .WithTitle(GetText(strs.leaderboard))
                        .WithDescription(GetLeaderboardString(game)));
                }
                catch
                {
                    // ignored
                }
                finally
                {
                    _service.RunningTrivias.TryRemove(ctx.Guild.Id, out _);
                }

            };

            trivia.OnStats += async (game) =>
            {
                try
                {
                    await SendConfirmAsync(GetText(strs.leaderboard), GetLeaderboardString(game));
                }
                catch
                {
                    // ignored
                }
            };

            trivia.OnTimeout += async (_, question) =>
            {
                try
                {
                    var embed = _eb.Create()
                        .WithErrorColor()
                        .WithTitle(GetText(strs.trivia_game))
                        .WithDescription(GetText(strs.trivia_times_up(Format.Bold(question.Answer))));
                    
                    if (Uri.IsWellFormedUriString(question.AnswerImageUrl, UriKind.Absolute))
                        embed.WithImageUrl(question.AnswerImageUrl);

                    await ctx.Channel.EmbedAsync(embed);

                }
                catch
                {
                    // ignored
                }
            };
        }

        private string GetLeaderboardString(TriviaGame tg)
        {
            var sb = new StringBuilder();

            foreach (var (id, pts) in tg.GetLeaderboard())
                sb.AppendLine(GetText(strs.trivia_points(Format.Bold($"<@{id}>"), pts)));

            return sb.ToString();

        }
    }
}