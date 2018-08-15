﻿using Discord;
using Discord.Commands;
using WizBot.Extensions;
using System.Threading.Tasks;
using System.Linq;
using WizBot.Common.Attributes;
using WizBot.Core.Services;
using WizBot.Modules.Searches.Services;

namespace WizBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class TranslateCommands : WizBotSubmodule
        {
            private readonly SearchesService _searches;
            private readonly IGoogleApiService _google;

            public TranslateCommands(SearchesService searches, IGoogleApiService google)
            {
                _searches = searches;
                _google = google;
            }

            [WizBotCommand, Usage, Description, Aliases]
            public async Task Translate(string langs, [Remainder] string text = null)
            {
                try
                {
                    await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
                    var translation = await _searches.Translate(langs, text).ConfigureAwait(false);
                    await Context.Channel.SendConfirmAsync(GetText("translation") + " " + langs, translation).ConfigureAwait(false);
                }
                catch
                {
                    await ReplyErrorLocalized("bad_input_format").ConfigureAwait(false);
                }
            }

            //[WizBotCommand, Usage, Description, Aliases]
            //[OwnerOnly]
            //public async Task Obfuscate([Remainder] string txt)
            //{
            //    var lastItem = "en";
            //    foreach (var item in _google.Languages.Except(new[] { "en" }).Where(x => x.Length < 4))
            //    {
            //        var txt2 = await _searches.Translate(lastItem + ">" + item, txt);
            //        await Context.Channel.EmbedAsync(new EmbedBuilder()
            //            .WithOkColor()
            //            .WithTitle(lastItem + ">" + item)
            //            .AddField("Input", txt)
            //            .AddField("Output", txt2));
            //        txt = txt2;
            //        await Task.Delay(500);
            //        lastItem = item;
            //    }
            //    txt = await _searches.Translate(lastItem + ">en", txt);
            //    await Context.Channel.SendConfirmAsync("Final output:\n\n" + txt);
            //}

            public enum AutoDeleteAutoTranslate
            {
                Del,
                Nodel
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            [OwnerOnly]
            public async Task AutoTranslate(AutoDeleteAutoTranslate autoDelete = AutoDeleteAutoTranslate.Nodel)
            {
                var channel = (ITextChannel)Context.Channel;

                if (autoDelete == AutoDeleteAutoTranslate.Del)
                {
                    _searches.TranslatedChannels.AddOrUpdate(channel.Id, true, (key, val) => true);
                    await ReplyConfirmLocalized("atl_ad_started").ConfigureAwait(false);
                    return;
                }
                
                if (_searches.TranslatedChannels.TryRemove(channel.Id, out _))
                {
                    await ReplyConfirmLocalized("atl_stopped").ConfigureAwait(false);
                    return;
                }
                if (_searches.TranslatedChannels.TryAdd(channel.Id, autoDelete == AutoDeleteAutoTranslate.Del))
                {
                    await ReplyConfirmLocalized("atl_started").ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task AutoTransLang([Remainder] string langs = null)
            {
                var ucp = (Context.User.Id, Context.Channel.Id);

                if (string.IsNullOrWhiteSpace(langs))
                {
                    if (_searches.UserLanguages.TryRemove(ucp, out langs))
                        await ReplyConfirmLocalized("atl_removed").ConfigureAwait(false);
                    return;
                }

                var langarr = langs.ToLowerInvariant().Split('>');
                if (langarr.Length != 2)
                    return;
                var from = langarr[0];
                var to = langarr[1];

                if (!_google.Languages.Contains(from) || !_google.Languages.Contains(to))
                {
                    await ReplyErrorLocalized("invalid_lang").ConfigureAwait(false);
                    return;
                }

                _searches.UserLanguages.AddOrUpdate(ucp, langs, (key, val) => langs);

                await ReplyConfirmLocalized("atl_set", from, to).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Translangs()
            {
                await Context.Channel.SendTableAsync(_google.Languages, str => $"{str,-15}", 3).ConfigureAwait(false);
            }

        }
    }
}