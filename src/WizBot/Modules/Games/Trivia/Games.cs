﻿using System.Net;
using System.Text;
using WizBot.Modules.Games.Common.Trivia;
using WizBot.Modules.Games.Services;

namespace WizBot.Modules.Games;

public partial class Games
{
    [Group]
    public partial class TriviaCommands : WizBotModule<TriviaGamesService>
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
        [WizBotOptions(typeof(TriviaOptions))]
        public async Task Trivia(params string[] args)
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
        public async Task Tl()
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
        public async Task Tq()
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

        private string GetLeaderboardString(TriviaGame tg)
        {
            var sb = new StringBuilder();

            foreach (var (id, pts) in tg.GetLeaderboard())
                sb.AppendLine(GetText(strs.trivia_points(Format.Bold($"<@{id}>"), pts)));

            return sb.ToString();

        }

        private IEmbedBuilder? questionEmbed = null;
        private IUserMessage? questionMessage = null;
        private bool showHowToQuit = false;
        
        private void RegisterEvents(TriviaGame trivia)
        {
            trivia.OnQuestion += OnTriviaOnOnQuestion;
            trivia.OnHint += OnTriviaOnOnHint;
            trivia.OnGuess += OnTriviaOnOnGuess;
            trivia.OnEnded += OnTriviaOnOnEnded;
            trivia.OnStats += OnTriviaOnOnStats;
            trivia.OnTimeout += OnTriviaOnOnTimeout;
        }
        
        private void UnregisterEvents(TriviaGame trivia)
        {
            trivia.OnQuestion -= OnTriviaOnOnQuestion;
            trivia.OnHint -= OnTriviaOnOnHint;
            trivia.OnGuess -= OnTriviaOnOnGuess;
            trivia.OnEnded -= OnTriviaOnOnEnded;
            trivia.OnStats -= OnTriviaOnOnStats;
            trivia.OnTimeout -= OnTriviaOnOnTimeout;
        }

        private async Task OnTriviaOnOnHint(TriviaGame game, TriviaQuestion question)
        {
            try
            {
                if (questionMessage is null)
                {
                    game.Stop();
                    return;
                }

                if (questionEmbed is not null)
                    await questionMessage.ModifyAsync(m => m.Embed = questionEmbed.WithFooter(question.GetHint()).Build());
            }
            catch (HttpException ex) when (ex.HttpCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
            {
                Log.Warning("Unable to edit message to show hint. Stopping trivia");
                game.Stop();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error editing triva message");
            }
        }

        private async Task OnTriviaOnOnQuestion(TriviaGame game, TriviaQuestion question)
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
            catch (HttpException ex) when (ex.HttpCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden or HttpStatusCode.BadRequest)
            {
                Log.Warning("Unable to send trivia questions. Stopping immediately");
                game.Stop();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error sending trivia embed");
                await Task.Delay(2000);
            }
        }

        private async Task OnTriviaOnOnTimeout(TriviaGame _, TriviaQuestion question)
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
        }

        private async Task OnTriviaOnOnStats(TriviaGame game)
        {
            try
            {
                await SendConfirmAsync(GetText(strs.leaderboard), GetLeaderboardString(game));
            }
            catch
            {
                // ignored
            }
        }

        private async Task OnTriviaOnOnEnded(TriviaGame game)
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

            UnregisterEvents(game);
            await Task.Delay(1000);
        }

        private async Task OnTriviaOnOnGuess(TriviaGame _, TriviaUser user, TriviaQuestion question, bool isWin)
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
        }
    }
}