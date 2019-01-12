﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using WizBot.Core.Services;
using WizBot.Core.Services.Impl;
using WizBot.Extensions;
using NLog;
using System.Globalization;
using System.Threading.Tasks;

namespace WizBot.Modules
{
    public abstract class WizBotTopLevelModule : ModuleBase
    {
        protected Logger _log { get; }
        protected CultureInfo _cultureInfo { get; set; }

        public string ModuleTypeName { get; }
        public string LowerModuleTypeName { get; }

        public WizBotStrings Strings { get; set; }
        public IBotConfigProvider Bc { get; set; }
        public CommandHandler CmdHandler { get; set; }
        public ILocalization Localization { get; set; }

        public string Prefix => CmdHandler.GetPrefix(Context.Guild);

        protected WizBotTopLevelModule(bool isTopLevelModule = true)
        {
            //if it's top level module
            ModuleTypeName = isTopLevelModule ? this.GetType().Name : this.GetType().DeclaringType.Name;
            LowerModuleTypeName = ModuleTypeName.ToLowerInvariant();
            _log = LogManager.GetCurrentClassLogger();
        }

        protected override void BeforeExecute(CommandInfo cmd)
        {
            _cultureInfo = Localization.GetCultureInfo(Context.Guild?.Id);
        }

        //public Task<IUserMessage> ReplyConfirmLocalized(string titleKey, string textKey, string url = null, string footer = null)
        //{
        //    var title = WizBot.ResponsesResourceManager.GetString(titleKey, cultureInfo);
        //    var text = WizBot.ResponsesResourceManager.GetString(textKey, cultureInfo);
        //    return Context.Channel.SendConfirmAsync(title, text, url, footer);
        //}

        //public Task<IUserMessage> ReplyConfirmLocalized(string textKey)
        //{
        //    var text = WizBot.ResponsesResourceManager.GetString(textKey, cultureInfo);
        //    return Context.Channel.SendConfirmAsync(Context.User.Mention + " " + textKey);
        //}

        //public Task<IUserMessage> ReplyErrorLocalized(string titleKey, string textKey, string url = null, string footer = null)
        //{
        //    var title = WizBot.ResponsesResourceManager.GetString(titleKey, cultureInfo);
        //    var text = WizBot.ResponsesResourceManager.GetString(textKey, cultureInfo);
        //    return Context.Channel.SendErrorAsync(title, text, url, footer);
        //}

        protected string GetText(string key) =>
            Strings.GetText(key, _cultureInfo, LowerModuleTypeName);

        protected string GetText(string key, params object[] replacements) =>
            Strings.GetText(key, _cultureInfo, LowerModuleTypeName, replacements);

        public Task<IUserMessage> ErrorLocalizedAsync(string textKey, params object[] replacements)
        {
            var text = GetText(textKey, replacements);
            return Context.Channel.SendErrorAsync(text);
        }

        public Task<IUserMessage> ReplyErrorLocalizedAsync(string textKey, params object[] replacements)
        {
            var text = GetText(textKey, replacements);
            return Context.Channel.SendErrorAsync(Format.Bold(Context.User.ToString()) + " " + text);
        }

        public Task<IUserMessage> ConfirmLocalizedAsync(string textKey, params object[] replacements)
        {
            var text = GetText(textKey, replacements);
            return Context.Channel.SendConfirmAsync(text);
        }

        public Task<IUserMessage> ReplyConfirmLocalizedAsync(string textKey, params object[] replacements)
        {
            var text = GetText(textKey, replacements);
            return Context.Channel.SendConfirmAsync(Format.Bold(Context.User.ToString()) + " " + text);
        }

        public async Task<bool> PromptUserConfirmAsync(EmbedBuilder embed)
        {
            embed.WithOkColor()
                .WithFooter("yes/no");

            var msg = await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            try
            {
                var input = await GetUserInputAsync(Context.User.Id, Context.Channel.Id).ConfigureAwait(false);
                input = input?.ToUpperInvariant();

                if (input != "YES" && input != "Y")
                {
                    return false;
                }

                return true;
            }
            finally
            {
                var _ = Task.Run(() => msg.DeleteAsync());
            }
        }

        // TypeConverter typeConverter = TypeDescriptor.GetConverter(propType); ?
        public async Task<string> GetUserInputAsync(ulong userId, ulong channelId)
        {
            var userInputTask = new TaskCompletionSource<string>();
            var dsc = (DiscordSocketClient)Context.Client;
            try
            {
                dsc.MessageReceived += MessageReceived;

                if ((await Task.WhenAny(userInputTask.Task, Task.Delay(10000)).ConfigureAwait(false)) != userInputTask.Task)
                {
                    return null;
                }

                return await userInputTask.Task.ConfigureAwait(false);
            }
            finally
            {
                dsc.MessageReceived -= MessageReceived;
            }

            Task MessageReceived(SocketMessage arg)
            {
                var _ = Task.Run(() =>
                {
                    if (!(arg is SocketUserMessage userMsg) ||
                        !(userMsg.Channel is ITextChannel chan) ||
                        userMsg.Author.Id != userId ||
                        userMsg.Channel.Id != channelId)
                    {
                        return Task.CompletedTask;
                    }

                    if (userInputTask.TrySetResult(arg.Content))
                    {
                        userMsg.DeleteAfter(1);
                    }
                    return Task.CompletedTask;
                });
                return Task.CompletedTask;
            }
        }
    }

    public abstract class WizBotTopLevelModule<TService> : WizBotTopLevelModule where TService : INService
    {
        public TService _service { get; set; }

        protected WizBotTopLevelModule(bool isTopLevel = true) : base(isTopLevel)
        {
        }
    }

    public abstract class WizBotSubmodule : WizBotTopLevelModule
    {
        protected WizBotSubmodule() : base(false) { }
    }

    public abstract class WizBotSubmodule<TService> : WizBotTopLevelModule<TService> where TService : INService
    {
        protected WizBotSubmodule() : base(false)
        {
        }
    }
}