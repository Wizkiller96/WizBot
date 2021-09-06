#nullable enable
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using NadekoBot.Common;
using NadekoBot.Common.Collections;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Common.Replacements;
using Serilog;

namespace NadekoBot.Modules.Utility.Services
{
    public sealed class RepeaterService : IReadyExecutor, INService
    {
        public const int MAX_REPEATERS = 5;

        private readonly DbService _db;
        private readonly IBotCredentials _creds;
        private readonly DiscordSocketClient _client;
        private LinkedList<RunningRepeater> _repeaterQueue;
        private ConcurrentHashSet<int> _noRedundant;

        private readonly object _queueLocker = new object();

        public RepeaterService(DiscordSocketClient client, DbService db, IBotCredentials creds)
        {
            _db = db;
            _creds = creds;
            _client = client;
            
            var uow = _db.GetDbContext();
            var shardRepeaters = uow
                ._context
                .Set<Repeater>()
                .FromSqlInterpolated($@"select * from repeaters 
where ((guildid >> 22) % {_creds.TotalShards}) == {_client.ShardId};")
                .AsNoTracking()
                .ToList();

            _noRedundant = new ConcurrentHashSet<int>(shardRepeaters
                .Where(x => x.NoRedundant)
                .Select(x => x.Id));

            _repeaterQueue = new LinkedList<RunningRepeater>(shardRepeaters
                .Select(rep => new RunningRepeater(rep))
                .OrderBy(x => x.NextTime));
        }
        
        public Task OnReadyAsync()
        {
            _ = Task.Run(RunRepeatersLoop);
            return Task.CompletedTask;
        }

        private async Task RunRepeatersLoop()
        {
            while (true)
            {
                try
                {
                    // calculate timeout for the first item
                    var timeout = GetNextTimeout();
                    
                    // wait it out, and recalculate afterwards
                    // because repeaters might've been modified meanwhile
                    if (timeout > TimeSpan.Zero)
                    {
                        await Task.Delay(timeout);
                        continue;
                    }

                    // collect (remove) all repeaters which need to run (3 seconds tolerance)
                    var now = DateTime.UtcNow + TimeSpan.FromSeconds(3);

                    var toExecute = new List<RunningRepeater>();
                    while (true)
                    {
                        lock (_repeaterQueue)
                        {
                            var current = _repeaterQueue.First;
                            if (current is null || current.Value.NextTime > now)
                                break;

                            toExecute.Add(current.Value);
                            _repeaterQueue.RemoveFirst();
                        }
                    }

                    // execute
                    foreach (var chunk in toExecute.Chunk(5))
                    {
                        await Task.WhenAll(chunk.Select(Trigger));
                    }

                    // reinsert
                    foreach (var rep in toExecute)
                    {
                        await HandlePostExecute(rep);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Critical error in repeater queue: {ErrorMessage}", ex.Message);
                    await Task.Delay(5000);
                }
            }
        }

        private async Task HandlePostExecute(RunningRepeater rep)
        {
            if (rep.ErrorCount >= 10)
            {
                await RemoveRepeaterInternal(rep.Repeater);
                return;
            }
            
            rep.UpdateNextTime();
            AddToQueue(rep);
        }
        
        public async Task<bool> TriggerExternal(ulong guildId, int index)
        {
            using var uow = _db.GetDbContext();
            
            var toTrigger = await uow._context.Repeaters
                .AsNoTracking()
                .Skip(index)
                .FirstOrDefaultAsyncEF(x => x.GuildId == guildId);

            if (toTrigger is null)
                return false;

            LinkedListNode<RunningRepeater>? node;
            lock (_queueLocker)
            {
                node = _repeaterQueue.FindNode(x => x.Repeater.Id == toTrigger.Id);
                if (node is null)
                    return false;
                
                _repeaterQueue.Remove(node);
            }

            await Trigger(node.Value);
            await HandlePostExecute(node.Value);
            return true;
        }
        
        private void AddToQueue(RunningRepeater rep)
        {
            lock (_queueLocker)
            {
                var current = _repeaterQueue.First;
                if (current is null)
                {
                    _repeaterQueue.AddFirst(rep);
                    return;
                }

                while (!(current is null) && current.Value.NextTime < rep.NextTime)
                    current = current.Next;

                if (current is null)
                    _repeaterQueue.AddLast(rep);
                else
                    _repeaterQueue.AddBefore(current, rep);
            }
        }

        private TimeSpan GetNextTimeout()
        {
            lock (_queueLocker)
            {
                var first = _repeaterQueue.First;

                // if there are no items in the queue, just wait out the minimum duration (1 minute) and try again
                if (first is null)
                    return TimeSpan.FromMinutes(1);

                return first.Value.NextTime - DateTime.UtcNow;
            }
        }
        
        private async Task Trigger(RunningRepeater rr)
        {
            var repeater = rr.Repeater;
            
            void ChannelMissingError()
            {
                rr.ErrorCount = Int32.MaxValue;
                Log.Warning("[Repeater] Channel [{Channelid}] for not found or insufficient permissions. " +
                            "Repeater will be removed. ", repeater.ChannelId);
            }

            var channel = _client.GetChannel(repeater.ChannelId) as ITextChannel;
            if (channel is null)
                channel = await _client.Rest.GetChannelAsync(repeater.ChannelId) as ITextChannel;

            if (channel is null)
            {
                ChannelMissingError();
                return;
            }

            var guild = _client.GetGuild(channel.GuildId);
            if (guild is null)
            {
                ChannelMissingError();
                return;
            }
            
            if (_noRedundant.Contains(repeater.Id))
            {
                try
                {
                    var lastMsgInChannel = await channel.GetMessagesAsync(2).Flatten().FirstAsync();
                    if (lastMsgInChannel != null && lastMsgInChannel.Id == repeater.LastMessageId)
                        return;
                }
                catch (Exception ex)
                {
                    Log.Warning(ex,
                        "[Repeater] Error while getting last channel message in {GuildId}/{ChannelId} " +
                        "Bot probably doesn't have the permission to read message history",
                        guild.Id,
                        channel.Id);
                }
            }

            if (repeater.LastMessageId is ulong lastMessageId)
            {
                try
                {
                    var oldMsg = await channel.GetMessageAsync(lastMessageId);
                    if (oldMsg != null)
                    {
                        await oldMsg.DeleteAsync().ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "[Repeater] Error while deleting previous message in {GuildId}/{ChannelId}", guild.Id, channel.Id);
                }
            }
            
            var rep = new ReplacementBuilder()
                .WithDefault(guild.CurrentUser, channel, guild, _client)
                .Build();

            try
            {
                IMessage newMsg;
                if (CREmbed.TryParse(repeater.Message, out var crEmbed))
                {
                    rep.Replace(crEmbed);
                    newMsg = await channel.EmbedAsync(crEmbed);
                }
                else
                {
                    newMsg = await channel.SendMessageAsync(rep.Replace(repeater.Message));
                }

                _ = newMsg.AddReactionAsync(new Emoji("🔄"));
                if (_noRedundant.Contains(repeater.Id))
                {
                    await SetRepeaterLastMessageInternal(repeater.Id, newMsg.Id);
                    repeater.LastMessageId = newMsg.Id;
                }

                rr.ErrorCount = 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Repeater] Error sending repeat message ({ErrorCount})", rr.ErrorCount++);
            }
        } 

        private async Task RemoveRepeaterInternal(Repeater r)
        {
            _noRedundant.TryRemove(r.Id);
            
            using var uow = _db.GetDbContext();
            await uow._context
                .Repeaters
                .DeleteAsync(x => x.Id == r.Id);
            
            await uow.SaveChangesAsync();
        }

        private RunningRepeater? RemoveFromQueue(int id)
        {
            lock (_queueLocker)
            {
                var node = _repeaterQueue.FindNode(x => x.Repeater.Id == id);
                if (node is null)
                    return null;
                
                _repeaterQueue.Remove(node);
                return node.Value;
            }
        }

        private async Task SetRepeaterLastMessageInternal(int repeaterId, ulong lastMsgId)
        {
            using var uow = _db.GetDbContext();
            await uow._context.Repeaters
                .AsQueryable()
                .Where(x => x.Id == repeaterId)
                .UpdateAsync(rep => new Repeater()
                {
                    LastMessageId = lastMsgId
                });
        }

        public async Task<RunningRepeater?> AddRepeaterAsync(
            ulong channelId,
            ulong guildId,
            TimeSpan interval,
            string message,
            bool isNoRedundant,
            TimeSpan? startTimeOfDay
        )
        {
            var rep = new Repeater()
            {
                ChannelId = channelId,
                GuildId = guildId,
                Interval = interval,
                Message = message,
                NoRedundant = isNoRedundant,
                LastMessageId = null,
                StartTimeOfDay = startTimeOfDay,
                DateAdded = DateTime.UtcNow
            };

            using var uow = _db.GetDbContext();

            if (await uow._context.Repeaters.AsNoTracking().CountAsyncEF() < MAX_REPEATERS)
                uow._context.Repeaters.Add(rep);
            else
                return null;

            await uow.SaveChangesAsync();

            if (isNoRedundant)
                _noRedundant.Add(rep.Id);
            var runner = new RunningRepeater(rep);
            AddToQueue(runner);
            return runner;
        }

        public async Task<RunningRepeater?> RemoveByIndexAsync(ulong guildId, int index)
        {
            if (index > MAX_REPEATERS * 2)
                throw new ArgumentOutOfRangeException(nameof(index));
            
            using var uow = _db.GetDbContext();
            var toRemove = await uow._context.Repeaters
                .AsNoTracking()
                .Skip(index)
                .FirstOrDefaultAsyncEF(x => x.GuildId == guildId);

            if (toRemove is null)
                return null;
            
            // first try removing from queue because it can fail
            // while triggering. Instruct user to try again
            var removed = RemoveFromQueue(toRemove.Id);
            if (removed is null)
                return null;

            _noRedundant.TryRemove(toRemove.Id);
            uow._context.Repeaters.Remove(toRemove);
            await uow.SaveChangesAsync();
            return removed;
        }

        public IReadOnlyCollection<RunningRepeater> GetRepeaters(ulong guildId)
        {
            lock (_queueLocker)
            {
                return _repeaterQueue.Where(x => x.Repeater.GuildId == guildId).ToList();
            }
        }

        public async Task<bool?> ToggleRedundantAsync(ulong guildId, int index)
        {
            using var uow = _db.GetDbContext();
            var toToggle = await uow._context
                .Repeaters
                .AsQueryable()
                .Skip(index)
                .FirstOrDefaultAsyncEF(x => x.GuildId == guildId);

            if (toToggle is null)
                return null;
            
            var newValue = toToggle.NoRedundant = !toToggle.NoRedundant;
            if (newValue)
            {
                _noRedundant.Add(toToggle.Id);
            }
            else
            {
                _noRedundant.TryRemove(toToggle.Id);
            }

            await uow.SaveChangesAsync();
            return newValue;
        }

        public bool IsNoRedundant(int repeaterId)
        {
            return _noRedundant.Contains(repeaterId);
        }
    }
}
