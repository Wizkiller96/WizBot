﻿using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using WizBot.Common.ModuleBehaviors;
using WizBot.Core.Services;
using WizBot.Core.Services.Database.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WizBot.Core.Common;

namespace WizBot.Modules.Permissions.Services
{
    public sealed class BlacklistService : IEarlyBehavior, INService
    {
        private readonly DbService _db;
        private readonly IPubSub _pubSub;
        private readonly IBotCredentials _creds;
        private IReadOnlyList<BlacklistEntry> _blacklist;
        public int Priority => -100;

        public ModuleBehaviorType BehaviorType => ModuleBehaviorType.Blocker;

        private readonly TypedKey<BlacklistEntry[]> blPubKey = new TypedKey<BlacklistEntry[]>("blacklist.reload");
        public BlacklistService(DbService db, IPubSub pubSub, IBotCredentials creds)
        {
            _db = db;
            _pubSub = pubSub;
            _creds = creds;

            Reload(false);
            _pubSub.Sub(blPubKey, OnReload);
        }

        private ValueTask OnReload(BlacklistEntry[] blacklist)
        {
            _blacklist = blacklist;
            return default;
        }

        public Task<bool> RunBehavior(DiscordSocketClient _, IGuild guild, IUserMessage usrMsg)
        {
            foreach (var bl in _blacklist)
            {
                if (guild != null && bl.Type == BlacklistType.Server && bl.ItemId == guild.Id)
                    return Task.FromResult(true);

                if (bl.Type == BlacklistType.Channel && bl.ItemId == usrMsg.Channel.Id)
                    return Task.FromResult(true);

                if (bl.Type == BlacklistType.User && bl.ItemId == usrMsg.Author.Id)
                    return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        
        public IReadOnlyList<BlacklistEntry> GetBlacklist()
            => _blacklist;

        public void Reload(bool publish = true)
        {
            using var uow = _db.GetDbContext();
            var toPublish = uow._context.Blacklist.AsNoTracking().ToArray();
            _blacklist = toPublish;
            if (publish)
            {
                _pubSub.Pub(blPubKey, toPublish);
            }
        }

        public void Blacklist(BlacklistType type, ulong id)
        {
            if (_creds.OwnerIds.Contains(id))
                return;

            using var uow = _db.GetDbContext();
            var item = new BlacklistEntry { ItemId = id, Type = type };
            uow._context.Blacklist.Add(item);
            uow.SaveChanges();
            
            Reload(true);
        }
        
        public void UnBlacklist(BlacklistType type, ulong id)
        {
            using var uow = _db.GetDbContext();
            var toRemove = uow._context.Blacklist
                .FirstOrDefault(bi => bi.ItemId == id && bi.Type == type);
            
            if (!(toRemove is null))
                uow._context.Blacklist.Remove(toRemove);
            
            uow.SaveChanges();
            
            Reload(true);
        }
        
        public void BlacklistUsers(IReadOnlyCollection<ulong> toBlacklist)
        {
            using (var uow = _db.GetDbContext()) 
            {
                var bc = uow._context.Blacklist;
                //blacklist the users
                bc.AddRange(toBlacklist.Select(x =>
                    new BlacklistEntry
                    {
                        ItemId = x,
                        Type = BlacklistType.User,
                    }));
                
                //clear their currencies
                uow.DiscordUsers.RemoveFromMany(toBlacklist);
                uow.SaveChanges();
            }
            
            Reload(true);
        }
    }
}