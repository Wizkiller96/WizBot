﻿using Discord;
using Discord.Commands;
using WizBot.Attributes;
using WizBot.Extensions;
using WizBot.Modules.Games.Commands.Models;
using WizBot.Services;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WizBot.Modules.Games
{
    public partial class Games
    {
        public class TypingGame
        {
            public const float WORD_VALUE = 4.5f;
            public ITextChannel Channel { get; }
            public string CurrentSentence { get; private set; }
            public bool IsActive { get; private set; }
            private readonly Stopwatch sw;
            private readonly List<ulong> finishedUserIds;
            private Logger _log { get; }

            public TypingGame(ITextChannel channel)
            {
                _log = LogManager.GetCurrentClassLogger();
                this.Channel = channel;
                IsActive = false;
                sw = new Stopwatch();
                finishedUserIds = new List<ulong>();
            }

            public async Task<bool> Stop()
            {
                if (!IsActive) return false;
                WizBot.Client.MessageReceived -= AnswerReceived;
                finishedUserIds.Clear();
                IsActive = false;
                sw.Stop();
                sw.Reset();
                try { await Channel.SendConfirmAsync("Typing contest stopped.").ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
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
                    await Channel.SendConfirmAsync($@":clock2: Next contest will last for {i} seconds. Type the bolded text as fast as you can.").ConfigureAwait(false);


                    var msg = await Channel.SendMessageAsync("Starting new typing contest in **3**...").ConfigureAwait(false);
                    await Task.Delay(1000).ConfigureAwait(false);
                    try
                    {
                        await msg.ModifyAsync(m => m.Content = "Starting new typing contest in **2**...").ConfigureAwait(false);
                        await Task.Delay(1000).ConfigureAwait(false);
                        await msg.ModifyAsync(m => m.Content = "Starting new typing contest in **1**...").ConfigureAwait(false);
                        await Task.Delay(1000).ConfigureAwait(false);
                    }
                    catch (Exception ex) { _log.Warn(ex); }

                    await msg.ModifyAsync(m => m.Content = Format.Bold(Format.Sanitize(CurrentSentence.Replace(" ", " \x200B")).SanitizeMentions())).ConfigureAwait(false);
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
                if (SpeedTypingCommands.TypingArticles.Any())
                    return SpeedTypingCommands.TypingArticles[new WizBotRandom().Next(0, SpeedTypingCommands.TypingArticles.Count)].Text;
                else
                    return $"No typing articles found. Use {WizBot.ModulePrefixes[typeof(Games).Name]}typeadd command to add a new article for typing.";

            }

            private void HandleAnswers()
            {
                WizBot.Client.MessageReceived += AnswerReceived;
            }

            private Task AnswerReceived(IMessage imsg)
            {
                if (imsg.Author.IsBot)
                    return Task.CompletedTask;
                var msg = imsg as IUserMessage;
                if (msg == null)
                    return Task.CompletedTask;
                var t = Task.Run(async () =>
                {
                    try
                    {
                        if (this.Channel == null || this.Channel.Id != this.Channel.Id) return;

                        var guess = msg.Content;

                        var distance = CurrentSentence.LevenshteinDistance(guess);
                        var decision = Judge(distance, guess.Length);
                        if (decision && !finishedUserIds.Contains(msg.Author.Id))
                        {
                            var wpm = CurrentSentence.Length / WORD_VALUE / sw.Elapsed.Seconds * 60;
                            finishedUserIds.Add(msg.Author.Id);
                            await Extensions.Extensions.EmbedAsync(this.Channel, (Discord.API.Embed)new EmbedBuilder().WithColor((uint)WizBot.OkColor)
                                .WithTitle((string)$"{msg.Author} finished the race!")
                                .AddField(efb => efb.WithName("Place").WithValue($"#{finishedUserIds.Count}").WithIsInline(true))
                                .AddField(efb => efb.WithName("WPM").WithValue($"{wpm:F2} *[{sw.Elapsed.Seconds.ToString()}sec]*").WithIsInline(true))
                                .AddField(efb => efb.WithName((string)"Errors").WithValue((string)distance.ToString()).WithIsInline((bool)true))
                                .Build()).ConfigureAwait(false);
                            if (finishedUserIds.Count % 4 == 0)
                            {
                                await Extensions.Extensions.SendConfirmAsync(this.Channel, (string)$":exclamation: A lot of people finished, here is the text for those still typing:\n\n**{Format.Sanitize((string)CurrentSentence.Replace((string)" ", (string)" \x200B")).SanitizeMentions()}**").ConfigureAwait(false);
                            }
                        }
                    }
                    catch { }
                });
                return Task.CompletedTask;
            }

            private bool Judge(int errors, int textLength) => errors <= textLength / 25;

        }

        [Group]
        public class SpeedTypingCommands
        {

            public static List<TypingArticle> TypingArticles { get; } = new List<TypingArticle>();

            const string typingArticlesPath = "data/typing_articles.json";

            static SpeedTypingCommands()
            {
                try { TypingArticles = JsonConvert.DeserializeObject<List<TypingArticle>>(File.ReadAllText(typingArticlesPath)); } catch { }
            }
            public static ConcurrentDictionary<ulong, TypingGame> RunningContests;

            public SpeedTypingCommands()
            {
                RunningContests = new ConcurrentDictionary<ulong, TypingGame>();
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task TypeStart(IUserMessage msg)
            {
                var channel = (ITextChannel)msg.Channel;

                var game = RunningContests.GetOrAdd(channel.Guild.Id, id => new TypingGame(channel));

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
            public async Task TypeStop(IUserMessage imsg)
            {
                var channel = (ITextChannel)imsg.Channel;
                TypingGame game;
                if (RunningContests.TryRemove(channel.Guild.Id, out game))
                {
                    await game.Stop().ConfigureAwait(false);
                    return;
                }
                await channel.SendErrorAsync("No contest to stop on this channel.").ConfigureAwait(false);
            }


            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task Typeadd(IUserMessage imsg, [Remainder] string text)
            {
                var channel = (ITextChannel)imsg.Channel;

                TypingArticles.Add(new TypingArticle
                {
                    Title = $"Text added on {DateTime.UtcNow} by {imsg.Author}",
                    Text = text.SanitizeMentions(),
                });

                File.WriteAllText(typingArticlesPath, JsonConvert.SerializeObject(TypingArticles));

                await channel.SendConfirmAsync("Added new article for typing game.").ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Typelist(IUserMessage imsg, int page = 1)
            {
                var channel = (ITextChannel)imsg.Channel;

                if (page < 1)
                    return;

                var articles = TypingArticles.Skip((page - 1) * 15).Take(15);

                if (!articles.Any())
                {
                    await channel.SendErrorAsync($"{imsg.Author.Mention} `No articles found on that page.`").ConfigureAwait(false);
                    return;
                }
                var i = (page - 1) * 15;
                await channel.SendConfirmAsync("List of articles for Type Race", String.Join("\n", articles.Select(a => $"`#{++i}` - {a.Text.TrimTo(50)}")))
                             .ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task Typedel(IUserMessage imsg, int index)
            {
                var channel = (ITextChannel)imsg.Channel;

                index -= 1;
                if (index < 0 || index >= TypingArticles.Count)
                    return;

                var removed = TypingArticles[index];
                TypingArticles.RemoveAt(index);

                File.WriteAllText(typingArticlesPath, JsonConvert.SerializeObject(TypingArticles));

                await channel.SendConfirmAsync($"`Removed typing article:` #{index + 1} - {removed.Text.TrimTo(50)}")
                             .ConfigureAwait(false);
            }
        }
    }
}