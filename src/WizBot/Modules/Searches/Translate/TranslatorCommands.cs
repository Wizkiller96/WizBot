﻿#nullable disable
namespace WizBot.Modules.Searches;

public partial class Searches
{
    [Group]
    public partial class TranslateCommands : WizBotModule<ITranslateService>
    {
        public enum AutoDeleteAutoTranslate
        {
            Del,
            Nodel
        }

        [Cmd]
        public async Task Translate(string from, string to, [Leftover] string text = null)
        {
            try
            {
                await ctx.Channel.TriggerTypingAsync();
                var translation = await _service.Translate(from, to, text);

                var embed = _eb.Create(ctx).WithOkColor().AddField(from, text).AddField(to, translation);

                await ctx.Channel.EmbedAsync(embed);
            }
            catch
            {
                await ReplyErrorLocalizedAsync(strs.bad_input_format);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [BotPerm(ChannelPerm.ManageMessages)]
        [OwnerOnly]
        public async Task AutoTranslate(AutoDeleteAutoTranslate autoDelete = AutoDeleteAutoTranslate.Nodel)
        {
            var toggle =
                await _service.ToggleAtl(ctx.Guild.Id, ctx.Channel.Id, autoDelete == AutoDeleteAutoTranslate.Del);
            if (toggle)
                await ReplyConfirmLocalizedAsync(strs.atl_started);
            else
                await ReplyConfirmLocalizedAsync(strs.atl_stopped);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task AutoTransLang()
        {
            if (await _service.UnregisterUser(ctx.Channel.Id, ctx.User.Id))
                await ReplyConfirmLocalizedAsync(strs.atl_removed);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task AutoTransLang(string from, string to)
        {
            var succ = await _service.RegisterUserAsync(ctx.User.Id, ctx.Channel.Id, from.ToLower(), to.ToLower());

            if (succ is null)
            {
                await ReplyErrorLocalizedAsync(strs.atl_not_enabled);
                return;
            }

            if (succ is false)
            {
                await ReplyErrorLocalizedAsync(strs.invalid_lang);
                return;
            }

            await ReplyConfirmLocalizedAsync(strs.atl_set(from, to));
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Translangs()
        {
            var langs = _service.GetLanguages().ToList();
            
            var eb = _eb.Create()
                        .WithTitle($"Supported {prefix}translate Languages")
                        .WithOkColor();

            foreach (var chunk in langs.Chunk(15))
            {
                eb.AddField("󠀁", chunk.Join("\n"), isInline: true);
            }

            await ctx.Channel.EmbedAsync(eb);
        }
    }
}