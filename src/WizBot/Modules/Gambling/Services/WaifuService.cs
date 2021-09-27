﻿using Discord;
using WizBot.Modules.Gambling.Common.Waifu;
using WizBot.Services;
using WizBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WizBot.Db;
using WizBot.Modules.Gambling.Common;
using WizBot.Db.Models;

namespace WizBot.Modules.Gambling.Services
{
    public class WaifuService : INService
    {
        public class FullWaifuInfo
        {
            public WaifuInfo Waifu { get; set; }
            public IEnumerable<string> Claims { get; set; }
            public int Divorces { get; set; }
        }

        private readonly DbService _db;
        private readonly ICurrencyService _cs;
        private readonly IDataCache _cache;
        private readonly GamblingConfigService _gss;

        public WaifuService(DbService db, ICurrencyService cs, IDataCache cache,
            GamblingConfigService gss)
        {
            _db = db;
            _cs = cs;
            _cache = cache;
            _gss = gss;
        }

        public async Task<bool> WaifuTransfer(IUser owner, ulong waifuId, IUser newOwner)
        {
            if (owner.Id == newOwner.Id || waifuId == newOwner.Id)
                return false;

            var settings = _gss.Data;
            
            using (var uow = _db.GetDbContext())
            {
                var waifu = uow.WaifuInfo.ByWaifuUserId(waifuId);
                var ownerUser = uow.GetOrCreateUser(owner);

                // owner has to be the owner of the waifu
                if (waifu is null || waifu.ClaimerId != ownerUser.Id)
                    return false;
                
                // if waifu likes the person, gotta pay the penalty
                if (waifu.AffinityId == ownerUser.Id)
                {
                    if (!await _cs.RemoveAsync(owner.Id,
                        "Waifu Transfer - affinity penalty", 
                        (int)(waifu.Price * 0.6),
                        true))
                    {
                        // unable to pay 60% penalty
                        return false;
                    }

                    waifu.Price = (int)(waifu.Price * 0.7); // half of 60% = 30% price reduction
                    if (waifu.Price < settings.Waifu.MinPrice)
                        waifu.Price = settings.Waifu.MinPrice;
                }
                else // if not, pay 10% fee
                {
                    if (!await _cs.RemoveAsync(owner.Id, "Waifu Transfer", waifu.Price / 10, gamble: true))
                    {
                        return false;
                    }

                    waifu.Price = (int) (waifu.Price * 0.95); // half of 10% = 5% price reduction
                    if (waifu.Price < settings.Waifu.MinPrice)
                        waifu.Price = settings.Waifu.MinPrice;
                }

                //new claimerId is the id of the new owner
                var newOwnerUser = uow.GetOrCreateUser(newOwner);
                waifu.ClaimerId = newOwnerUser.Id;

                await uow.SaveChangesAsync();
            }

            return true;
        }

        public int GetResetPrice(IUser user)
        {
            var settings = _gss.Data;
            using (var uow = _db.GetDbContext())
            {
                var waifu = uow.WaifuInfo.ByWaifuUserId(user.Id);

                if (waifu is null)
                    return settings.Waifu.MinPrice;

                var divorces = uow.WaifuUpdates.Count(x => x.Old != null &&
                                                                    x.Old.UserId == user.Id &&
                                                                    x.UpdateType == WaifuUpdateType.Claimed &&
                                                                    x.New == null);
                var affs = uow.WaifuUpdates
                    .AsQueryable()
                    .Where(w => w.User.UserId == user.Id && w.UpdateType == WaifuUpdateType.AffinityChanged &&
                                w.New != null)
                    .ToList()
                    .GroupBy(x => x.New)
                    .Count();

                return (int) Math.Ceiling(waifu.Price * 1.25f) +
                       ((divorces + affs + 2) * settings.Waifu.Multipliers.WaifuReset);
            }
        }

        public async Task<bool> TryReset(IUser user)
        {
            using (var uow = _db.GetDbContext())
            {
                var price = GetResetPrice(user);
                if (!await _cs.RemoveAsync(user.Id, "Waifu Reset", price, gamble: true))
                    return false;

                var affs = uow.WaifuUpdates
                    .AsQueryable()
                    .Where(w => w.User.UserId == user.Id
                                && w.UpdateType == WaifuUpdateType.AffinityChanged
                                && w.New != null);

                var divorces = uow.WaifuUpdates
                    .AsQueryable()
                    .Where(x => x.Old != null &&
                                x.Old.UserId == user.Id &&
                                x.UpdateType == WaifuUpdateType.Claimed &&
                                x.New == null);

                //reset changes of heart to 0
                uow.WaifuUpdates.RemoveRange(affs);
                //reset divorces to 0
                uow.WaifuUpdates.RemoveRange(divorces);
                var waifu = uow.WaifuInfo.ByWaifuUserId(user.Id);
                //reset price, remove items
                //remove owner, remove affinity
                waifu.Price = 50;
                waifu.Items.Clear();
                waifu.ClaimerId = null;
                waifu.AffinityId = null;

                //wives stay though

                uow.SaveChanges();
            }

            return true;
        }

        public async Task<(WaifuInfo, bool, WaifuClaimResult)> ClaimWaifuAsync(IUser user, IUser target, int amount)
        {
            var settings = _gss.Data;
            WaifuClaimResult result;
            WaifuInfo w;
            bool isAffinity;
            using (var uow = _db.GetDbContext())
            {
                w = uow.WaifuInfo.ByWaifuUserId(target.Id);
                isAffinity = (w?.Affinity?.UserId == user.Id);
                if (w is null)
                {
                    var claimer = uow.GetOrCreateUser(user);
                    var waifu = uow.GetOrCreateUser(target);
                    if (!await _cs.RemoveAsync(user.Id, "Claimed Waifu", amount, gamble: true))
                    {
                        result = WaifuClaimResult.NotEnoughFunds;
                    }
                    else
                    {
                        uow.WaifuInfo.Add(w = new WaifuInfo()
                        {
                            Waifu = waifu,
                            Claimer = claimer,
                            Affinity = null,
                            Price = amount
                        });
                        uow.WaifuUpdates.Add(new WaifuUpdate()
                        {
                            User = waifu,
                            Old = null,
                            New = claimer,
                            UpdateType = WaifuUpdateType.Claimed
                        });
                        result = WaifuClaimResult.Success;
                    }
                }
                else if (isAffinity && amount > w.Price * settings.Waifu.Multipliers.CrushClaim)
                {
                    if (!await _cs.RemoveAsync(user.Id, "Claimed Waifu", amount, gamble: true))
                    {
                        result = WaifuClaimResult.NotEnoughFunds;
                    }
                    else
                    {
                        var oldClaimer = w.Claimer;
                        w.Claimer = uow.GetOrCreateUser(user);
                        w.Price = amount + (amount / 4);
                        result = WaifuClaimResult.Success;

                        uow.WaifuUpdates.Add(new WaifuUpdate()
                        {
                            User = w.Waifu,
                            Old = oldClaimer,
                            New = w.Claimer,
                            UpdateType = WaifuUpdateType.Claimed
                        });
                    }
                }
                else if (amount >= w.Price * settings.Waifu.Multipliers.NormalClaim) // if no affinity
                {
                    if (!await _cs.RemoveAsync(user.Id, "Claimed Waifu", amount, gamble: true))
                    {
                        result = WaifuClaimResult.NotEnoughFunds;
                    }
                    else
                    {
                        var oldClaimer = w.Claimer;
                        w.Claimer = uow.GetOrCreateUser(user);
                        w.Price = amount;
                        result = WaifuClaimResult.Success;

                        uow.WaifuUpdates.Add(new WaifuUpdate()
                        {
                            User = w.Waifu,
                            Old = oldClaimer,
                            New = w.Claimer,
                            UpdateType = WaifuUpdateType.Claimed
                        });
                    }
                }
                else
                    result = WaifuClaimResult.InsufficientAmount;


                await uow.SaveChangesAsync();
            }

            return (w, isAffinity, result);
        }

        public async Task<(DiscordUser, bool, TimeSpan?)> ChangeAffinityAsync(IUser user, IGuildUser target)
        {
            DiscordUser oldAff = null;
            var success = false;
            TimeSpan? remaining = null;
            using (var uow = _db.GetDbContext())
            {
                var w = uow.WaifuInfo.ByWaifuUserId(user.Id);
                var newAff = target is null ? null : uow.GetOrCreateUser(target);
                if (w?.Affinity?.UserId == target?.Id)
                {
                }
                else if (!_cache.TryAddAffinityCooldown(user.Id, out remaining))
                {
                }
                else if (w is null)
                {
                    var thisUser = uow.GetOrCreateUser(user);
                    uow.WaifuInfo.Add(new WaifuInfo()
                    {
                        Affinity = newAff,
                        Waifu = thisUser,
                        Price = 1,
                        Claimer = null
                    });
                    success = true;

                    uow.WaifuUpdates.Add(new WaifuUpdate()
                    {
                        User = thisUser,
                        Old = null,
                        New = newAff,
                        UpdateType = WaifuUpdateType.AffinityChanged
                    });
                }
                else
                {
                    if (w.Affinity != null)
                        oldAff = w.Affinity;
                    w.Affinity = newAff;
                    success = true;

                    uow.WaifuUpdates.Add(new WaifuUpdate()
                    {
                        User = w.Waifu,
                        Old = oldAff,
                        New = newAff,
                        UpdateType = WaifuUpdateType.AffinityChanged
                    });
                }

                await uow.SaveChangesAsync();
            }

            return (oldAff, success, remaining);
        }

        public IEnumerable<WaifuLbResult> GetTopWaifusAtPage(int page)
        {
            using (var uow = _db.GetDbContext())
            {
                return uow.WaifuInfo.GetTop(9, page * 9);
            }
        }

        public ulong GetWaifuUserId(ulong ownerId, string name)
        {
            using var uow = _db.GetDbContext();
            return uow.WaifuInfo.GetWaifuUserId(ownerId, name);
        }

        public async Task<(WaifuInfo, DivorceResult, long, TimeSpan?)> DivorceWaifuAsync(IUser user, ulong targetId)
        {
            DivorceResult result;
            TimeSpan? remaining = null;
            long amount = 0;
            WaifuInfo w = null;
            using (var uow = _db.GetDbContext())
            {
                w = uow.WaifuInfo.ByWaifuUserId(targetId);
                var now = DateTime.UtcNow;
                if (w?.Claimer is null || w.Claimer.UserId != user.Id)
                    result = DivorceResult.NotYourWife;
                else if (!_cache.TryAddDivorceCooldown(user.Id, out remaining))
                {
                    result = DivorceResult.Cooldown;
                }
                else
                {
                    amount = w.Price / 2;

                    if (w.Affinity?.UserId == user.Id)
                    {
                        await _cs.AddAsync(w.Waifu.UserId, "Waifu Compensation", amount, gamble: true);
                        w.Price = (int) Math.Floor(w.Price * _gss.Data.Waifu.Multipliers.DivorceNewValue);
                        result = DivorceResult.SucessWithPenalty;
                    }
                    else
                    {
                        await _cs.AddAsync(user.Id, "Waifu Refund", amount, gamble: true);

                        result = DivorceResult.Success;
                    }

                    var oldClaimer = w.Claimer;
                    w.Claimer = null;

                    uow.WaifuUpdates.Add(new WaifuUpdate()
                    {
                        User = w.Waifu,
                        Old = oldClaimer,
                        New = null,
                        UpdateType = WaifuUpdateType.Claimed
                    });
                }

                await uow.SaveChangesAsync();
            }

            return (w, result, amount, remaining);
        }

        public async Task<bool> GiftWaifuAsync(IUser from, IUser giftedWaifu, WaifuItemModel itemObj)
        {
            if (!await _cs.RemoveAsync(from, "Bought waifu item", itemObj.Price, gamble: true))
            {
                return false;
            }

            using (var uow = _db.GetDbContext())
            {
                var w = uow.WaifuInfo.ByWaifuUserId(giftedWaifu.Id, 
                    set => set.Include(x => x.Items)
                        .Include(x => x.Claimer));
                if (w is null)
                {
                    uow.WaifuInfo.Add(w = new WaifuInfo()
                    {
                        Affinity = null,
                        Claimer = null,
                        Price = 1,
                        Waifu = uow.GetOrCreateUser(giftedWaifu),
                    });
                }

                w.Items.Add(new WaifuItem()
                {
                    Name = itemObj.Name.ToLowerInvariant(),
                    ItemEmoji = itemObj.ItemEmoji,
                });
                
                if (w.Claimer?.UserId == from.Id)
                {
                    w.Price += (int) (itemObj.Price * _gss.Data.Waifu.Multipliers.GiftEffect);
                }
                else
                {
                    w.Price += itemObj.Price / 2;
                }

                await uow.SaveChangesAsync();
            }

            return true;
        }

        public WaifuInfoStats GetFullWaifuInfoAsync(ulong targetId)
        {
            using (var uow = _db.GetDbContext())
            {
                var wi = uow.GetWaifuInfo(targetId);
                if (wi is null)
                {
                    wi = new WaifuInfoStats
                    {
                        AffinityCount = 0,
                        AffinityName = null,
                        ClaimCount = 0,
                        ClaimerName = null,
                        Claims = new List<string>(),
                        Fans = new List<string>(),
                        DivorceCount = 0,
                        FullName = null,
                        Items = new List<WaifuItem>(),
                        Price = 1
                    };
                }

                return wi;
            }
        }
        public WaifuInfoStats GetFullWaifuInfoAsync(IGuildUser target)
        {
            using (var uow = _db.GetDbContext())
            {
                var du = uow.GetOrCreateUser(target);
                
                return GetFullWaifuInfoAsync(target.Id);
            }
        }

        public string GetClaimTitle(int count)
        {
            ClaimTitle title;
            if (count == 0)
                title = ClaimTitle.Lonely;
            else if (count == 1)
                title = ClaimTitle.Devoted;
            else if (count < 3)
                title = ClaimTitle.Rookie;
            else if (count < 6)
                title = ClaimTitle.Schemer;
            else if (count < 10)
                title = ClaimTitle.Dilettante;
            else if (count < 17)
                title = ClaimTitle.Intermediate;
            else if (count < 25)
                title = ClaimTitle.Seducer;
            else if (count < 35)
                title = ClaimTitle.Expert;
            else if (count < 50)
                title = ClaimTitle.Veteran;
            else if (count < 75)
                title = ClaimTitle.Incubis;
            else if (count < 100)
                title = ClaimTitle.Harem_King;
            else
                title = ClaimTitle.Harem_God;

            return title.ToString().Replace('_', ' ');
        }

        public string GetAffinityTitle(int count)
        {
            AffinityTitle title;
            if (count < 1)
                title = AffinityTitle.Pure;
            else if (count < 2)
                title = AffinityTitle.Faithful;
            else if (count < 4)
                title = AffinityTitle.Playful;
            else if (count < 8)
                title = AffinityTitle.Cheater;
            else if (count < 11)
                title = AffinityTitle.Tainted;
            else if (count < 15)
                title = AffinityTitle.Corrupted;
            else if (count < 20)
                title = AffinityTitle.Lewd;
            else if (count < 25)
                title = AffinityTitle.Sloot;
            else if (count < 35)
                title = AffinityTitle.Depraved;
            else
                title = AffinityTitle.Harlot;

            return title.ToString().Replace('_', ' ');
        }

        public IReadOnlyList<WaifuItemModel> GetWaifuItems()
        {
            var conf = _gss.Data;
            return conf.Waifu.Items
                .Select(x => new WaifuItemModel(x.ItemEmoji, (int)(x.Price * conf.Waifu.Multipliers.AllGiftPrices), x.Name))
                .ToList();
        }
    }
}