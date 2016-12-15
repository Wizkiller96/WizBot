﻿using Discord;
using Discord.Commands;
using WizBot.Attributes;
using WizBot.Extensions;
using WizBot.Services;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WizBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class AnimalRacing
        {
            public static ConcurrentDictionary<ulong, AnimalRace> AnimalRaces { get; } = new ConcurrentDictionary<ulong, AnimalRace>();

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Race(IUserMessage umsg)
            {
                var channel = (ITextChannel)umsg.Channel;

                var ar = new AnimalRace(channel.Guild.Id, channel);

                if (ar.Fail)
                    await channel.SendErrorAsync("🏁 `Failed starting a race. Another race is probably running.`");
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task JoinRace(IUserMessage umsg, int amount = 0)
            {
                var channel = (ITextChannel)umsg.Channel;

                if (amount < 0)
                    amount = 0;


                AnimalRace ar;
                if (!AnimalRaces.TryGetValue(channel.Guild.Id, out ar))
                {
                    await channel.SendErrorAsync("No race exists on this server");
                    return;
                }
                await ar.JoinRace(umsg.Author as IGuildUser, amount);
            }

            public class AnimalRace
            {

                private ConcurrentQueue<string> animals { get; }

                public bool Fail { get; set; }

                public List<Participant> participants = new List<Participant>();
                private ulong serverId;
                private int messagesSinceGameStarted = 0;
                private Logger _log { get; }

                public ITextChannel raceChannel { get; set; }
                public bool Started { get; private set; } = false;

                public AnimalRace(ulong serverId, ITextChannel ch)
                {
                    this._log = LogManager.GetCurrentClassLogger();
                    this.serverId = serverId;
                    this.raceChannel = ch;
                    if (!AnimalRaces.TryAdd(serverId, this))
                    {
                        Fail = true;
                        return;
                    }

                    using (var uow = DbHandler.UnitOfWork())
                    {
                        animals = new ConcurrentQueue<string>(uow.BotConfig.GetOrCreate().RaceAnimals.Select(ra => ra.Icon).Shuffle());
                    }


                    var cancelSource = new CancellationTokenSource();
                    var token = cancelSource.Token;
                    var fullgame = CheckForFullGameAsync(token);
                    Task.Run(async () =>
                    {
                        try
                        {
                            try { await raceChannel.SendConfirmAsync($"🏁`Race is starting in 20 seconds or when the room is full. Type {WizBot.ModulePrefixes[typeof(Gambling).Name]}jr to join the race.`"); } catch (Exception ex) { _log.Warn(ex); }
                            var t = await Task.WhenAny(Task.Delay(20000, token), fullgame);
                            Started = true;
                            cancelSource.Cancel();
                            if (t == fullgame)
                            {
                                try { await raceChannel.SendConfirmAsync("🏁`Race full, starting right now!`"); } catch (Exception ex) { _log.Warn(ex); }
                            }
                            else if (participants.Count > 1)
                            {
                                try { await raceChannel.SendConfirmAsync("🏁`Game starting with " + participants.Count + " participants.`"); } catch (Exception ex) { _log.Warn(ex); }
                            }
                            else
                            {
                                try { await raceChannel.SendErrorAsync("🏁`Race failed to start since there was not enough participants.`"); } catch (Exception ex) { _log.Warn(ex); }
                                var p = participants.FirstOrDefault();

                                if (p != null && p.AmountBet > 0)
                                    await CurrencyHandler.AddCurrencyAsync(p.User, "BetRace", p.AmountBet, false).ConfigureAwait(false);
                                End();
                                return;
                            }
                            await Task.Run(StartRace);
                            End();
                        }
                        catch { try { End(); } catch { } }
                    });
                }

                private void End()
                {
                    AnimalRace throwaway;
                    AnimalRaces.TryRemove(serverId, out throwaway);
                }

                private async Task StartRace()
                {
                    var rng = new WizBotRandom();
                    Participant winner = null;
                    IUserMessage msg = null;
                    int place = 1;
                    try
                    {
                        WizBot.Client.MessageReceived += Client_MessageReceived;

                        while (!participants.All(p => p.Total >= 60))
                        {
                            //update the state
                            participants.ForEach(p =>
                            {
                                p.Total += 1 + rng.Next(0, 10);
                                if (p.Total > 60)
                                {
                                    p.Total = 60;
                                    if (winner == null)
                                    {
                                        winner = p;
                                    }
                                    if (p.Place == 0)
                                        p.Place = place++;
                                }
                            });


                            //draw the state

                            var text = $@"|🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🔚|
{String.Join("\n", participants.Select(p => $"{(int)(p.Total / 60f * 100),-2}%|{p.ToString()}"))}
|🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🏁🔚|";
                            if (msg == null || messagesSinceGameStarted >= 10) // also resend the message if channel was spammed
                            {
                                if (msg != null)
                                    try { await msg.DeleteAsync(); } catch { }
                                messagesSinceGameStarted = 0;
                                try { msg = await raceChannel.SendMessageAsync(text).ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
                            }
                            else
                            {
                                try { await msg.ModifyAsync(m => m.Content = text).ConfigureAwait(false); } catch (Exception ex) { _log.Warn(ex); }
                            }

                            await Task.Delay(2500);
                        }
                    }
                    catch { }
                    finally
                    {
                        WizBot.Client.MessageReceived -= Client_MessageReceived;
                    }

                    if (winner.AmountBet > 0)
                    {
                        var wonAmount = winner.AmountBet * (participants.Count - 1);
                        
                        await CurrencyHandler.AddCurrencyAsync(winner.User, "Won a Race", wonAmount, false).ConfigureAwait(false);
                        await raceChannel.SendConfirmAsync($"🏁 {winner.User.Mention} as {winner.Animal} **Won the race and {wonAmount}{CurrencySign}!**").ConfigureAwait(false);
                    }
                    else
                    {
                        await raceChannel.SendConfirmAsync($"🏁 {winner.User.Mention} as {winner.Animal} **Won the race!**");
                    }

                }

                private Task Client_MessageReceived(IMessage imsg)
                {
                    var msg = imsg as IUserMessage;
                    if (msg == null)
                        return Task.CompletedTask;
                    if (msg.IsAuthor() || !(imsg.Channel is ITextChannel) || imsg.Channel != raceChannel)
                        return Task.CompletedTask;
                    messagesSinceGameStarted++;
                    return Task.CompletedTask;
                }

                private async Task CheckForFullGameAsync(CancellationToken cancelToken)
                {
                    while (animals.Count > 0)
                    {
                        await Task.Delay(100, cancelToken);
                    }
                }

                public async Task JoinRace(IGuildUser u, int amount = 0)
                {
                    var animal = "";
                    if (!animals.TryDequeue(out animal))
                    {
                        await raceChannel.SendErrorAsync($"{u.Mention} `There is no running race on this server.`");
                        return;
                    }
                    var p = new Participant(u, animal, amount);
                    if (participants.Contains(p))
                    {
                        await raceChannel.SendErrorAsync($"{u.Mention} `You already joined this race.`");
                        return;
                    }
                    if (Started)
                    {
                        await raceChannel.SendErrorAsync($"{u.Mention} `Race is already started`");
                        return;
                    }
                    if (amount > 0)
                        if (!await CurrencyHandler.RemoveCurrencyAsync((IGuildUser)u, "BetRace", amount, true).ConfigureAwait(false))
                        {
                            try { await raceChannel.SendErrorAsync($"{u.Mention} You don't have enough {Gambling.CurrencyName}s.").ConfigureAwait(false); } catch { }
                            return;
                        }
                    participants.Add(p);
                    await raceChannel.SendConfirmAsync($"{u.Mention} **joined the race as a {p.Animal}" + (amount > 0 ? $" and bet {amount} {CurrencySign}!**" : "**"));
                }
            }

            public class Participant
            {
                public IGuildUser User { get; set; }
                public string Animal { get; set; }
                public int AmountBet { get; set; }

                public float Coeff { get; set; }
                public int Total { get; set; }

                public int Place { get; set; } = 0;

                public Participant(IGuildUser u, string a, int amount)
                {
                    this.User = u;
                    this.Animal = a;
                    this.AmountBet = amount;
                }

                public override int GetHashCode()
                {
                    return User.GetHashCode();
                }

                public override bool Equals(object obj)
                {
                    var p = obj as Participant;
                    return p == null ?
                        false :
                        p.User == User;
                }

                public override string ToString()
                {
                    var str = new string('‣', Total) + Animal;
                    if (Place == 0)
                        return str;
                    if (Place == 1)
                    {
                        return str + "🏆";
                    }
                    else if (Place == 2)
                    {
                        return str + "`2nd`";
                    }
                    else if (Place == 3)
                    {
                        return str + "`3rd`";
                    }
                    else
                    {
                        return str + $"`{Place}th`";
                    }

                }
            }
        }
    }
}