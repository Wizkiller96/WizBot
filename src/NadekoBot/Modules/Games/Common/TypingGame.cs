using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using NadekoBot.Common;
using NadekoBot.Extensions;
using NadekoBot.Modules.Games.Services;
using CommandLine;
using NadekoBot.Services;
using Serilog;

namespace NadekoBot.Modules.Games.Common
{
    public class TypingGame
    {
        public class Options : INadekoCommandOptions
        {
            [Option('s', "start-time", Default = 5, Required = false, HelpText = "How long does it take for the race to start. Default 5.")]
            public int StartTime { get; set; } = 5;

            public void NormalizeOptions()
            {
                if (StartTime < 3 || StartTime > 30)
                    StartTime = 5;
            }
        }

        public const float WORD_VALUE = 4.5f;
        public ITextChannel Channel { get; }
        public string CurrentSentence { get; private set; }
        public bool IsActive { get; private set; }
        private readonly Stopwatch sw;
        private readonly List<ulong> finishedUserIds;
        private readonly DiscordSocketClient _client;
        private readonly GamesService _games;
        private readonly string _prefix;
        private readonly Options _options;
        private readonly IEmbedBuilderService _eb;

        public TypingGame(GamesService games, DiscordSocketClient client, ITextChannel channel, 
            string prefix, Options options, IEmbedBuilderService eb)
        {
            _games = games;
            _client = client;
            _prefix = prefix;
            _options = options;
            _eb = eb;

            this.Channel = channel;
            IsActive = false;
            sw = new Stopwatch();
            finishedUserIds = new List<ulong>();
        }

        public async Task<bool> Stop()
        {
            if (!IsActive) return false;
            _client.MessageReceived -= AnswerReceived;
            finishedUserIds.Clear();
            IsActive = false;
            sw.Stop();
            sw.Reset();
            try
            {
                await Channel.SendConfirmAsync(_eb, "Typing contest stopped.");
            }
            catch (Exception ex)
            {
                Log.Warning(ex.ToString());
            }

            return true;
        }

        public async Task Start()
        {
            if (IsActive) return; // can't start running game
            IsActive = true;
            CurrentSentence = GetRandomSentence();
            var i = (int)(CurrentSentence.Length / WORD_VALUE * 1.7f);
            try
            {
                await Channel.SendConfirmAsync(_eb,
                    $@":clock2: Next contest will last for {i} seconds. Type the bolded text as fast as you can.");


                var time = _options.StartTime;

                var msg = await Channel.SendMessageAsync($"Starting new typing contest in **{time}**...", options: new RequestOptions()
                {
                    RetryMode = RetryMode.AlwaysRetry
                }).ConfigureAwait(false);

                do
                {
                    await Task.Delay(2000).ConfigureAwait(false);
                    time -= 2;
                    try { await msg.ModifyAsync(m => m.Content = $"Starting new typing contest in **{time}**..").ConfigureAwait(false); } catch { }
                } while (time > 2);

                await msg.ModifyAsync(m => {
                    m.Content = CurrentSentence.Replace(" ", " \x200B", StringComparison.InvariantCulture);
                }).ConfigureAwait(false);
                sw.Start();
                HandleAnswers();

                while (i > 0)
                {
                    await Task.Delay(1000).ConfigureAwait(false);
                    i--;
                    if (!IsActive)
                        return;
                }

            }
            catch { }
            finally
            {
                await Stop().ConfigureAwait(false);
            }
        }

        public string GetRandomSentence()
        {
            if (_games.TypingArticles.Any())
                return _games.TypingArticles[new NadekoRandom().Next(0, _games.TypingArticles.Count)].Text;
            else
                return $"No typing articles found. Use {_prefix}typeadd command to add a new article for typing.";

        }

        private void HandleAnswers()
        {
            _client.MessageReceived += AnswerReceived;
        }

        private Task AnswerReceived(SocketMessage imsg)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (imsg.Author.IsBot)
                        return;
                    var msg = imsg as SocketUserMessage;
                    if (msg is null)
                        return;

                    if (this.Channel is null || this.Channel.Id != msg.Channel.Id) return;

                    var guess = msg.Content;

                    var distance = CurrentSentence.LevenshteinDistance(guess);
                    var decision = Judge(distance, guess.Length);
                    if (decision && !finishedUserIds.Contains(msg.Author.Id))
                    {
                        var elapsed = sw.Elapsed;
                        var wpm = CurrentSentence.Length / WORD_VALUE / elapsed.TotalSeconds * 60;
                        finishedUserIds.Add(msg.Author.Id);
                        await this.Channel.EmbedAsync(_eb.Create().WithOkColor()
                            .WithTitle($"{msg.Author} finished the race!")
                            .AddField("Place", $"#{finishedUserIds.Count}", true)
                            .AddField("WPM", $"{wpm:F1} *[{elapsed.TotalSeconds:F2}sec]*", true)
                            .AddField("Errors", distance.ToString(), true));
                        
                        if (finishedUserIds.Count % 4 == 0)
                        {
                            await this.Channel.SendConfirmAsync(_eb,
                                    $":exclamation: A lot of people finished, here is the text for those still typing:" +
                                    $"\n\n**{Format.Sanitize(CurrentSentence.Replace(" ", " \x200B", StringComparison.InvariantCulture)).SanitizeMentions(true)}**")
                                .ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex.ToString());
                }
            });
            return Task.CompletedTask;
        }

        private static bool Judge(int errors, int textLength) => errors <= textLength / 25;

    }
}