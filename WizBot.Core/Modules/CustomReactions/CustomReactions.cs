﻿using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using WizBot.Core.Services;
using Discord;
using WizBot.Extensions;
using Discord.WebSocket;
using WizBot.Common.Attributes;
using WizBot.Modules.CustomReactions.Services;

namespace WizBot.Modules.CustomReactions
{
    public class CustomReactions : WizBotTopLevelModule<CustomReactionsService>
    {
        private readonly IBotCredentials _creds;
        private readonly DbService _db;
        private readonly DiscordSocketClient _client;

        public CustomReactions(IBotCredentials creds, DbService db,
            DiscordSocketClient client)
        {
            _creds = creds;
            _db = db;
            _client = client;
        }

        private bool AdminInGuildOrOwnerInDm() => (Context.Guild == null && _creds.IsOwner(Context.User))
                || (Context.Guild != null && ((IGuildUser)Context.User).GuildPermissions.Administrator);

        [WizBotCommand, Usage, Description, Aliases]
        public async Task AddCustReact(string key, [Remainder] string message)
        {
            var channel = Context.Channel as ITextChannel;
            if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(key))
                return;

            if (!AdminInGuildOrOwnerInDm())
            {
                await ReplyErrorLocalized("insuff_perms").ConfigureAwait(false);
                return;
            }

            var cr = await _service.AddCustomReaction(Context.Guild?.Id, key, message);

            await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                .WithTitle(GetText("new_cust_react"))
                .WithDescription($"#{cr.Id}")
                .AddField(efb => efb.WithName(GetText("trigger")).WithValue(key))
                .AddField(efb => efb.WithName(GetText("response")).WithValue(message.Length > 1024 ? GetText("redacted_too_long") : message))
                ).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        public async Task EditCustReact(int id, [Remainder] string message)
        {
            var channel = Context.Channel as ITextChannel;
            if (string.IsNullOrWhiteSpace(message) || id < 0)
                return;

            if ((channel == null && !_creds.IsOwner(Context.User)) || (channel != null && !((IGuildUser)Context.User).GuildPermissions.Administrator))
            {
                await ReplyErrorLocalized("insuff_perms").ConfigureAwait(false);
                return;
            }

            var cr = await _service.EditCustomReaction(Context.Guild?.Id, id, message).ConfigureAwait(false);
            if (cr != null)
            {
                await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithTitle(GetText("edited_cust_react"))
                    .WithDescription($"#{id}")
                    .AddField(efb => efb.WithName(GetText("trigger")).WithValue(cr.Trigger))
                    .AddField(efb => efb.WithName(GetText("response")).WithValue(message.Length > 1024 ? GetText("redacted_too_long") : message))
                    ).ConfigureAwait(false);
            }
            else
            {
                await ReplyErrorLocalized("edit_fail").ConfigureAwait(false);
            }
        }

        [WizBotCommand, Usage, Description, Aliases]
        [Priority(1)]
        public async Task ListCustReact(int page = 1)
        {
            if (--page < 0 || page > 999)
                return;

            var customReactions = _service.GetCustomReactions(Context.Guild?.Id);

            if (customReactions == null || !customReactions.Any())
            {
                await ReplyErrorLocalized("no_found").ConfigureAwait(false);
                return;
            }

            await Context.SendPaginatedConfirmAsync(page, curPage =>
                new EmbedBuilder().WithOkColor()
                    .WithTitle(GetText("name"))
                    .WithDescription(string.Join("\n", customReactions.OrderBy(cr => cr.Trigger)
                                                    .Skip(curPage * 20)
                                                    .Take(20)
                                                    .Select(cr =>
                                                    {
                                                        var str = $"`#{cr.Id}` {cr.Trigger}";
                                                        if (cr.AutoDeleteTrigger)
                                                        {
                                                            str = "🗑" + str;
                                                        }
                                                        if (cr.DmResponse)
                                                        {
                                                            str = "📪" + str;
                                                        }
                                                        return str;
                                                    }))), customReactions.Count(), 20)
                                .ConfigureAwait(false);
        }

        public enum All
        {
            All
        }

        [WizBotCommand, Usage, Description, Aliases]
        [Priority(0)]
        public async Task ListCustReact(All _)
        {
            var customReactions = _service.GetCustomReactions(Context.Guild?.Id);

            if (customReactions == null || !customReactions.Any())
            {
                await ReplyErrorLocalized("no_found").ConfigureAwait(false);
                return;
            }

            using (var txtStream = await customReactions.GroupBy(cr => cr.Trigger)
                                                        .OrderBy(cr => cr.Key)
                                                        .Select(cr => new { Trigger = cr.Key, Responses = cr.Select(y => new { id = y.Id, text = y.Response }).ToList() })
                                                        .ToJson()
                                                        .ToStream()
                                                        .ConfigureAwait(false))
            {

                if (Context.Guild == null) // its a private one, just send back
                    await Context.Channel.SendFileAsync(txtStream, "customreactions.txt", GetText("list_all")).ConfigureAwait(false);
                else
                    await ((IGuildUser)Context.User).SendFileAsync(txtStream, "customreactions.txt", GetText("list_all"), false).ConfigureAwait(false);
            }
        }

        [WizBotCommand, Usage, Description, Aliases]
        public async Task ListCustReactG(int page = 1)
        {
            if (--page < 0 || page > 9999)
                return;
            var customReactions = _service.GetCustomReactions(Context.Guild?.Id);

            if (customReactions == null || !customReactions.Any())
            {
                await ReplyErrorLocalized("no_found").ConfigureAwait(false);
            }
            else
            {
                var ordered = customReactions
                    .GroupBy(cr => cr.Trigger)
                    .OrderBy(cr => cr.Key)
                    .ToList();

                await Context.SendPaginatedConfirmAsync(page, (curPage) =>
                    new EmbedBuilder().WithOkColor()
                        .WithTitle(GetText("name"))
                        .WithDescription(string.Join("\r\n", ordered
                                                         .Skip(curPage * 20)
                                                         .Take(20)
                                                         .Select(cr => $"**{cr.Key.Trim().ToLowerInvariant()}** `x{cr.Count()}`"))),
                    ordered.Count, 20).ConfigureAwait(false);
            }
        }

        [WizBotCommand, Usage, Description, Aliases]
        public async Task ShowCustReact(int id)
        {
            var found = _service.GetCustomReaction(Context.Guild?.Id, id);

            if (found == null)
            {
                await ReplyErrorLocalized("no_found_id").ConfigureAwait(false);
                return;
            }
            else
            {
                await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithDescription($"#{id}")
                    .AddField(efb => efb.WithName(GetText("trigger")).WithValue(found.Trigger.TrimTo(1024)))
                    .AddField(efb => efb.WithName(GetText("response")).WithValue((found.Response + "\n```css\n" + found.Response).TrimTo(1020) + "```"))
                    ).ConfigureAwait(false);
            }
        }

        [WizBotCommand, Usage, Description, Aliases]
        public async Task DelCustReact(int id)
        {
            if (!AdminInGuildOrOwnerInDm())
            {
                await ReplyErrorLocalized("insuff_perms").ConfigureAwait(false);
                return;
            }

            var cr = await _service.DeleteCustomReactionAsync(Context.Guild?.Id, id);

            if (cr != null)
            {
                await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithTitle(GetText("deleted"))
                    .WithDescription("#" + cr.Id)
                    .AddField(efb => efb.WithName(GetText("trigger")).WithValue(cr.Trigger.TrimTo(1024)))
                    .AddField(efb => efb.WithName(GetText("response")).WithValue(cr.Response.TrimTo(1024)))).ConfigureAwait(false);
            }
            else
            {
                await ReplyErrorLocalized("no_found_id").ConfigureAwait(false);
            }
        }

        [WizBotCommand, Usage, Description, Aliases]
        public Task CrCa(int id)
            => InternalCrEdit(id, CustomReactionsService.CrField.ContainsAnywhere);

        [WizBotCommand, Usage, Description, Aliases]
        public Task CrDm(int id)
            => InternalCrEdit(id, CustomReactionsService.CrField.DmResponse);

        [WizBotCommand, Usage, Description, Aliases]
        public Task CrAd(int id)
            => InternalCrEdit(id, CustomReactionsService.CrField.AutoDelete);

        [WizBotCommand, Usage, Description, Aliases]
        [OwnerOnly]
        public Task CrsReload()
        {
            _service.TriggerReloadCustomReactions();

            return Context.Channel.SendConfirmAsync("👌");
        }

        private async Task InternalCrEdit(int id, CustomReactionsService.CrField option)
        {
            if (!AdminInGuildOrOwnerInDm())
            {
                await ReplyErrorLocalized("insuff_perms").ConfigureAwait(false);
                return;
            }
            var (success, newVal) = await _service.ToggleCrOptionAsync(id, option).ConfigureAwait(false);
            if (!success)
            {
                await ReplyErrorLocalized("no_found_id").ConfigureAwait(false);
                return;
            }

            if (newVal)
            {
                await ReplyConfirmLocalized("option_enabled", Format.Code(option.ToString()), Format.Code(id.ToString())).ConfigureAwait(false);
            }
            else
            {
                await ReplyConfirmLocalized("option_disabled", Format.Code(option.ToString()), Format.Code(id.ToString())).ConfigureAwait(false);
            }
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task CrClear()
        {
            if (await PromptUserConfirmAsync(new EmbedBuilder()
                .WithTitle("Custom reaction clear")
                .WithDescription("This will delete all custom reactions on this server.")).ConfigureAwait(false))
            {
                var count = _service.ClearCustomReactions(Context.Guild.Id);
                await ReplyConfirmLocalized("cleared", count).ConfigureAwait(false);
            }
        }
    }
}