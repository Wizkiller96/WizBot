using Discord;
using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Core.Modules.Gambling.Common.Waifu;
using NadekoBot.Extensions;
using NadekoBot.Modules.Gambling.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Internal;
using NadekoBot.Core.Modules.Gambling.Common;
using NadekoBot.Core.Modules.Gambling.Services;
using NadekoBot.Core.Services.Database.Models;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class WaifuClaimCommands : GamblingSubmodule<WaifuService>
        {

            public WaifuClaimCommands(GamblingConfigService gamblingConfService) : base(gamblingConfService)
            {
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task WaifuReset()
            {
                var price = _service.GetResetPrice(ctx.User);
                var embed = new EmbedBuilder()
                        .WithTitle(GetText("waifu_reset_confirm"))
                        .WithDescription(GetText("waifu_reset_price", Format.Bold(price + CurrencySign)));

                if (!await PromptUserConfirmAsync(embed))
                    return;

                if (await _service.TryReset(ctx.User))
                {
                    await ReplyConfirmLocalizedAsync("waifu_reset");
                    return;
                }
                await ReplyErrorLocalizedAsync("waifu_reset_fail");
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task WaifuClaim(int amount, [Leftover]IUser target)
            {
                if (amount < _config.Waifu.MinPrice)
                {
                    await ReplyErrorLocalizedAsync("waifu_isnt_cheap", _config.Waifu.MinPrice + CurrencySign);
                    return;
                }

                if (target.Id == ctx.User.Id)
                {
                    await ReplyErrorLocalizedAsync("waifu_not_yourself");
                    return;
                }

                var (w, isAffinity, result) = await _service.ClaimWaifuAsync(ctx.User, target, amount);

                if (result == WaifuClaimResult.InsufficientAmount)
                {
                    await ReplyErrorLocalizedAsync("waifu_not_enough", Math.Ceiling(w.Price * (isAffinity ? 0.88f : 1.1f)));
                    return;
                }
                if (result == WaifuClaimResult.NotEnoughFunds)
                {
                    await ReplyErrorLocalizedAsync("not_enough", CurrencySign);
                    return;
                }
                var msg = GetText("waifu_claimed",
                    Format.Bold(target.ToString()),
                    amount + CurrencySign);
                if (w.Affinity?.UserId == ctx.User.Id)
                    msg += "\n" + GetText("waifu_fulfilled", target, w.Price + CurrencySign);
                else
                    msg = " " + msg;
                await ctx.Channel.SendConfirmAsync(ctx.User.Mention + msg);
            }
            
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(0)]
            public async Task WaifuTransfer(ulong waifuId, IUser newOwner)
            {
                if (!await _service.WaifuTransfer(ctx.User, waifuId, newOwner)
                )
                {
                    await ReplyErrorLocalizedAsync("waifu_transfer_fail");
                    return;
                }

                await ReplyConfirmLocalizedAsync("waifu_transfer_success",
                    Format.Bold(waifuId.ToString()),
                    Format.Bold(ctx.User.ToString()),
                    Format.Bold(newOwner.ToString()));
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(1)]
            public async Task WaifuTransfer(IUser waifu, IUser newOwner)
            {
                if (!await _service.WaifuTransfer(ctx.User, waifu.Id, newOwner))
                {
                    await ReplyErrorLocalizedAsync("waifu_transfer_fail");
                    return;
                }

                await ReplyConfirmLocalizedAsync("waifu_transfer_success",
                    Format.Bold(waifu.ToString()),
                    Format.Bold(ctx.User.ToString()),
                    Format.Bold(newOwner.ToString()));
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(-1)]
            public Task Divorce([Leftover] string target)
            {
                var waifuUserId = _service.GetWaifuUserId(ctx.User.Id, target);
                if (waifuUserId == default)
                {
                    return ReplyErrorLocalizedAsync("waifu_not_yours");
                }

                return Divorce(waifuUserId);
            }
            
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(0)]
            public Task Divorce([Leftover]IGuildUser target) 
                => Divorce(target.Id);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(1)]
            public async Task Divorce([Leftover]ulong targetId)
            {
                if (targetId == ctx.User.Id)
                    return;

                var (w, result, amount, remaining) = await _service.DivorceWaifuAsync(ctx.User, targetId);

                if (result == DivorceResult.SucessWithPenalty)
                {
                    await ReplyConfirmLocalizedAsync("waifu_divorced_like", Format.Bold(w.Waifu.ToString()), amount + CurrencySign);
                }
                else if (result == DivorceResult.Success)
                {
                    await ReplyConfirmLocalizedAsync("waifu_divorced_notlike", amount + CurrencySign);
                }
                else if (result == DivorceResult.NotYourWife)
                {
                    await ReplyErrorLocalizedAsync("waifu_not_yours");
                }
                else
                {
                    await ReplyErrorLocalizedAsync("waifu_recent_divorce",
                        Format.Bold(((int)remaining?.TotalHours).ToString()),
                        Format.Bold(remaining?.Minutes.ToString()));
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Affinity([Leftover]IGuildUser u = null)
            {
                if (u?.Id == ctx.User.Id)
                {
                    await ReplyErrorLocalizedAsync("waifu_egomaniac");
                    return;
                }
                var (oldAff, sucess, remaining) = await _service.ChangeAffinityAsync(ctx.User, u);
                if (!sucess)
                {
                    if (remaining != null)
                    {
                        await ReplyErrorLocalizedAsync("waifu_affinity_cooldown",
                            Format.Bold(((int)remaining?.TotalHours).ToString()),
                            Format.Bold(remaining?.Minutes.ToString()));
                    }
                    else
                    {
                        await ReplyErrorLocalizedAsync("waifu_affinity_already");
                    }
                    return;
                }
                if (u == null)
                {
                    await ReplyConfirmLocalizedAsync("waifu_affinity_reset");
                }
                else if (oldAff == null)
                {
                    await ReplyConfirmLocalizedAsync("waifu_affinity_set", Format.Bold(u.ToString()));
                }
                else
                {
                    await ReplyConfirmLocalizedAsync("waifu_affinity_changed", Format.Bold(oldAff.ToString()), Format.Bold(u.ToString()));
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task WaifuLb(int page = 1)
            {
                page--;

                if (page < 0)
                    return;

                if (page > 100)
                    page = 100;

                var waifus = _service.GetTopWaifusAtPage(page);

                if (waifus.Count() == 0)
                {
                    await ReplyConfirmLocalizedAsync("waifus_none");
                    return;
                }

                var embed = new EmbedBuilder()
                    .WithTitle(GetText("waifus_top_waifus"))
                    .WithOkColor();

                var i = 0;
                foreach (var w in waifus)
                {
                    var j = i++;
                    embed.AddField(efb => efb.WithName("#" + ((page * 9) + j + 1) + " - " + w.Price + CurrencySign).WithValue(w.ToString()).WithIsInline(false));
                }

                await ctx.Channel.EmbedAsync(embed);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(1)]
            public Task WaifuInfo([Leftover]IUser target = null)
            {
                if (target == null)
                    target = ctx.User;

                return InternalWaifuInfo(target.Id, target.ToString());
            }
            
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(0)]
            public Task WaifuInfo(ulong targetId)
                => InternalWaifuInfo(targetId);

            private Task InternalWaifuInfo(ulong targetId, string name = null)
            {
                var wi = _service.GetFullWaifuInfoAsync(targetId);
                var affInfo = _service.GetAffinityTitle(wi.AffinityCount);

                var waifuItems = _service.GetWaifuItems()
                    .ToDictionary(x => x.ItemEmoji, x => x);
                
                
                var nobody = GetText("nobody");
                var itemsStr = !wi.Items.Any()
                    ? "-"
                    : string.Join("\n", wi.Items
                        .Where(x => waifuItems.TryGetValue(x.ItemEmoji, out _))
                        .OrderBy(x => waifuItems[x.ItemEmoji].Price)
                        .GroupBy(x => x.ItemEmoji)
                        .Select(x => $"{x.Key} x{x.Count(),-3}")
                        .Chunk(2)
                        .Select(x => string.Join(" ", x)));

                var fansStr = wi
                    .Fans
                    .Shuffle()
                    .Take(30)
                    .Select(x => wi.Claims.Contains(x) ? $"{x} 💞" : x)
                    .JoinWith('\n');
                
                if (string.IsNullOrWhiteSpace(fansStr))
                    fansStr = "-";

                var embed = new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle(GetText("waifu") + " " + (wi.FullName ?? name ?? targetId.ToString()) + " - \"the " +
                               _service.GetClaimTitle(wi.ClaimCount) + "\"")
                    .AddField(GetText("price"), wi.Price.ToString(), true)
                    .AddField(GetText("claimed_by"), wi.ClaimerName ?? nobody, true)
                    .AddField(GetText("likes"), wi.AffinityName ?? nobody, true)
                    .AddField(GetText("changes_of_heart"), $"{wi.AffinityCount} - \"the {affInfo}\"", true)
                    .AddField(GetText("divorces"), wi.DivorceCount.ToString(), true)
                    .AddField("\u200B", "\u200B", true)
                    .AddField(GetText("fans", wi.Fans.Count), fansStr, true)
                    .AddField($"Waifus ({wi.ClaimCount})", wi.ClaimCount == 0 
                        ? nobody 
                        : string.Join("\n", wi.Claims.Shuffle().Take(30)), true)
                    .AddField(GetText("gifts"), itemsStr, true);

                return ctx.Channel.EmbedAsync(embed);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(1)]
            public async Task WaifuGift(int page = 1)
            {
                if (--page < 0 || page > 3)
                    return;

                var waifuItems = _service.GetWaifuItems();
                await ctx.SendPaginatedConfirmAsync(page, (cur) =>
                {
                    var embed = new EmbedBuilder()
                        .WithTitle(GetText("waifu_gift_shop"))
                        .WithOkColor();

                    waifuItems
                        .OrderBy(x => x.Price)
                        .Skip(9 * cur)
                        .Take(9)
                        .ForEach(x => embed.AddField($"{x.ItemEmoji} {x.Name}", x.Price, true));

                    return embed;
                }, waifuItems.Count, 9);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(0)]
            public async Task WaifuGift(string itemName, [Leftover] IUser waifu)
            {
                if (waifu.Id == ctx.User.Id)
                    return;

                var allItems = _service.GetWaifuItems();
                var item = allItems.FirstOrDefault(x => x.Name.ToLowerInvariant() == itemName.ToLowerInvariant());
                if (item is null)
                {
                    await ReplyErrorLocalizedAsync("waifu_gift_not_exist");
                    return;
                }
                var sucess = await _service.GiftWaifuAsync(ctx.User, waifu, item);

                if (sucess)
                {
                    await ReplyConfirmLocalizedAsync("waifu_gift", 
                        Format.Bold(item.ToString() + " " + item.ItemEmoji),
                        Format.Bold(waifu.ToString()));
                }
                else
                {
                    await ReplyErrorLocalizedAsync("not_enough", CurrencySign);
                }
            }
        }
    }
}