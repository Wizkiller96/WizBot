#nullable disable
using Microsoft.EntityFrameworkCore;
using NadekoBot.Common.Collections;
using NadekoBot.Db;
using NadekoBot.Modules.Gambling.Common;
using NadekoBot.Modules.Gambling.Services;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Gambling;

public partial class Gambling
{
    [Group]
    public partial class ShopCommands : GamblingSubmodule<IShopService>
    {
        public enum List
        {
            List
        }

        public enum Role
        {
            Role
        }

        private readonly DbService _db;
        private readonly ICurrencyService _cs;

        public ShopCommands(DbService db, ICurrencyService cs, GamblingConfigService gamblingConf)
            : base(gamblingConf)
        {
            _db = db;
            _cs = cs;
        }

        private Task ShopInternalAsync(int page = 0)
        {
            if (page < 0)
                throw new ArgumentOutOfRangeException(nameof(page));

            using var uow = _db.GetDbContext();
            var entries = uow.GuildConfigsForId(ctx.Guild.Id,
                                 set => set.Include(x => x.ShopEntries).ThenInclude(x => x.Items))
                             .ShopEntries.ToIndexed();
            return ctx.SendPaginatedConfirmAsync(page,
                curPage =>
                {
                    var theseEntries = entries.Skip(curPage * 9).Take(9).ToArray();

                    if (!theseEntries.Any())
                        return _eb.Create().WithErrorColor().WithDescription(GetText(strs.shop_none));
                    var embed = _eb.Create().WithOkColor().WithTitle(GetText(strs.shop));

                    for (var i = 0; i < theseEntries.Length; i++)
                    {
                        var entry = theseEntries[i];
                        embed.AddField($"#{(curPage * 9) + i + 1} - {N(entry.Price)}",
                            EntryToString(entry),
                            true);
                    }

                    return embed;
                },
                entries.Count,
                9);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public partial Task Shop(int page = 1)
        {
            if (--page < 0)
                return Task.CompletedTask;

            return ShopInternalAsync(page);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async partial Task Buy(int index)
        {
            index -= 1;
            if (index < 0)
                return;
            ShopEntry entry;
            await using (var uow = _db.GetDbContext())
            {
                var config = uow.GuildConfigsForId(ctx.Guild.Id,
                    set => set.Include(x => x.ShopEntries).ThenInclude(x => x.Items));
                var entries = new IndexedCollection<ShopEntry>(config.ShopEntries);
                entry = entries.ElementAtOrDefault(index);
                uow.SaveChanges();
            }

            if (entry is null)
            {
                await ReplyErrorLocalizedAsync(strs.shop_item_not_found);
                return;
            }

            if (entry.Type == ShopEntryType.Role)
            {
                var guser = (IGuildUser)ctx.User;
                var role = ctx.Guild.GetRole(entry.RoleId);

                if (role is null)
                {
                    await ReplyErrorLocalizedAsync(strs.shop_role_not_found);
                    return;
                }

                if (guser.RoleIds.Any(id => id == role.Id))
                {
                    await ReplyErrorLocalizedAsync(strs.shop_role_already_bought);
                    return;
                }

                if (await _cs.RemoveAsync(ctx.User.Id, entry.Price, new("shop", "buy", entry.Type.ToString())))
                {
                    try
                    {
                        await guser.AddRoleAsync(role);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Error adding shop role");
                        await _cs.AddAsync(ctx.User.Id, entry.Price, new("shop", "error-refund"));
                        await ReplyErrorLocalizedAsync(strs.shop_role_purchase_error);
                        return;
                    }

                    var profit = GetProfitAmount(entry.Price);
                    await _cs.AddAsync(entry.AuthorId, profit, new("shop", "sell", $"Shop sell item - {entry.Type}"));
                    await _cs.AddAsync(ctx.Client.CurrentUser.Id, entry.Price - profit, new("shop", "cut"));
                    await ReplyConfirmLocalizedAsync(strs.shop_role_purchase(Format.Bold(role.Name)));
                    return;
                }

                await ReplyErrorLocalizedAsync(strs.not_enough(CurrencySign));
                return;
            }

            if (entry.Type == ShopEntryType.List)
            {
                if (entry.Items.Count == 0)
                {
                    await ReplyErrorLocalizedAsync(strs.out_of_stock);
                    return;
                }

                var item = entry.Items.ToArray()[new NadekoRandom().Next(0, entry.Items.Count)];

                if (await _cs.RemoveAsync(ctx.User.Id, entry.Price, new("shop", "buy", entry.Type.ToString())))
                {
                    await using (var uow = _db.GetDbContext())
                    {
                        uow.Set<ShopEntryItem>().Remove(item);
                        uow.SaveChanges();
                    }

                    try
                    {
                        await ctx.User.EmbedAsync(_eb.Create()
                                                     .WithOkColor()
                                                     .WithTitle(GetText(strs.shop_purchase(ctx.Guild.Name)))
                                                     .AddField(GetText(strs.item), item.Text)
                                                     .AddField(GetText(strs.price), entry.Price.ToString(), true)
                                                     .AddField(GetText(strs.name), entry.Name, true));

                        await _cs.AddAsync(entry.AuthorId,
                            GetProfitAmount(entry.Price),
                            new("shop", "sell", entry.Name));
                    }
                    catch
                    {
                        await _cs.AddAsync(ctx.User.Id, entry.Price, new("shop", "error-refund", entry.Name));
                        await using (var uow = _db.GetDbContext())
                        {
                            var entries = new IndexedCollection<ShopEntry>(uow.GuildConfigsForId(ctx.Guild.Id,
                                                                                  set => set.Include(x => x.ShopEntries)
                                                                                      .ThenInclude(x => x.Items))
                                                                              .ShopEntries);
                            entry = entries.ElementAtOrDefault(index);
                            if (entry is not null)
                            {
                                if (entry.Items.Add(item))
                                    uow.SaveChanges();
                            }
                        }

                        await ReplyErrorLocalizedAsync(strs.shop_buy_error);
                        return;
                    }

                    await ReplyConfirmLocalizedAsync(strs.shop_item_purchase);
                }
                else
                    await ReplyErrorLocalizedAsync(strs.not_enough(CurrencySign));
            }
        }

        private static long GetProfitAmount(int price)
            => (int)Math.Ceiling(0.90 * price);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [BotPerm(GuildPerm.ManageRoles)]
        public async partial Task ShopAdd(Role _, int price, [Leftover] IRole role)
        {
            if (price < 1)
                return;

            var entry = new ShopEntry
            {
                Name = "-",
                Price = price,
                Type = ShopEntryType.Role,
                AuthorId = ctx.User.Id,
                RoleId = role.Id,
                RoleName = role.Name
            };
            await using (var uow = _db.GetDbContext())
            {
                var entries = new IndexedCollection<ShopEntry>(uow.GuildConfigsForId(ctx.Guild.Id,
                                                                      set => set.Include(x => x.ShopEntries)
                                                                          .ThenInclude(x => x.Items))
                                                                  .ShopEntries)
                {
                    entry
                };
                uow.GuildConfigsForId(ctx.Guild.Id, set => set).ShopEntries = entries;
                uow.SaveChanges();
            }

            await ctx.Channel.EmbedAsync(EntryToEmbed(entry).WithTitle(GetText(strs.shop_item_add)));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async partial Task ShopAdd(List _, int price, [Leftover] string name)
        {
            if (price < 1)
                return;

            var entry = new ShopEntry
            {
                Name = name.TrimTo(100),
                Price = price,
                Type = ShopEntryType.List,
                AuthorId = ctx.User.Id,
                Items = new()
            };
            await using (var uow = _db.GetDbContext())
            {
                var entries = new IndexedCollection<ShopEntry>(uow.GuildConfigsForId(ctx.Guild.Id,
                                                                      set => set.Include(x => x.ShopEntries)
                                                                          .ThenInclude(x => x.Items))
                                                                  .ShopEntries)
                {
                    entry
                };
                uow.GuildConfigsForId(ctx.Guild.Id, set => set).ShopEntries = entries;
                uow.SaveChanges();
            }

            await ctx.Channel.EmbedAsync(EntryToEmbed(entry).WithTitle(GetText(strs.shop_item_add)));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async partial Task ShopListAdd(int index, [Leftover] string itemText)
        {
            index -= 1;
            if (index < 0)
                return;
            var item = new ShopEntryItem
            {
                Text = itemText
            };
            ShopEntry entry;
            var rightType = false;
            var added = false;
            await using (var uow = _db.GetDbContext())
            {
                var entries = new IndexedCollection<ShopEntry>(uow.GuildConfigsForId(ctx.Guild.Id,
                                                                      set => set.Include(x => x.ShopEntries)
                                                                          .ThenInclude(x => x.Items))
                                                                  .ShopEntries);
                entry = entries.ElementAtOrDefault(index);
                if (entry is not null && (rightType = entry.Type == ShopEntryType.List))
                {
                    if (entry.Items.Add(item))
                    {
                        added = true;
                        uow.SaveChanges();
                    }
                }
            }

            if (entry is null)
                await ReplyErrorLocalizedAsync(strs.shop_item_not_found);
            else if (!rightType)
                await ReplyErrorLocalizedAsync(strs.shop_item_wrong_type);
            else if (added == false)
                await ReplyErrorLocalizedAsync(strs.shop_list_item_not_unique);
            else
                await ReplyConfirmLocalizedAsync(strs.shop_list_item_added);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async partial Task ShopRemove(int index)
        {
            index -= 1;
            if (index < 0)
                return;
            ShopEntry removed;
            await using (var uow = _db.GetDbContext())
            {
                var config = uow.GuildConfigsForId(ctx.Guild.Id,
                    set => set.Include(x => x.ShopEntries).ThenInclude(x => x.Items));

                var entries = new IndexedCollection<ShopEntry>(config.ShopEntries);
                removed = entries.ElementAtOrDefault(index);
                if (removed is not null)
                {
                    uow.RemoveRange(removed.Items);
                    uow.Remove(removed);
                    uow.SaveChanges();
                }
            }

            if (removed is null)
                await ReplyErrorLocalizedAsync(strs.shop_item_not_found);
            else
                await ctx.Channel.EmbedAsync(EntryToEmbed(removed).WithTitle(GetText(strs.shop_item_rm)));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async partial Task ShopChangePrice(int index, int price)
        {
            if (--index < 0 || price <= 0)
                return;

            var succ = await _service.ChangeEntryPriceAsync(ctx.Guild.Id, index, price);
            if (succ)
            {
                await ShopInternalAsync(index / 9);
                await ctx.OkAsync();
            }
            else
                await ctx.ErrorAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async partial Task ShopChangeName(int index, [Leftover] string newName)
        {
            if (--index < 0 || string.IsNullOrWhiteSpace(newName))
                return;

            var succ = await _service.ChangeEntryNameAsync(ctx.Guild.Id, index, newName);
            if (succ)
            {
                await ShopInternalAsync(index / 9);
                await ctx.OkAsync();
            }
            else
                await ctx.ErrorAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async partial Task ShopSwap(int index1, int index2)
        {
            if (--index1 < 0 || --index2 < 0 || index1 == index2)
                return;

            var succ = await _service.SwapEntriesAsync(ctx.Guild.Id, index1, index2);
            if (succ)
            {
                await ShopInternalAsync(index1 / 9);
                await ctx.OkAsync();
            }
            else
                await ctx.ErrorAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async partial Task ShopMove(int fromIndex, int toIndex)
        {
            if (--fromIndex < 0 || --toIndex < 0 || fromIndex == toIndex)
                return;

            var succ = await _service.MoveEntryAsync(ctx.Guild.Id, fromIndex, toIndex);
            if (succ)
            {
                await ShopInternalAsync(toIndex / 9);
                await ctx.OkAsync();
            }
            else
                await ctx.ErrorAsync();
        }

        public IEmbedBuilder EntryToEmbed(ShopEntry entry)
        {
            var embed = _eb.Create().WithOkColor();

            if (entry.Type == ShopEntryType.Role)
            {
                return embed
                       .AddField(GetText(strs.name),
                           GetText(strs.shop_role(Format.Bold(ctx.Guild.GetRole(entry.RoleId)?.Name
                                                              ?? "MISSING_ROLE"))),
                           true)
                       .AddField(GetText(strs.price), N(entry.Price), true)
                       .AddField(GetText(strs.type), entry.Type.ToString(), true);
            }

            if (entry.Type == ShopEntryType.List)
            {
                return embed.AddField(GetText(strs.name), entry.Name, true)
                            .AddField(GetText(strs.price), N(entry.Price), true)
                            .AddField(GetText(strs.type), GetText(strs.random_unique_item), true);
            }

            //else if (entry.Type == ShopEntryType.Infinite_List)
            //    return embed.AddField(GetText(strs.name), GetText(strs.shop_role(Format.Bold(entry.RoleName)), true))
            //            .AddField(GetText(strs.price), entry.Price.ToString(), true)
            //            .AddField(GetText(strs.type), entry.Type.ToString(), true);
            return null;
        }

        public string EntryToString(ShopEntry entry)
        {
            if (entry.Type == ShopEntryType.Role)
                return GetText(strs.shop_role(Format.Bold(ctx.Guild.GetRole(entry.RoleId)?.Name ?? "MISSING_ROLE")));
            if (entry.Type == ShopEntryType.List)
                return GetText(strs.unique_items_left(entry.Items.Count)) + "\n" + entry.Name;
            return "";
        }
    }
}