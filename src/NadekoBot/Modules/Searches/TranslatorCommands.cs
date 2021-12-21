using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class TranslateCommands : NadekoSubmodule<ITranslateService>
        {
            [NadekoCommand, Aliases]
            public async Task Translate(string from, string to, [Leftover] string text)
            {
                try
                {
                    await ctx.Channel.TriggerTypingAsync().ConfigureAwait(false);
                    var translation = await _service.Translate(from.ToLower(), to.ToLower(), text).ConfigureAwait(false);

                    var embed = _eb.Create(ctx)
                        .WithOkColor()
                        .AddField(from, text, false)
                        .AddField(to, translation, false);

                    await ctx.Channel.EmbedAsync(embed);
                }
                catch
                {
                    await ReplyErrorLocalizedAsync(strs.bad_input_format).ConfigureAwait(false);
                }
            }

            public enum AutoDeleteAutoTranslate
            {
                Del,
                Nodel
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            [BotPerm(ChannelPerm.ManageMessages)]
            [OwnerOnly]
            public async Task AutoTranslate(AutoDeleteAutoTranslate autoDelete = AutoDeleteAutoTranslate.Nodel)
            {
                var toggle = await _service.ToggleAtl(ctx.Guild.Id, ctx.Channel.Id, autoDelete == AutoDeleteAutoTranslate.Del);
                if (toggle)
                {
                    await ReplyConfirmLocalizedAsync(strs.atl_started).ConfigureAwait(false);
                }
                else
                {
                    await ReplyConfirmLocalizedAsync(strs.atl_stopped).ConfigureAwait(false);
                }
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task AutoTransLang()
            {
                if (await _service.UnregisterUser(ctx.Channel.Id, ctx.User.Id))
                {
                    await ReplyConfirmLocalizedAsync(strs.atl_removed).ConfigureAwait(false);
                }
            }
            
            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task AutoTransLang(string from, string to)
            {
                var succ = await _service.RegisterUserAsync(ctx.User.Id,
                    ctx.Channel.Id,
                    from.ToLower(),
                    to.ToLower());

                if (succ is null)
                {
                    await ReplyErrorLocalizedAsync(strs.atl_not_enabled);
                    return;
                }
                
                if (succ is false)
                {
                    await ReplyErrorLocalizedAsync(strs.invalid_lang).ConfigureAwait(false);
                    return;
                }

                await ReplyConfirmLocalizedAsync(strs.atl_set(from, to));
            }

            [NadekoCommand, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Translangs()
            {
                await ctx.Channel.SendTableAsync(_service.GetLanguages(), str => $"{str,-15}", 3).ConfigureAwait(false);
            }

        }
    }
}