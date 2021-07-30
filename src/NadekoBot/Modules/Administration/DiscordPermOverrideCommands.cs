using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Common.TypeReaders;
using NadekoBot.Extensions;
using NadekoBot.Modules.Administration.Services;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class DiscordPermOverrideCommands : NadekoSubmodule<DiscordPermOverrideService>
        {
            // override stats, it should require that the user has managessages guild permission
            // .po 'stats' add user guild managemessages
            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            public async Task DiscordPermOverride(CommandOrCrInfo cmd, params GuildPerm[] perms)
            {
                if (perms is null || perms.Length == 0)
                {
                    await _service.RemoveOverride(ctx.Guild.Id, cmd.Name);
                    await ReplyConfirmLocalizedAsync(strs.perm_override_reset);
                    return;
                }

                var aggregatePerms = perms.Aggregate((acc, seed) => seed | acc);
                await _service.AddOverride(ctx.Guild.Id, cmd.Name, aggregatePerms);

                await ReplyConfirmLocalizedAsync(strs.perm_override(
                    Format.Bold(aggregatePerms.ToString()),
                    Format.Code(cmd.Name)));
            }
            
            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            public async Task DiscordPermOverrideReset()
            {
                var result = await PromptUserConfirmAsync(_eb.Create()
                    .WithOkColor()
                    .WithDescription(GetText(strs.perm_override_all_confirm)));
                
                if (!result)
                    return;
                await _service.ClearAllOverrides(ctx.Guild.Id);

                await ReplyConfirmLocalizedAsync(strs.perm_override_all);
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            public async Task DiscordPermOverrideList(int page = 1)
            {
                if (--page < 0)
                    return;
                
                var overrides = await _service.GetAllOverrides(ctx.Guild.Id);

                await ctx.SendPaginatedConfirmAsync(page, curPage =>
                {
                    var eb = _eb.Create()
                        .WithTitle(GetText(strs.perm_overrides))
                        .WithOkColor();

                    var thisPageOverrides = overrides
                        .Skip(9 * curPage)
                        .Take(9)
                        .ToList();

                    if (thisPageOverrides.Count == 0)
                        eb.WithDescription(GetText(strs.perm_override_page_none));
                    else
                        eb.WithDescription(string.Join("\n",
                            thisPageOverrides.Select(ov => $"{ov.Command} => {ov.Perm.ToString()}")));

                    return eb;
                }, overrides.Count, 9, true);
            }
        }
    }
}