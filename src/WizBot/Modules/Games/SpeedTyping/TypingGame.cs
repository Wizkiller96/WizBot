﻿#nullable disable
using CommandLine;
using WizBot.Modules.Games.Services;
using System.Diagnostics;

namespace WizBot.Modules.Games.Common;

public class TypingGame
{
    public const float WORD_VALUE = 4.5f;
    public ITextChannel Channel { get; }
    public string CurrentSentence { get; private set; }
    public bool IsActive { get; private set; }
    private readonly Stopwatch _sw;
    private readonly List<ulong> _finishedUserIds;
    private readonly DiscordSocketClient _client;
    private readonly GamesService _games;
    private readonly string _prefix;
    private readonly Options _options;
    private readonly IEmbedBuilderService _eb;

    public TypingGame(
        GamesService games,
        DiscordSocketClient client,
        ITextChannel channel,
        string prefix,
        Options options,
        IEmbedBuilderService eb)
    {
        _games = games;
        _client = client;
        _prefix = prefix;
        _options = options;
        _eb = eb;

        Channel = channel;
        IsActive = false;
        _sw = new();
        _finishedUserIds = new();
    }

    public async Task<bool> Stop()
    {
        if (!IsActive)
            return false;
        _client.MessageReceived -= AnswerReceived;
        _finishedUserIds.Clear();
        IsActive = false;
        _sw.Stop();
        _sw.Reset();
        try
        {
            await Channel.SendConfirmAsync(_eb, "Typing contest stopped.");
        }
        catch
        {
        }

        return true;
    }

    public async Task Start()
    {
        if (IsActive)
            return; // can't start running game
        IsActive = true;
        CurrentSentence = GetRandomSentence();
        var i = (int)(CurrentSentence.Length / WORD_VALUE * 1.7f);
        try
        {
            await Channel.SendConfirmAsync(_eb,
                $@":clock2: Next contest will last for {i} seconds. Type the bolded text as fast as you can.");


            var time = _options.StartTime;

            var msg = await Channel.SendMessageAsync($"Starting new typing contest in **{time}**...",
                options: new()
                {
                    RetryMode = RetryMode.AlwaysRetry
                });

            do
            {
                await Task.Delay(2000);
                time -= 2;
                try { await msg.ModifyAsync(m => m.Content = $"Starting new typing contest in **{time}**.."); }
                catch { }
            } while (time > 2);

            await msg.ModifyAsync(m =>
            {
                m.Content = CurrentSentence.Replace(" ", " \x200B", StringComparison.InvariantCulture);
            });
            _sw.Start();
            HandleAnswers();

            while (i > 0)
            {
                await Task.Delay(1000);
                i--;
                if (!IsActive)
                    return;
            }
        }
        catch { }
        finally
        {
            await Stop();
        }
    }

    public string GetRandomSentence()
    {
        if (_games.TypingArticles.Any())
            return _games.TypingArticles[new WizBotRandom().Next(0, _games.TypingArticles.Count)].Text;
        return $"No typing articles found. Use {_prefix}typeadd command to add a new article for typing.";
    }

    private void HandleAnswers()
        => _client.MessageReceived += AnswerReceived;

    private Task AnswerReceived(SocketMessage imsg)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (imsg.Author.IsBot)
                    return;
                if (imsg is not SocketUserMessage msg)
                    return;

                if (Channel is null || Channel.Id != msg.Channel.Id)
                    return;

                var guess = msg.Content;

                var distance = CurrentSentence.LevenshteinDistance(guess);
                var decision = Judge(distance, guess.Length);
                if (decision && !_finishedUserIds.Contains(msg.Author.Id))
                {
                    var elapsed = _sw.Elapsed;
                    var wpm = CurrentSentence.Length / WORD_VALUE / elapsed.TotalSeconds * 60;
                    _finishedUserIds.Add(msg.Author.Id);
                    await Channel.EmbedAsync(_eb.Create()
                                                .WithOkColor()
                                                .WithTitle($"{msg.Author} finished the race!")
                                                .AddField("Place", $"#{_finishedUserIds.Count}", true)
                                                .AddField("WPM", $"{wpm:F1} *[{elapsed.TotalSeconds:F2}sec]*", true)
                                                .AddField("Errors", distance.ToString(), true));

                    if (_finishedUserIds.Count % 4 == 0)
                    {
                        await Channel.SendConfirmAsync(_eb,
                            ":exclamation: A lot of people finished, here is the text for those still typing:"
                            + $"\n\n**{Format.Sanitize(CurrentSentence.Replace(" ", " \x200B", StringComparison.InvariantCulture)).SanitizeMentions(true)}**");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error receiving typing game answer: {ErrorMessage}", ex.Message);
            }
        });
        return Task.CompletedTask;
    }

    private static bool Judge(int errors, int textLength)
        => errors <= textLength / 25;

    public class Options : IWizBotCommandOptions
    {
        [Option('s',
            "start-time",
            Default = 5,
            Required = false,
            HelpText = "How long does it take for the race to start. Default 5.")]
        public int StartTime { get; set; } = 5;

        public void NormalizeOptions()
        {
            if (StartTime is < 3 or > 30)
                StartTime = 5;
        }
    }
}