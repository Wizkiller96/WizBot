using Discord;
using Discord.Commands;
using WizBot.Common.Attributes;
using WizBot.Core.Services;
using WizBot.Extensions;
using WizBot.Modules.CustomReactions.Services;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WizBot.Core.Common;

namespace WizBot.Modules.CustomReactions
{
    public class CustomReactions : WizBotModule<CustomReactionsService>
    {
        private readonly IBotCredentials _creds;
        private readonly IHttpClientFactory _clientFactory;

        public CustomReactions(IBotCredentials creds, IHttpClientFactory clientFactory)
        {
            _creds = creds;
            _clientFactory = clientFactory;
        }

        private bool AdminInGuildOrOwnerInDm() => (ctx.Guild == null && _creds.IsOwner(ctx.User))
                                                  || (ctx.Guild != null && ((IGuildUser)ctx.User).GuildPermissions.Administrator);

        [WizBotCommand, Usage, Description, Aliases]
        public async Task AddCustReact(string key, [Leftover] string message)
        {
            var channel = ctx.Channel as ITextChannel;
            if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(key))
                return;

            if (!AdminInGuildOrOwnerInDm())
            {
                await ReplyErrorLocalizedAsync("insuff_perms").ConfigureAwait(false);
                return;
            }

            var cr = await _service.AddAsync(ctx.Guild?.Id, key, message);

            await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                .WithTitle(GetText("new_cust_react"))
                .WithDescription($"#{cr.Id}")
                .AddField(efb => efb.WithName(GetText("trigger")).WithValue(key))
                .AddField(efb => efb.WithName(GetText("response")).WithValue(message.Length > 1024 ? GetText("redacted_too_long") : message))
                ).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        public async Task EditCustReact(int id, [Leftover] string message)
        {
            var channel = ctx.Channel as ITextChannel;
            if (string.IsNullOrWhiteSpace(message) || id < 0)
                return;

            if ((channel == null && !_creds.IsOwner(ctx.User)) || (channel != null && !((IGuildUser)ctx.User).GuildPermissions.Administrator))
            {
                await ReplyErrorLocalizedAsync("insuff_perms").ConfigureAwait(false);
                return;
            }

            var cr = await _service.EditAsync(ctx.Guild?.Id, id, message).ConfigureAwait(false);
            if (cr != null)
            {
                await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithTitle(GetText("edited_cust_react"))
                    .WithDescription($"#{id}")
                    .AddField(efb => efb.WithName(GetText("trigger")).WithValue(cr.Trigger))
                    .AddField(efb => efb.WithName(GetText("response")).WithValue(message.Length > 1024 ? GetText("redacted_too_long") : message))
                    ).ConfigureAwait(false);
            }
            else
            {
                await ReplyErrorLocalizedAsync("edit_fail").ConfigureAwait(false);
            }
        }

        [WizBotCommand, Usage, Description, Aliases]
        [Priority(1)]
        public async Task ListCustReact(int page = 1)
        {
            if (--page < 0 || page > 999)
                return;

            var customReactions = _service.GetCustomReactionsFor(ctx.Guild?.Id);

            if (customReactions == null || !customReactions.Any())
            {
                await ReplyErrorLocalizedAsync("no_found").ConfigureAwait(false);
                return;
            }

            await ctx.SendPaginatedConfirmAsync(page, curPage =>
                new EmbedBuilder().WithOkColor()
                    .WithTitle(GetText("custom_reactions"))
                    .WithDescription(string.Join("\n", customReactions.OrderBy(cr => cr.Trigger)
                                                    .Skip(curPage * 20)
                                                    .Take(20)
                                                    .Select(cr =>
                                                    {
                                                        var str = $"`{cr.Id}` {cr.Trigger}";
                                                        if (cr.AutoDeleteTrigger)
                                                        {
                                                            str = " \\✗ " + str;
                                                        }
                                                        else
                                                        {
                                                            str = "◾" + str;
                                                        }
                                                        if (cr.DmResponse)
                                                        {
                                                            str = "\\✉ " + str;
                                                        }
                                                        else
                                                        {
                                                            str = "◾" + str;
                                                        }
                                                        if (cr.ContainsAnywhere)
                                                        {
                                                            str = "\\🗯 " + str;
                                                        }
                                                        else
                                                        {
                                                            str = "◾" + str;
                                                        }
                                                        
                                                        var reactions = cr.GetReactions();
                                                        if(reactions.Any())
                                                        {
                                                            str = str + " // " + string.Join(" ", reactions);
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
            await ReplyPendingLocalizedAsync("obsolete_use", Format.Code($"{Prefix}crsexport"));
            await CrsExport();
        }

        [WizBotCommand, Usage, Description, Aliases]
        public async Task ListCustReactG(int page = 1)
        {
            await ReplyPendingLocalizedAsync("obsolete_use", Format.Code($"{Prefix}crsexport"));
            await CrsExport();
        }

        [WizBotCommand, Usage, Description, Aliases]
        public async Task ShowCustReact(int id)
        {
            var found = _service.GetCustomReaction(ctx.Guild?.Id, id);

            if (found == null)
            {
                await ReplyErrorLocalizedAsync("no_found_id").ConfigureAwait(false);
                return;
            }
            else
            {
                await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
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
                await ReplyErrorLocalizedAsync("insuff_perms").ConfigureAwait(false);
                return;
            }

            var cr = await _service.DeleteAsync(ctx.Guild?.Id, id);

            if (cr != null)
            {
                await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                    .WithTitle(GetText("deleted"))
                    .WithDescription("#" + cr.Id)
                    .AddField(efb => efb.WithName(GetText("trigger")).WithValue(cr.Trigger.TrimTo(1024)))
                    .AddField(efb => efb.WithName(GetText("response")).WithValue(cr.Response.TrimTo(1024)))).ConfigureAwait(false);
            }
            else
            {
                await ReplyErrorLocalizedAsync("no_found_id").ConfigureAwait(false);
            }
        }

        [WizBotCommand, Usage, Description, Aliases]
        public async Task CrReact(int id, params string[] emojiStrs)
        {
            if (!AdminInGuildOrOwnerInDm())
            {
                await ReplyErrorLocalizedAsync("insuff_perms").ConfigureAwait(false);
                return;
            }

            var cr = _service.GetCustomReaction(Context.Guild?.Id, id);
            if (cr is null)
            {
                await ReplyErrorLocalizedAsync("no_found").ConfigureAwait(false);
                return;
            }

            if (emojiStrs.Length == 0)
            {
                await _service.ResetCrReactions(ctx.Guild?.Id, id);
                await ReplyConfirmLocalizedAsync("crr_reset", Format.Bold(id.ToString())).ConfigureAwait(false);
                return;
            }

            List<string> succ = new List<string>();
            foreach (var emojiStr in emojiStrs)
            {

                var emote = emojiStr.ToIEmote();

                // i should try adding these emojis right away to the message, to make sure the bot can react with these emojis. If it fails, skip that emoji
                try
                {
                    await Context.Message.AddReactionAsync(emote).ConfigureAwait(false);
                    await Task.Delay(100).ConfigureAwait(false);
                    succ.Add(emojiStr);

                    if (succ.Count >= 3)
                        break;
                }
                catch { }
            }

            if(succ.Count == 0)
            {
                await ReplyErrorLocalizedAsync("invalid_emojis").ConfigureAwait(false);
                return;
            }

            await _service.SetCrReactions(ctx.Guild?.Id, id, succ);


            await ReplyConfirmLocalizedAsync("crr_set", Format.Bold(id.ToString()), string.Join(", ", succ.Select(x => x.ToString()))).ConfigureAwait(false);

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
        public Task CrAt(int id)
            => InternalCrEdit(id, CustomReactionsService.CrField.AllowTarget);

        [WizBotCommand, Usage, Description, Aliases]
        [OwnerOnly]
        public async Task CrsReload()
        {
            await _service.TriggerReloadCustomReactions();

            await ctx.OkAsync();
        }

        private async Task InternalCrEdit(int id, CustomReactionsService.CrField option)
        {
            var cr = _service.GetCustomReaction(ctx.Guild?.Id, id);
            if (!AdminInGuildOrOwnerInDm())
            {
                await ReplyErrorLocalizedAsync("insuff_perms").ConfigureAwait(false);
                return;
            }
            var (success, newVal) = await _service.ToggleCrOptionAsync(id, option).ConfigureAwait(false);
            if (!success)
            {
                await ReplyErrorLocalizedAsync("no_found_id").ConfigureAwait(false);
                return;
            }

            if (newVal)
            {
                await ReplyConfirmLocalizedAsync("option_enabled", Format.Code(option.ToString()), Format.Code(id.ToString())).ConfigureAwait(false);
            }
            else
            {
                await ReplyConfirmLocalizedAsync("option_disabled", Format.Code(option.ToString()), Format.Code(id.ToString())).ConfigureAwait(false);
            }
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task CrClear()
        {
            if (await PromptUserConfirmAsync(new EmbedBuilder()
                .WithTitle("Custom reaction clear")
                .WithDescription("This will delete all custom reactions on this server.")).ConfigureAwait(false))
            {
                var count = _service.DeleteAllCustomReactions(ctx.Guild.Id);
                await ReplyConfirmLocalizedAsync("cleared", count).ConfigureAwait(false);
            }
        }

        [WizBotCommand, Usage, Description, Aliases]
        public async Task CrsExport()
        {
            if (!AdminInGuildOrOwnerInDm())
            {
                await ReplyErrorLocalizedAsync("insuff_perms").ConfigureAwait(false);
                return;
            }
            
            _ = ctx.Channel.TriggerTypingAsync();

            var serialized = _service.ExportCrs(ctx.Guild?.Id);
            using var stream = await serialized.ToStream();
            await ctx.Channel.SendFileAsync(stream, "crs-export.yml", text: null);
        }

        [WizBotCommand, Usage, Description, Aliases]
#if GLOBAL_WIZBOT
        [OwnerOnly]
#endif
        public async Task CrsImport([Leftover]string input = null)
        {
            if (!AdminInGuildOrOwnerInDm())
            {
                await ReplyErrorLocalizedAsync("insuff_perms").ConfigureAwait(false);
                return;
            }

            input = input?.Trim();

            _ = ctx.Channel.TriggerTypingAsync();

            if (input is null)
            {
                var attachment = ctx.Message.Attachments.FirstOrDefault();
                if (attachment is null)
                {
                    await ReplyErrorLocalizedAsync("expr_import_no_input");
                    return;
                }

                using var client = _clientFactory.CreateClient();
                input = await client.GetStringAsync(attachment.Url);

                if (string.IsNullOrWhiteSpace(input))
                {
                    await ReplyErrorLocalizedAsync("expr_import_no_input");
                    return;
                }
            }

            var succ = await _service.ImportCrsAsync(ctx.Guild?.Id, input);
            if (!succ)
            {
                await ReplyErrorLocalizedAsync("expr_import_invalid_data");
                return;
            }
            
            await ctx.OkAsync();
        }
    }
}
