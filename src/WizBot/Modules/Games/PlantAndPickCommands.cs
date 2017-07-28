using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using WizBot.Extensions;
using WizBot.Services;
using WizBot.Services.Database.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WizBot.Common.Attributes;
using WizBot.Modules.Games.Services;

namespace WizBot.Modules.Games
{
    public partial class Games
    {
        /// <summary>
        /// Flower picking/planting idea is given to me by its
        /// inceptor Violent Crumble from Game Developers League discord server
        /// (he has !cookie and !nom) Thanks a lot Violent!
        /// Check out GDL (its a growing gamedev community):
        /// https://discord.gg/0TYNJfCU4De7YIk8
        /// </summary>
        [Group]
        public class PlantPickCommands : WizBotSubmodule
        {
            private readonly CurrencyService _cs;
            private readonly IBotConfigProvider _bc;
            private readonly GamesService _games;
            private readonly DbService _db;

            public PlantPickCommands(IBotConfigProvider bc, CurrencyService cs, GamesService games,
                DbService db)
            {
                _bc = bc;
                _cs = cs;
                _games = games;
                _db = db;
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Pick()
            {
                var channel = (ITextChannel)Context.Channel;

                if (!(await channel.Guild.GetCurrentUserAsync()).GetPermissions(channel).ManageMessages)
                    return;


                try { await Context.Message.DeleteAsync().ConfigureAwait(false); } catch { }
                if (!_games.PlantedFlowers.TryRemove(channel.Id, out List<IUserMessage> msgs))
                    return;

                await Task.WhenAll(msgs.Where(m => m != null).Select(toDelete => toDelete.DeleteAsync())).ConfigureAwait(false);

                await _cs.AddAsync((IGuildUser)Context.User, $"Picked {_bc.BotConfig.CurrencyPluralName}", msgs.Count, false).ConfigureAwait(false);
                var msg = await ReplyConfirmLocalized("picked", msgs.Count + _bc.BotConfig.CurrencySign)
                    .ConfigureAwait(false);
                msg.DeleteAfter(10);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Plant(int amount = 1)
            {
                if (amount < 1)
                    return;

                var removed = await _cs.RemoveAsync((IGuildUser)Context.User, $"Planted a {_bc.BotConfig.CurrencyName}", amount, false).ConfigureAwait(false);
                if (!removed)
                {
                    await ReplyErrorLocalized("not_enough", _bc.BotConfig.CurrencySign).ConfigureAwait(false);
                    return;
                }

                var imgData = _games.GetRandomCurrencyImage();

                var msgToSend = GetText("planted",
                    Format.Bold(Context.User.ToString()),
                    amount + _bc.BotConfig.CurrencySign,
                    Prefix);

                if (amount > 1)
                    msgToSend += " " + GetText("pick_pl", Prefix);
                else
                    msgToSend += " " + GetText("pick_sn", Prefix);

                IUserMessage msg;
                using (var toSend = imgData.Data.ToStream())
                {
                    msg = await Context.Channel.SendFileAsync(toSend, imgData.Name, msgToSend).ConfigureAwait(false);
                }

                var msgs = new IUserMessage[amount];
                msgs[0] = msg;

                _games.PlantedFlowers.AddOrUpdate(Context.Channel.Id, msgs.ToList(), (id, old) =>
                {
                    old.AddRange(msgs);
                    return old;
                });
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
#if GLOBAL_WIZBOT
            [OwnerOnly]
#endif
            public async Task GenCurrency()
            {
                var channel = (ITextChannel)Context.Channel;

                bool enabled;
                using (var uow = _db.UnitOfWork)
                {
                    var guildConfig = uow.GuildConfigs.For(channel.Guild.Id, set => set.Include(gc => gc.GenerateCurrencyChannelIds));

                    var toAdd = new GCChannelId() { ChannelId = channel.Id };
                    if (!guildConfig.GenerateCurrencyChannelIds.Contains(toAdd))
                    {
                        guildConfig.GenerateCurrencyChannelIds.Add(toAdd);
                        _games.GenerationChannels.Add(channel.Id);
                        enabled = true;
                    }
                    else
                    {
                        guildConfig.GenerateCurrencyChannelIds.Remove(toAdd);
                        _games.GenerationChannels.TryRemove(channel.Id);
                        enabled = false;
                    }
                    await uow.CompleteAsync();
                }
                if (enabled)
                {
                    await ReplyConfirmLocalized("curgen_enabled").ConfigureAwait(false);
                }
                else
                {
                    await ReplyConfirmLocalized("curgen_disabled").ConfigureAwait(false);
                }
            }
        }
    }
}