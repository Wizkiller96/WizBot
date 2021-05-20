using Discord;
using Discord.WebSocket;
using WizBot.Common;
using WizBot.Common.ModuleBehaviors;
using WizBot.Core.Services;
using WizBot.Core.Services.Database;
using WizBot.Core.Services.Database.Models;
using WizBot.Extensions;
using WizBot.Modules.CustomReactions.Extensions;
using WizBot.Modules.Permissions.Common;
using WizBot.Modules.Permissions.Services;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using WizBot.Core.Common;

namespace WizBot.Modules.CustomReactions.Services
{
    public class CustomReactionsService : IEarlyBehavior, INService, IReadyExecutor
    {
        public enum CrField
        {
            AutoDelete,
            DmResponse,
            ContainsAnywhere,
            Message,
        }

        private readonly object _grWriteLock = new object();
        
        private readonly TypedKey<CustomReaction> _gcrAddedKey = new TypedKey<CustomReaction>("gcr.added");
        private readonly TypedKey<int> _gcrDeletedkey = new TypedKey<int>("gcr.deleted");
        private readonly TypedKey<CustomReaction> _gcrEditedKey = new TypedKey<CustomReaction>("gcr.edited");
        private readonly TypedKey<bool> _crsReloadedKey = new TypedKey<bool>("crs.reloaded");
        
        private CustomReaction[] _globalReactions;
        private ConcurrentDictionary<ulong, ConcurrentDictionary<int, CustomReaction>> _guildReactions;

        public int Priority => -1;
        public ModuleBehaviorType BehaviorType => ModuleBehaviorType.Executor;

        private readonly Logger _log;
        private readonly DbService _db;
        private readonly DiscordSocketClient _client;
        private readonly PermissionService _perms;
        private readonly CommandHandler _cmd;
        private readonly IBotStrings _strings;
        private readonly WizBot _bot;
        private readonly GlobalPermissionService _gperm;
        private readonly IPubSub _pubSub;
        private readonly Random _rng;

        public CustomReactionsService(PermissionService perms, DbService db, IBotStrings strings, WizBot bot,
            DiscordSocketClient client, CommandHandler cmd, GlobalPermissionService gperm,
            IPubSub pubSub)
        {
            _log = LogManager.GetCurrentClassLogger();
            _db = db;
            _client = client;
            _perms = perms;
            _cmd = cmd;
            _strings = strings;
            _bot = bot;
            _gperm = gperm;
            _pubSub = pubSub;
            _rng = new WizBotRandom();

            _pubSub.Sub(_crsReloadedKey, OnCrsShouldReload);
            pubSub.Sub(_gcrAddedKey, OnGcrAdded);
            pubSub.Sub(_gcrDeletedkey, OnGcrDeleted);
            pubSub.Sub(_gcrEditedKey, OnGcrEdited);

            bot.JoinedGuild += Bot_JoinedGuild;
            _client.LeftGuild += _client_LeftGuild;
        }

        private Task OnCrsShouldReload(bool _) 
            => ReloadInternal(_bot.GetCurrentGuildIds());

        // it is perfectly fine to have global customreactions as an array
        // 1. global custom reactions are almost never added (compared to how many times they are being looped through)
        // 2. only need write locks for this
        // 3. there's never many of them (at most a thousand, usually < 100)
        private Task OnGcrAdded(CustomReaction c)
        {
            lock (_grWriteLock)
            {
                var newGlobalReactions = new CustomReaction[_globalReactions.Length + 1];
                Array.Copy(_globalReactions, newGlobalReactions, _globalReactions.Length);
                newGlobalReactions[_globalReactions.Length] = c;
                _globalReactions = newGlobalReactions;
            }

            return Task.CompletedTask;
        }

        private Task OnGcrDeleted(int id)
        {
            lock (_grWriteLock)
            {
                var newGlobalReactions = new CustomReaction[_globalReactions.Length - 1];
                for (int i = 0, k = 0; i < _globalReactions.Length; i++, k++)
                {
                    if (_globalReactions[i].Id == id)
                    {
                        k--;
                        continue;
                    }

                    newGlobalReactions[k] = _globalReactions[i];
                }

                _globalReactions = newGlobalReactions;
            }

            return Task.CompletedTask;
        }
        
        private Task OnGcrEdited(CustomReaction c)
        {
            lock (_grWriteLock)
            {
                for (var i = 0; i < _globalReactions.Length; i++)
                {
                    if (_globalReactions[i].Id == c.Id)
                    {
                        _globalReactions[i] = c;
                        return Task.CompletedTask;
                    }
                }

                // if edited cr is not found?!
                // add it
                OnGcrAdded(c);
            }

            return Task.CompletedTask;
        }

        private Task ReloadInternal(IEnumerable<ulong> allGuilds)
        {
            using var uow = _db.GetDbContext();
            return ReloadInternal(allGuilds, uow);
        }

        private async Task ReloadInternal(IEnumerable<ulong> allGuildIds, IUnitOfWork uow)
        {
            var guildItems = await uow.CustomReactions.GetFor(allGuildIds);
            _guildReactions = new ConcurrentDictionary<ulong, ConcurrentDictionary<int, CustomReaction>>(guildItems
                .GroupBy(k => k.GuildId!.Value)
                .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.Id, x => x).ToConcurrent()));

            var globalItems = uow.CustomReactions.GetGlobal();
            lock (_grWriteLock)
            {
                _globalReactions = globalItems.ToArray();
            }
        }

        private Task _client_LeftGuild(SocketGuild arg)
        {
            _guildReactions.TryRemove(arg.Id, out _);
            return Task.CompletedTask;
        }

        private Task Bot_JoinedGuild(GuildConfig gc)
        {
            var _ = Task.Run(() =>
            {
                using var uow = _db.GetDbContext();
                var crs = uow.CustomReactions.ForId(gc.GuildId)
                    .ToDictionary(x => x.Id, x => x)
                    .ToConcurrent();
                _guildReactions.AddOrUpdate(gc.GuildId, crs, (key, old) => crs);
            });
            return Task.CompletedTask;
        }

        public Task AddGcr(CustomReaction cr)
        {
            return _pubSub.Pub(_gcrAddedKey, cr);
        }

        public Task DelGcr(int id)
        {
            return _pubSub.Pub(_gcrDeletedkey, id);
        }

        public CustomReaction TryGetCustomReaction(IUserMessage umsg)
        {
            var channel = umsg.Channel as SocketTextChannel;
            if (channel == null)
                return null;

            var content = umsg.Content.Trim().ToLowerInvariant();

            if (_guildReactions.TryGetValue(channel.Guild.Id, out var reactions))
            {
                if (reactions != null && reactions.Any())
                {
                    var rs = reactions.Values.Where(cr =>
                    {
                        if (cr == null)
                            return false;

                        var hasTarget = cr.Response.ToLowerInvariant().Contains("%target%");
                        var trigger = cr.TriggerWithContext(umsg, _client).Trim().ToLowerInvariant();
                        return ((cr.ContainsAnywhere &&
                            (content.GetWordPosition(trigger) != WordPosition.None))
                            || (hasTarget && content.StartsWith(trigger + " ", StringComparison.InvariantCulture))
                            || content == trigger);
                    }).ToArray();

                    if (rs.Length != 0)
                    {
                        var reaction = rs[new WizBotRandom().Next(0, rs.Length)];
                        if (reaction != null)
                        {
                            if (reaction.Response == "-")
                                return null;
                            //using (var uow = _db.UnitOfWork)
                            //{
                            //    var rObj = uow.CustomReactions.GetById(reaction.Id);
                            //    if (rObj != null)
                            //    {
                            //        rObj.UseCount += 1;
                            //        uow.Complete();
                            //    }
                            //}
                            return reaction;
                        }
                    }
                }
            }

            var localGrs = _globalReactions;

            var result = new List<CustomReaction>(1);
            for (var i = 0; i < localGrs.Length; i++)
            {
                var cr = localGrs[i];
                var hasTarget = cr.Response.ToLowerInvariant().Contains("%target%");
                var trigger = cr.TriggerWithContext(umsg, _client).Trim().ToLowerInvariant();
                if ((cr.ContainsAnywhere &&
                     (content.GetWordPosition(trigger) != WordPosition.None))
                    || (hasTarget && content.StartsWith(trigger + " ", StringComparison.InvariantCulture))
                    || content == trigger)
                {
                    result.Add(cr);
                }
            }
            
            if (result.Count == 0)
                return null;
            
            var greaction = result[_rng.Next(0, result.Count)];

            return greaction;
        }

        public async Task<bool> RunBehavior(DiscordSocketClient client, IGuild guild, IUserMessage msg)
        {
            // maybe this message is a custom reaction
            var cr = await Task.Run(() => TryGetCustomReaction(msg)).ConfigureAwait(false);
            if (cr != null)
            {
                try
                {
                    if (_gperm.BlockedModules.Contains("ActualCustomReactions"))
                    {
                        return true;
                    }

                    if (guild is SocketGuild sg)
                    {
                        var pc = _perms.GetCacheFor(guild.Id);
                        if (!pc.Permissions.CheckPermissions(msg, cr.Trigger, "ActualCustomReactions",
                            out int index))
                        {
                            if (pc.Verbose)
                            {
                                var returnMsg = _strings.GetText("trigger", sg.Id,
                                    index + 1,
                                    Format.Bold(pc.Permissions[index].GetCommand(_cmd.GetPrefix(guild), sg)));
                                try
                                {
                                    await msg.Channel.SendErrorAsync(returnMsg).ConfigureAwait(false);
                                }
                                catch
                                {
                                }

                                _log.Info(returnMsg);
                            }

                            return true;
                        }
                    }
                    
                    var sentMsg = await cr.Send(msg, _client, false).ConfigureAwait(false);

                    var reactions = cr.GetReactions();
                    foreach(var reaction in reactions)
                    {
                        try
                        {
                            await sentMsg.AddReactionAsync(reaction.ToIEmote());
                        }
                        catch
                        {
                            _log.Warn("Unable to add reactions to message {Message} in server {GuildId}", sentMsg.Id, cr.GuildId);
                            break;
                        }
                        await Task.Delay(1000);
                    }

                    if (cr.AutoDeleteTrigger)
                    {
                        try { await msg.DeleteAsync().ConfigureAwait(false); } catch { }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    _log.Warn(ex.Message);
                }
            }
            return false;
        }

        public async Task ResetCRReactions(ulong? guildId, int id)
        {
            CustomReaction cr;
            using (var uow = _db.GetDbContext())
            {
                cr = uow.CustomReactions.GetById(id);
                if (cr is null)
                    return;

                cr.Reactions = string.Empty;

                await uow.SaveChangesAsync();
            }

            if (guildId is null)
            {
                await PublishEditedGcr(cr).ConfigureAwait(false);
            }
            else
            {
                if (_guildReactions.TryGetValue(guildId.Value, out var crs)
                    && crs.TryGetValue(cr.Id, out var oldCr))
                {
                    oldCr.Reactions = cr.Reactions;
                }
            }
        }

        public async Task SetCrReactions(ulong? guildId, int id, IEnumerable<string> emojis)
        {
            CustomReaction cr;
            using (var uow = _db.GetDbContext())
            {
                cr = uow.CustomReactions.GetById(id);
                if (cr is null)
                    return;

                cr.Reactions = string.Join("@@@", emojis);

                await uow.SaveChangesAsync();
            }

            if (guildId is null)
            {
                await PublishEditedGcr(cr).ConfigureAwait(false);
            }
            else
            {
                if (_guildReactions.TryGetValue(guildId.Value, out var crs)
                    && crs.TryGetValue(cr.Id, out var oldCr))
                {
                    oldCr.Reactions = cr.Reactions;
                }
            }
        }

        public Task TriggerReloadCustomReactions() 
            => _pubSub.Pub(_crsReloadedKey, true);

        public async Task<(bool Sucess, bool NewValue)> ToggleCrOptionAsync(int id, CrField field)
        {
            var newVal = false;
            CustomReaction cr;
            using (var uow = _db.GetDbContext())
            {
                cr = uow.CustomReactions.GetById(id);
                if (cr == null)
                    return (false, false);
                if (field == CrField.AutoDelete)
                    newVal = cr.AutoDeleteTrigger = !cr.AutoDeleteTrigger;
                else if (field == CrField.ContainsAnywhere)
                    newVal = cr.ContainsAnywhere = !cr.ContainsAnywhere;
                else if (field == CrField.DmResponse)
                    newVal = cr.DmResponse = !cr.DmResponse;

                uow.SaveChanges();
            }

            if (cr.GuildId == null)
            {
                await PublishEditedGcr(cr).ConfigureAwait(false);
            }
            else
            {
                if (_guildReactions.TryGetValue(cr.GuildId.Value, out var crs)
                    && crs.TryGetValue(id, out var oldCr))
                {
                    if (oldCr != null)
                    {
                        oldCr.DmResponse = cr.DmResponse;
                        oldCr.ContainsAnywhere = cr.ContainsAnywhere;
                        oldCr.AutoDeleteTrigger = cr.AutoDeleteTrigger;
                    }
                }
            }

            return (true, newVal);
        }

        private Task PublishEditedGcr(CustomReaction cr)
        {
            // only publish global cr changes
            if (cr.GuildId != 0 && cr.GuildId != null)
                return Task.CompletedTask;

            return _pubSub.Pub(_gcrEditedKey, cr);
        }

        public int ClearCustomReactions(ulong id)
        {
            using var uow = _db.GetDbContext();
            var count = uow.CustomReactions.ClearFromGuild(id);
            _guildReactions.TryRemove(id, out _);
            uow.SaveChanges();
            return count;
        }

        public async Task<CustomReaction> AddCustomReaction(ulong? guildId, string key, string message)
        {
            key = key.ToLowerInvariant();
            var cr = new CustomReaction()
            {
                GuildId = guildId,
                IsRegex = false,
                Trigger = key,
                Response = message,
            };

            using (var uow = _db.GetDbContext())
            {
                uow.CustomReactions.Add(cr);

                await uow.SaveChangesAsync();
            }

            if (guildId == null)
            {
                await AddGcr(cr).ConfigureAwait(false);
            }
            else
            {
                var crs = _guildReactions.GetOrAdd(guildId.Value, new ConcurrentDictionary<int, CustomReaction>());
                crs.AddOrUpdate(cr.Id, cr, delegate { return cr; });
            }

            return cr;
        }

        public async Task<CustomReaction> EditCustomReaction(ulong? guildId, int id, string message)
        {
            CustomReaction cr;
            using (var uow = _db.GetDbContext())
            {
                cr = uow.CustomReactions.GetById(id);

                if (cr == null || cr.GuildId != guildId)
                    return null;
                
                cr.Response = message;
                await uow.SaveChangesAsync();
            }
            
            if (guildId == null)
            {
                await PublishEditedGcr(cr).ConfigureAwait(false);
            }
            else
            {
                if (_guildReactions.TryGetValue(guildId.Value, out var crs)
                    && crs.TryGetValue(cr.Id, out var oldCr))
                {
                    oldCr.Response = message;
                }
            }

            return cr;
        }

        public IEnumerable<CustomReaction> GetCustomReactions(ulong? guildId)
        {
            if (guildId == null)
                return _globalReactions.ToList();
            
            return _guildReactions.GetOrAdd(guildId.Value, new ConcurrentDictionary<int, CustomReaction>()).Values;
        }

        public CustomReaction GetCustomReaction(ulong? guildId, int id)
        {
            using var uow = _db.GetDbContext();
            var cr = uow.CustomReactions.GetById(id);
            if (cr == null || cr.GuildId != guildId)
                return null;
                
            return cr;
        }

        public async Task<CustomReaction> DeleteCustomReactionAsync(ulong? guildId, int id)
        {
            var success = false;
            using var uow = _db.GetDbContext();
            var toDelete = uow.CustomReactions.GetById(id);
            if(toDelete != null)
            {
                if ((toDelete.GuildId == null || toDelete.GuildId == 0) && guildId == null)
                {
                    uow.CustomReactions.Remove(toDelete);
                    await DelGcr(toDelete.Id);
                    success = true;
                }
                else if (toDelete.GuildId is ulong gid && toDelete.GuildId != 0 && guildId == toDelete.GuildId)
                {
                    uow.CustomReactions.Remove(toDelete);
                    var grs = _guildReactions.GetOrAdd(gid, new ConcurrentDictionary<int, CustomReaction>());
                    success = grs.TryRemove(toDelete.Id, out _);
                }
                if (success)
                    await uow.SaveChangesAsync();
            }

            return success
                ? toDelete
                : null;
        }

        public bool ReactionExists(ulong? guildId, string input)
        {
            using var uow = _db.GetDbContext();
            var cr = uow.CustomReactions.GetByGuildIdAndInput(guildId, input);
            return cr != null;
        }

        public Task OnReadyAsync()
        {
            return ReloadInternal(_bot.GetCurrentGuildIds());
        }
    }
}