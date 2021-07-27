using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Modules.Games.Common.Trivia;
using NadekoBot.Modules.Games.Services;
using Serilog;

namespace NadekoBot.Modules.Games.Common.Trivia
{
    public class TriviaGame
    {
        private readonly SemaphoreSlim _guessLock = new SemaphoreSlim(1, 1);
        private readonly IDataCache _cache;
        private readonly IBotStrings _strings;
        private readonly DiscordSocketClient _client;
        private readonly GamesConfig _config;
        private readonly ICurrencyService _cs;
        private readonly TriviaOptions _options;

        public IGuild Guild { get; }
        public ITextChannel Channel { get; }

        private CancellationTokenSource _triviaCancelSource;

        public TriviaQuestion CurrentQuestion { get; private set; }
        public HashSet<TriviaQuestion> OldQuestions { get; } = new HashSet<TriviaQuestion>();

        public ConcurrentDictionary<IGuildUser, int> Users { get; } = new ConcurrentDictionary<IGuildUser, int>();

        public bool GameActive { get; private set; }
        public bool ShouldStopGame { get; private set; }

        private readonly TriviaQuestionPool _questionPool;
        private int _timeoutCount = 0;
        private readonly string _quitCommand;
        private readonly IEmbedBuilderService _eb;

        public TriviaGame(IBotStrings strings, DiscordSocketClient client, GamesConfig config,
            IDataCache cache, ICurrencyService cs, IGuild guild, ITextChannel channel,
            TriviaOptions options, string quitCommand, IEmbedBuilderService eb)
        {
            _cache = cache;
            _questionPool = new TriviaQuestionPool(_cache);
            _strings = strings;
            _client = client;
            _config = config;
            _cs = cs;
            _options = options;
            _quitCommand = quitCommand;
            _eb = eb;

            Guild = guild;
            Channel = channel;
        }

        private string GetText(in LocStr key)
             => _strings.GetText(key, Channel.GuildId);

        public async Task StartGame()
        {
            var showHowToQuit = false;
            while (!ShouldStopGame)
            {
                // reset the cancellation source    
                _triviaCancelSource = new CancellationTokenSource();
                showHowToQuit = !showHowToQuit;

                // load question
                CurrentQuestion = _questionPool.GetRandomQuestion(OldQuestions, _options.IsPokemon);
                if (string.IsNullOrWhiteSpace(CurrentQuestion?.Answer) || string.IsNullOrWhiteSpace(CurrentQuestion.Question))
                {
                    await Channel.SendErrorAsync(_eb, GetText(strs.trivia_game), GetText(strs.failed_loading_question)).ConfigureAwait(false);
                    return;
                }
                OldQuestions.Add(CurrentQuestion); //add it to exclusion list so it doesn't show up again

                IEmbedBuilder questionEmbed;
                IUserMessage questionMessage;
                try
                {
                    questionEmbed = _eb.Create().WithOkColor()
                        .WithTitle(GetText(strs.trivia_game))
                        .AddField(GetText(strs.category), CurrentQuestion.Category)
                        .AddField(GetText(strs.question), CurrentQuestion.Question);

                    if (showHowToQuit)
                        questionEmbed.WithFooter(GetText(strs.trivia_quit(_quitCommand)));
                    
                    if (Uri.IsWellFormedUriString(CurrentQuestion.ImageUrl, UriKind.Absolute))
                        questionEmbed.WithImageUrl(CurrentQuestion.ImageUrl);

                    questionMessage = await Channel.EmbedAsync(questionEmbed).ConfigureAwait(false);
                }
                catch (HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.NotFound ||
                                               ex.HttpCode == System.Net.HttpStatusCode.Forbidden ||
                                               ex.HttpCode == System.Net.HttpStatusCode.BadRequest)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error sending trivia embed");
                    await Task.Delay(2000).ConfigureAwait(false);
                    continue;
                }

                //receive messages
                try
                {
                    _client.MessageReceived += PotentialGuess;

                    //allow people to guess
                    GameActive = true;
                    try
                    {
                        //hint
                        await Task.Delay(_options.QuestionTimer * 1000 / 2, _triviaCancelSource.Token).ConfigureAwait(false);
                        if (!_options.NoHint)
                            try
                            {
                                await questionMessage.ModifyAsync(m => m.Embed = questionEmbed.WithFooter(CurrentQuestion.GetHint()).Build())
                                    .ConfigureAwait(false);
                            }
                            catch (HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.NotFound || ex.HttpCode == System.Net.HttpStatusCode.Forbidden)
                            {
                                break;
                            }
                            catch (Exception ex) { Log.Warning(ex, "Error editing triva message"); }

                        //timeout
                        await Task.Delay(_options.QuestionTimer * 1000 / 2, _triviaCancelSource.Token).ConfigureAwait(false);

                    }
                    catch (TaskCanceledException) { _timeoutCount = 0; } //means someone guessed the answer
                }
                finally
                {
                    GameActive = false;
                    _client.MessageReceived -= PotentialGuess;
                }
                if (!_triviaCancelSource.IsCancellationRequested)
                {
                    try
                    {
                        var embed = _eb.Create().WithErrorColor()
                            .WithTitle(GetText(strs.trivia_game))
                            .WithDescription(GetText(strs.trivia_times_up(Format.Bold(CurrentQuestion.Answer))));
                        if (Uri.IsWellFormedUriString(CurrentQuestion.AnswerImageUrl, UriKind.Absolute))
                            embed.WithImageUrl(CurrentQuestion.AnswerImageUrl);

                        await Channel.EmbedAsync(embed).ConfigureAwait(false);

                        if (_options.Timeout != 0 && ++_timeoutCount >= _options.Timeout)
                            await StopGame().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Error sending trivia time's up message");
                    }
                }
                await Task.Delay(5000).ConfigureAwait(false);
            }
        }

        public async Task EnsureStopped()
        {
            ShouldStopGame = true;

            await Channel.EmbedAsync(_eb.Create().WithOkColor()
                    .WithAuthor("Trivia Game Ended")
                    .WithTitle("Final Results")
                    .WithDescription(GetLeaderboard())).ConfigureAwait(false);
        }

        public async Task StopGame()
        {
            var old = ShouldStopGame;
            ShouldStopGame = true;
            if (!old)
            {
                try
                {
                    await Channel.SendConfirmAsync(_eb,
                        GetText(strs.trivia_game),
                        GetText(strs.trivia_stopping));

                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error sending trivia stopping message");
                }
            }
        }

        private Task PotentialGuess(SocketMessage imsg)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (imsg.Author.IsBot)
                        return;

                    var umsg = imsg as SocketUserMessage;

                    var textChannel = umsg?.Channel as ITextChannel;
                    if (textChannel is null || textChannel.Guild != Guild)
                        return;

                    var guildUser = (IGuildUser)umsg.Author;

                    var guess = false;
                    await _guessLock.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        if (GameActive && CurrentQuestion.IsAnswerCorrect(umsg.Content) && !_triviaCancelSource.IsCancellationRequested)
                        {
                            Users.AddOrUpdate(guildUser, 1, (gu, old) => ++old);
                            guess = true;
                        }
                    }
                    finally { _guessLock.Release(); }
                    if (!guess) return;
                    _triviaCancelSource.Cancel();


                    if (_options.WinRequirement != 0 && Users[guildUser] == _options.WinRequirement)
                    {
                        ShouldStopGame = true;
                        try
                        {
                            var embedS = _eb.Create().WithOkColor()
                                .WithTitle(GetText(strs.trivia_game))
                                .WithDescription(GetText(strs.trivia_win(
                                    guildUser.Mention,
                                    Format.Bold(CurrentQuestion.Answer))));
                            if (Uri.IsWellFormedUriString(CurrentQuestion.AnswerImageUrl, UriKind.Absolute))
                                embedS.WithImageUrl(CurrentQuestion.AnswerImageUrl);
                            await Channel.EmbedAsync(embedS).ConfigureAwait(false);
                        }
                        catch
                        {
                            // ignored
                        }
                        var reward = _config.Trivia.CurrencyReward;
                        if (reward > 0)
                            await _cs.AddAsync(guildUser, "Won trivia", reward, true).ConfigureAwait(false);
                        return;
                    }
                    var embed = _eb.Create().WithOkColor()
                        .WithTitle(GetText(strs.trivia_game))
                        .WithDescription(GetText(strs.trivia_guess(guildUser.Mention, Format.Bold(CurrentQuestion.Answer))));
                    if (Uri.IsWellFormedUriString(CurrentQuestion.AnswerImageUrl, UriKind.Absolute))
                        embed.WithImageUrl(CurrentQuestion.AnswerImageUrl);
                    await Channel.EmbedAsync(embed).ConfigureAwait(false);
                }
                catch (Exception ex) { Log.Warning(ex.ToString()); }
            });
            return Task.CompletedTask;
        }

        public string GetLeaderboard()
        {
            if (Users.Count == 0)
                return GetText(strs.no_results);

            var sb = new StringBuilder();

            foreach (var kvp in Users.OrderByDescending(kvp => kvp.Value))
            {
                sb.AppendLine(GetText(strs.trivia_points(Format.Bold(kvp.Key.ToString()), kvp.Value)).SnPl(kvp.Value));
            }

            return sb.ToString();
        }
    }
}