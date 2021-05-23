﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using WizBot.Common;
using WizBot.Common.Attributes;
using WizBot.Common.Collections;
using WizBot.Core.Modules.Gambling.Common;
using WizBot.Core.Modules.Gambling.Services;
using WizBot.Core.Services;
using WizBot.Core.Services.Database.Models;
using WizBot.Extensions;
using Serilog;

namespace WizBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class FlowerShopCommands : GamblingSubmodule<IShopService>
        {
            private readonly DbService _db;
            private readonly ICurrencyService _cs;

            public enum Role
            {
                Role
            }

            public enum List
            {
                List
            }

            public FlowerShopCommands(DbService db, ICurrencyService cs, GamblingConfigService gamblingConf)
                : base(gamblingConf) 
            {
                _db = db;
                _cs = cs;
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public Task ShopInternalAsync(int page = 0)
            {
                if (page < 0)
                    throw new ArgumentOutOfRangeException(nameof(page));
                
                using var uow = _db.GetDbContext();
                var entries = uow.GuildConfigs.ForId(ctx.Guild.Id,
                        set => set.Include(x => x.ShopEntries)
                            .ThenInclude(x => x.Items)).ShopEntries
                        .ToIndexed();
                return ctx.SendPaginatedConfirmAsync(page, (curPage) =>
                {
                    var theseEntries = entries.Skip(curPage * 9).Take(9).ToArray();

                    if (!theseEntries.Any())
                        return new EmbedBuilder().WithErrorColor()
                            .WithDescription(GetText("shop_none"));
                    var embed = new EmbedBuilder().WithOkColor()
                        .WithTitle(GetText("shop", CurrencySign));

                    for (int i = 0; i < theseEntries.Length; i++)
                    {
                        var entry = theseEntries[i];
                        embed.AddField(
                            $"#{curPage * 9 + i + 1} - {entry.Price}{CurrencySign}",
                            EntryToString(entry),
                            true);
                    }
                    return embed;
                }, entries.Count, 9, true);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public Task Shop(int page = 1)
            {
                if (--page < 0)
                    return Task.CompletedTask;
                
                return ShopInternalAsync(page);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Buy(int index)
            {
                index -= 1;
                if (index < 0)
                    return;
                ShopEntry entry;
                using (var uow = _db.GetDbContext())
                {
                    var config = uow.GuildConfigs.ForId(ctx.Guild.Id, set => set
                        .Include(x => x.ShopEntries)
                        .ThenInclude(x => x.Items));
                    var entries = new IndexedCollection<ShopEntry>(config.ShopEntries);
                    entry = entries.ElementAtOrDefault(index);
                    uow.SaveChanges();
                }

                if (entry == null)
                {
                    await ReplyErrorLocalizedAsync("shop_item_not_found").ConfigureAwait(false);
                    return;
                }

                if (entry.Type == ShopEntryType.Role)
                {
                    var guser = (IGuildUser)ctx.User;
                    var role = ctx.Guild.GetRole(entry.RoleId);

                    if (role == null)
                    {
                        await ReplyErrorLocalizedAsync("shop_role_not_found").ConfigureAwait(false);
                        return;
                    }
                    
                    if (guser.RoleIds.Any(id => id == role.Id))
                    {
                        await ReplyErrorLocalizedAsync("shop_role_already_bought").ConfigureAwait(false);
                        return;
                    }

                    if (await _cs.RemoveAsync(ctx.User.Id, $"Shop purchase - {entry.Type}", entry.Price).ConfigureAwait(false))
                    {
                        try
                        {
                            await guser.AddRoleAsync(role).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Log.Warning(ex, "Error adding shop role");
                            await _cs.AddAsync(ctx.User.Id, $"Shop error refund", entry.Price).ConfigureAwait(false);
                            await ReplyErrorLocalizedAsync("shop_role_purchase_error").ConfigureAwait(false);
                            return;
                        }
                        var profit = GetProfitAmount(entry.Price);
                        await _cs.AddAsync(entry.AuthorId, $"Shop sell item - {entry.Type}", profit).ConfigureAwait(false);
                        await _cs.AddAsync(ctx.Client.CurrentUser.Id, $"Shop sell item - cut", entry.Price - profit).ConfigureAwait(false);
                        await ReplyConfirmLocalizedAsync("shop_role_purchase", Format.Bold(role.Name)).ConfigureAwait(false);
                        return;
                    }
                    else
                    {
                        await ReplyErrorLocalizedAsync("not_enough", CurrencySign).ConfigureAwait(false);
                        return;
                    }
                }
                else if (entry.Type == ShopEntryType.List)
                {
                    if (entry.Items.Count == 0)
                    {
                        await ReplyErrorLocalizedAsync("out_of_stock").ConfigureAwait(false);
                        return;
                    }

                    var item = entry.Items.ToArray()[new WizBotRandom().Next(0, entry.Items.Count)];

                    if (await _cs.RemoveAsync(ctx.User.Id, $"Shop purchase - {entry.Type}", entry.Price).ConfigureAwait(false))
                    {
                        using (var uow = _db.GetDbContext())
                        {
                            var x = uow._context.Set<ShopEntryItem>().Remove(item);
                            uow.SaveChanges();
                        }
                        try
                        {
                            await (await ctx.User.GetOrCreateDMChannelAsync().ConfigureAwait(false))
                                .EmbedAsync(new EmbedBuilder().WithOkColor()
                                .WithTitle(GetText("shop_purchase", ctx.Guild.Name))
                                .AddField(efb => efb.WithName(GetText("item")).WithValue(item.Text).WithIsInline(false))
                                .AddField(efb => efb.WithName(GetText("price")).WithValue(entry.Price.ToString()).WithIsInline(true))
                                .AddField(efb => efb.WithName(GetText("name")).WithValue(entry.Name).WithIsInline(true)))
                                .ConfigureAwait(false);

                            await _cs.AddAsync(entry.AuthorId,
                                    $"Shop sell item - {entry.Name}",
                                    GetProfitAmount(entry.Price)).ConfigureAwait(false);
                        }
                        catch
                        {
                            await _cs.AddAsync(ctx.User.Id,
                                $"Shop error refund - {entry.Name}",
                                entry.Price).ConfigureAwait(false);
                            using (var uow = _db.GetDbContext())
                            {
                                var entries = new IndexedCollection<ShopEntry>(uow.GuildConfigs.ForId(ctx.Guild.Id,
                                    set => set.Include(x => x.ShopEntries)
                                              .ThenInclude(x => x.Items)).ShopEntries);
                                entry = entries.ElementAtOrDefault(index);
                                if (entry != null)
                                {
                                    if (entry.Items.Add(item))
                                    {
                                        uow.SaveChanges();
                                    }
                                }
                            }
                            await ReplyErrorLocalizedAsync("shop_buy_error").ConfigureAwait(false);
                            return;
                        }
                        await ReplyConfirmLocalizedAsync("shop_item_purchase").ConfigureAwait(false);
                    }
                    else
                    {
                        await ReplyErrorLocalizedAsync("not_enough", CurrencySign).ConfigureAwait(false);
                        return;
                    }
                }

            }

            private static long GetProfitAmount(int price) =>
                (int)(Math.Ceiling(0.90 * price));

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            [BotPerm(GuildPerm.ManageRoles)]
            public async Task ShopAdd(Role _, int price, [Leftover] IRole role)
            {
                var entry = new ShopEntry()
                {
                    Name = "-",
                    Price = price,
                    Type = ShopEntryType.Role,
                    AuthorId = ctx.User.Id,
                    RoleId = role.Id,
                    RoleName = role.Name
                };
                using (var uow = _db.GetDbContext())
                {
                    var entries = new IndexedCollection<ShopEntry>(uow.GuildConfigs.ForId(ctx.Guild.Id,
                        set => set.Include(x => x.ShopEntries)
                                  .ThenInclude(x => x.Items)).ShopEntries)
                    {
                        entry
                    };
                    uow.GuildConfigs.ForId(ctx.Guild.Id, set => set).ShopEntries = entries;
                    uow.SaveChanges();
                }
                await ctx.Channel.EmbedAsync(EntryToEmbed(entry)
                    .WithTitle(GetText("shop_item_add"))).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            public async Task ShopAdd(List _, int price, [Leftover]string name)
            {
                var entry = new ShopEntry()
                {
                    Name = name.TrimTo(100),
                    Price = price,
                    Type = ShopEntryType.List,
                    AuthorId = ctx.User.Id,
                    Items = new HashSet<ShopEntryItem>(),
                };
                using (var uow = _db.GetDbContext())
                {
                    var entries = new IndexedCollection<ShopEntry>(uow.GuildConfigs.ForId(ctx.Guild.Id,
                        set => set.Include(x => x.ShopEntries)
                                  .ThenInclude(x => x.Items)).ShopEntries)
                    {
                        entry
                    };
                    uow.GuildConfigs.ForId(ctx.Guild.Id, set => set).ShopEntries = entries;
                    uow.SaveChanges();
                }
                await ctx.Channel.EmbedAsync(EntryToEmbed(entry)
                    .WithTitle(GetText("shop_item_add"))).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            public async Task ShopListAdd(int index, [Leftover] string itemText)
            {
                index -= 1;
                if (index < 0)
                    return;
                var item = new ShopEntryItem()
                {
                    Text = itemText
                };
                ShopEntry entry;
                bool rightType = false;
                bool added = false;
                using (var uow = _db.GetDbContext())
                {
                    var entries = new IndexedCollection<ShopEntry>(uow.GuildConfigs.ForId(ctx.Guild.Id,
                        set => set.Include(x => x.ShopEntries)
                                  .ThenInclude(x => x.Items)).ShopEntries);
                    entry = entries.ElementAtOrDefault(index);
                    if (entry != null && (rightType = (entry.Type == ShopEntryType.List)))
                    {
                        if (added = entry.Items.Add(item))
                        {
                            uow.SaveChanges();
                        }
                    }
                }
                if (entry == null)
                    await ReplyErrorLocalizedAsync("shop_item_not_found").ConfigureAwait(false);
                else if (!rightType)
                    await ReplyErrorLocalizedAsync("shop_item_wrong_type").ConfigureAwait(false);
                else if (added == false)
                    await ReplyErrorLocalizedAsync("shop_list_item_not_unique").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalizedAsync("shop_list_item_added").ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            public async Task ShopRemove(int index)
            {
                index -= 1;
                if (index < 0)
                    return;
                ShopEntry removed;
                using (var uow = _db.GetDbContext())
                {
                    var config = uow.GuildConfigs.ForId(ctx.Guild.Id, set => set
                        .Include(x => x.ShopEntries)
                        .ThenInclude(x => x.Items));

                    var entries = new IndexedCollection<ShopEntry>(config.ShopEntries);
                    removed = entries.ElementAtOrDefault(index);
                    if (removed != null)
                    {
                        uow._context.RemoveRange(removed.Items);
                        uow._context.Remove(removed);
                        uow.SaveChanges();
                    }
                }

                if (removed == null)
                    await ReplyErrorLocalizedAsync("shop_item_not_found").ConfigureAwait(false);
                else
                    await ctx.Channel.EmbedAsync(EntryToEmbed(removed)
                        .WithTitle(GetText("shop_item_rm"))).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            public async Task ShopChangePrice(int index, int price)
            {
                if (--index < 0 || price <= 0)
                    return;

                var succ = await _service.ChangeEntryPriceAsync(Context.Guild.Id, index, price);
                if (succ)
                {
                    await ShopInternalAsync(index / 9);
                    await ctx.OkAsync();
                }
                else
                {
                    await ctx.ErrorAsync();
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            public async Task ShopChangeName(int index, [Leftover] string newName)
            {
                if (--index < 0 || string.IsNullOrWhiteSpace(newName))
                    return;
                
                var succ = await _service.ChangeEntryNameAsync(Context.Guild.Id, index, newName);
                if (succ)
                {
                    await ShopInternalAsync(index / 9);
                    await ctx.OkAsync();
                }
                else
                {
                    await ctx.ErrorAsync();
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            public async Task ShopSwap(int index1, int index2)
            {
                if (--index1 < 0 || --index2 < 0 || index1 == index2)
                    return;
                
                var succ = await _service.SwapEntriesAsync(Context.Guild.Id, index1, index2);
                if (succ)
                {
                    await ShopInternalAsync(index1 / 9);
                    await ctx.OkAsync();
                }
                else
                {
                    await ctx.ErrorAsync();
                }
            }
            
            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            public async Task ShopMove(int fromIndex, int toIndex)
            {
                if (--fromIndex < 0 || --toIndex < 0 || fromIndex == toIndex)
                    return;

                var succ = await _service.MoveEntryAsync(Context.Guild.Id, fromIndex, toIndex);
                if (succ)
                {
                    await ShopInternalAsync(toIndex / 9);
                    await ctx.OkAsync();
                }
                else
                {
                    await ctx.ErrorAsync();
                }
            }
            
            public EmbedBuilder EntryToEmbed(ShopEntry entry)
            {
                var embed = new EmbedBuilder().WithOkColor();

                if (entry.Type == ShopEntryType.Role)
                    return embed.AddField(efb => efb.WithName(GetText("name")).WithValue(GetText("shop_role", Format.Bold(ctx.Guild.GetRole(entry.RoleId)?.Name ?? "MISSING_ROLE"))).WithIsInline(true))
                            .AddField(efb => efb.WithName(GetText("price")).WithValue(entry.Price.ToString()).WithIsInline(true))
                            .AddField(efb => efb.WithName(GetText("type")).WithValue(entry.Type.ToString()).WithIsInline(true));
                else if (entry.Type == ShopEntryType.List)
                    return embed.AddField(efb => efb.WithName(GetText("name")).WithValue(entry.Name).WithIsInline(true))
                            .AddField(efb => efb.WithName(GetText("price")).WithValue(entry.Price.ToString()).WithIsInline(true))
                            .AddField(efb => efb.WithName(GetText("type")).WithValue(GetText("random_unique_item")).WithIsInline(true));
                //else if (entry.Type == ShopEntryType.Infinite_List)
                //    return embed.AddField(efb => efb.WithName(GetText("name")).WithValue(GetText("shop_role", Format.Bold(entry.RoleName))).WithIsInline(true))
                //            .AddField(efb => efb.WithName(GetText("price")).WithValue(entry.Price.ToString()).WithIsInline(true))
                //            .AddField(efb => efb.WithName(GetText("type")).WithValue(entry.Type.ToString()).WithIsInline(true));
                else return null;
            }

            public string EntryToString(ShopEntry entry)
            {
                if (entry.Type == ShopEntryType.Role)
                {
                    return GetText("shop_role", Format.Bold(ctx.Guild.GetRole(entry.RoleId)?.Name ?? "MISSING_ROLE"));
                }
                else if (entry.Type == ShopEntryType.List)
                {
                    return GetText("unique_items_left", entry.Items.Count) + "\n" + entry.Name;
                }
                //else if (entry.Type == ShopEntryType.Infinite_List)
                //{

                //}
                return "";
            }
        }
    }
}