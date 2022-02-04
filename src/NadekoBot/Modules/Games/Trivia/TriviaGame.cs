#nullable disable
using System.Net;
using System.Text;

namespace NadekoBot.Modules.Games.Common.Trivia;

public class TriviaGame
{
    public IGuild Guild { get; }
    public ITextChannel Channel { get; }

    public TriviaQuestion CurrentQuestion { get; private set; }
    public HashSet<TriviaQuestion> OldQuestions { get; } = new();

    public ConcurrentDictionary<IGuildUser, int> Users { get; } = new();

    public bool GameActive { get; private set; }
    public bool ShouldStopGame { get; private set; }
    private readonly SemaphoreSlim _guessLock = new(1, 1);
    private readonly IDataCache _cache;
    private readonly IBotStrings _strings;
    private readonly DiscordSocketClient _client;
    private readonly GamesConfig _config;
    private readonly ICurrencyService _cs;
    private readonly TriviaOptions _options;

    private CancellationTokenSource triviaCancelSource;

    private readonly TriviaQuestionPool _questionPool;
    private int timeoutCount;
    private readonly string _quitCommand;
    private readonly IEmbedBuilderService _eb;

    public TriviaGame(
        IBotStrings strings,
        DiscordSocketClient client,
        GamesConfig config,
        IDataCache cache,
        ICurrencyService cs,
        IGuild guild,
        ITextChannel channel,
        TriviaOptions options,
        string quitCommand,
        IEmbedBuilderService eb)
    {
        _cache = cache;
        _questionPool = new(_cache);
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
            triviaCancelSource = new();
            showHowToQuit = !showHowToQuit;

            // load question
            CurrentQuestion = _questionPool.GetRandomQuestion(OldQuestions, _options.IsPokemon);
            if (string.IsNullOrWhiteSpace(CurrentQuestion?.Answer)
                || string.IsNullOrWhiteSpace(CurrentQuestion.Question))
            {
                await Channel.SendErrorAsync(_eb, GetText(strs.trivia_game), GetText(strs.failed_loading_question));
                return;
            }

            OldQuestions.Add(CurrentQuestion); //add it to exclusion list so it doesn't show up again

            IEmbedBuilder questionEmbed;
            IUserMessage questionMessage;
            try
            {
                questionEmbed = _eb.Create()
                                   .WithOkColor()
                                   .WithTitle(GetText(strs.trivia_game))
                                   .AddField(GetText(strs.category), CurrentQuestion.Category)
                                   .AddField(GetText(strs.question), CurrentQuestion.Question);

                if (showHowToQuit)
                    questionEmbed.WithFooter(GetText(strs.trivia_quit(_quitCommand)));

                if (Uri.IsWellFormedUriString(CurrentQuestion.ImageUrl, UriKind.Absolute))
                    questionEmbed.WithImageUrl(CurrentQuestion.ImageUrl);

                questionMessage = await Channel.EmbedAsync(questionEmbed);
            }
            catch (HttpException ex) when (ex.HttpCode is HttpStatusCode.NotFound
                                               or HttpStatusCode.Forbidden
                                               or HttpStatusCode.BadRequest)
            {
                return;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error sending trivia embed");
                await Task.Delay(2000);
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
                    await Task.Delay(_options.QuestionTimer * 1000 / 2, triviaCancelSource.Token);
                    if (!_options.NoHint)
                    {
                        try
                        {
                            await questionMessage.ModifyAsync(m
                                => m.Embed = questionEmbed.WithFooter(CurrentQuestion.GetHint()).Build());
                        }
                        catch (HttpException ex) when (ex.HttpCode is HttpStatusCode.NotFound
                                                           or HttpStatusCode.Forbidden)
                        {
                            break;
                        }
                        catch (Exception ex) { Log.Warning(ex, "Error editing triva message"); }
                    }

                    //timeout
                    await Task.Delay(_options.QuestionTimer * 1000 / 2, triviaCancelSource.Token);
                }
                catch (TaskCanceledException) { timeoutCount = 0; } //means someone guessed the answer
            }
            finally
            {
                GameActive = false;
                _client.MessageReceived -= PotentialGuess;
            }

            if (!triviaCancelSource.IsCancellationRequested)
            {
                try
                {
                    var embed = _eb.Create()
                                   .WithErrorColor()
                                   .WithTitle(GetText(strs.trivia_game))
                                   .WithDescription(GetText(strs.trivia_times_up(Format.Bold(CurrentQuestion.Answer))));
                    if (Uri.IsWellFormedUriString(CurrentQuestion.AnswerImageUrl, UriKind.Absolute))
                        embed.WithImageUrl(CurrentQuestion.AnswerImageUrl);

                    await Channel.EmbedAsync(embed);

                    if (_options.Timeout != 0 && ++timeoutCount >= _options.Timeout)
                        await StopGame();
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error sending trivia time's up message");
                }
            }

            await Task.Delay(5000);
        }
    }

    public async Task EnsureStopped()
    {
        ShouldStopGame = true;

        await Channel.EmbedAsync(_eb.Create()
                                    .WithOkColor()
                                    .WithAuthor("Trivia Game Ended")
                                    .WithTitle("Final Results")
                                    .WithDescription(GetLeaderboard()));
    }

    public async Task StopGame()
    {
        var old = ShouldStopGame;
        ShouldStopGame = true;
        if (!old)
        {
            try
            {
                await Channel.SendConfirmAsync(_eb, GetText(strs.trivia_game), GetText(strs.trivia_stopping));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error sending trivia stopping message");
            }
        }
    }

    private Task PotentialGuess(SocketMessage imsg)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (imsg.Author.IsBot)
                    return;

                var umsg = imsg as SocketUserMessage;

                if (umsg?.Channel is not ITextChannel textChannel || textChannel.Guild != Guild)
                    return;

                var guildUser = (IGuildUser)umsg.Author;

                var guess = false;
                await _guessLock.WaitAsync();
                try
                {
                    if (GameActive
                        && CurrentQuestion.IsAnswerCorrect(umsg.Content)
                        && !triviaCancelSource.IsCancellationRequested)
                    {
                        Users.AddOrUpdate(guildUser, 1, (_, old) => ++old);
                        guess = true;
                    }
                }
                finally { _guessLock.Release(); }

                if (!guess)
                    return;
                triviaCancelSource.Cancel();


                if (_options.WinRequirement != 0 && Users[guildUser] == _options.WinRequirement)
                {
                    ShouldStopGame = true;
                    try
                    {
                        var embedS = _eb.Create()
                                        .WithOkColor()
                                        .WithTitle(GetText(strs.trivia_game))
                                        .WithDescription(GetText(strs.trivia_win(guildUser.Mention,
                                            Format.Bold(CurrentQuestion.Answer))));
                        if (Uri.IsWellFormedUriString(CurrentQuestion.AnswerImageUrl, UriKind.Absolute))
                            embedS.WithImageUrl(CurrentQuestion.AnswerImageUrl);
                        await Channel.EmbedAsync(embedS);
                    }
                    catch
                    {
                        // ignored
                    }

                    var reward = _config.Trivia.CurrencyReward;
                    if (reward > 0)
                        await _cs.AddAsync(guildUser, reward, new("trivia", "win"));
                    return;
                }

                var embed = _eb.Create()
                               .WithOkColor()
                               .WithTitle(GetText(strs.trivia_game))
                               .WithDescription(GetText(strs.trivia_guess(guildUser.Mention,
                                   Format.Bold(CurrentQuestion.Answer))));
                if (Uri.IsWellFormedUriString(CurrentQuestion.AnswerImageUrl, UriKind.Absolute))
                    embed.WithImageUrl(CurrentQuestion.AnswerImageUrl);
                await Channel.EmbedAsync(embed);
            }
            catch (Exception ex) { Log.Warning(ex, "Exception in a potential guess"); }
        });
        return Task.CompletedTask;
    }

    public string GetLeaderboard()
    {
        if (Users.Count == 0)
            return GetText(strs.no_results);

        var sb = new StringBuilder();

        foreach (var kvp in Users.OrderByDescending(kvp => kvp.Value))
            sb.AppendLine(GetText(strs.trivia_points(Format.Bold(kvp.Key.ToString()), kvp.Value)));

        return sb.ToString();
    }
}