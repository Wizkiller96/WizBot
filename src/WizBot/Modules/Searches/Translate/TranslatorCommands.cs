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
        public async Task Translate(string fromLang, string toLang, [Leftover] string text = null)
        {
            try
            {
                await ctx.Channel.TriggerTypingAsync();
                var translation = await _service.Translate(fromLang, toLang, text);

                var embed = _sender.CreateEmbed().WithOkColor().AddField(fromLang, text).AddField(toLang, translation);

                await Response().Embed(embed).SendAsync();
            }
            catch
            {
                await Response().Error(strs.bad_input_format).SendAsync();
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
                await Response().Confirm(strs.atl_started).SendAsync();
            else
                await Response().Confirm(strs.atl_stopped).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task AutoTransLang()
        {
            if (await _service.UnregisterUser(ctx.Channel.Id, ctx.User.Id))
                await Response().Confirm(strs.atl_removed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task AutoTransLang(string fromLang, string toLang)
        {
            var succ = await _service.RegisterUserAsync(ctx.User.Id, ctx.Channel.Id, fromLang.ToLower(), toLang.ToLower());

            if (succ is null)
            {
                await Response().Error(strs.atl_not_enabled).SendAsync();
                return;
            }

            if (succ is false)
            {
                await Response().Error(strs.invalid_lang).SendAsync();
                return;
            }

            await Response().Confirm(strs.atl_set(fromLang, toLang)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Translangs()
        {
            var langs = _service.GetLanguages().ToList();
            
            var eb = _sender.CreateEmbed()
                        .WithTitle(GetText(strs.supported_languages))
                        .WithOkColor();

            foreach (var chunk in langs.Chunk(15))
            {
                eb.AddField("󠀁", chunk.Join("\n"), inline: true);
            }

            await Response().Embed(eb).SendAsync();
        }
    }
}