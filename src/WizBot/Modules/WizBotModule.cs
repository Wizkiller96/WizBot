using Discord;
using Discord.Commands;
using WizBot.Extensions;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading.Tasks;

namespace WizBot.Modules
{
    public abstract class WizBotModule : ModuleBase
    {
        protected readonly Logger _log;
        public readonly string _prefix;
        public readonly CultureInfo cultureInfo;

        public WizBotModule(bool isTopLevelModule = true)
        {
            //if it's top level module
            var typeName = isTopLevelModule ? this.GetType().Name : this.GetType().DeclaringType.Name;
            if (!WizBot.ModulePrefixes.TryGetValue(typeName, out _prefix))
                _prefix = "?err?";
            _log = LogManager.GetCurrentClassLogger();

            cultureInfo = (Context.Guild == null
                ? CultureInfo.CurrentCulture
                : WizBot.Localization.GetCultureInfo(Context.Guild));
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

        public Task<IUserMessage> ErrorLocalized(string textKey, params object[] replacements)
        {
            var text = WizBot.ResponsesResourceManager.GetString(textKey, cultureInfo);
            return Context.Channel.SendErrorAsync(string.Format(text, replacements));
        }

        public Task<IUserMessage> ReplyErrorLocalized(string textKey, params object[] replacements)
        {
            var text = WizBot.ResponsesResourceManager.GetString(textKey, cultureInfo);
            return Context.Channel.SendErrorAsync(Context.User.Mention + " " + string.Format(text, replacements));
        }

        public Task<IUserMessage> ConfirmLocalized(string textKey, params object[] replacements)
        {
            var text = WizBot.ResponsesResourceManager.GetString(textKey, cultureInfo);
            return Context.Channel.SendConfirmAsync(string.Format(text, replacements));
        }

        public Task<IUserMessage> ReplyConfirmLocalized(string textKey, params object[] replacements)
        {
            var text = WizBot.ResponsesResourceManager.GetString(textKey, cultureInfo);
            return Context.Channel.SendConfirmAsync(Context.User.Mention + " " + string.Format(text, replacements));
        }
    }

    public abstract class WizBotSubmodule : WizBotModule
    {
        public WizBotSubmodule() : base(false)
        {
        }
    }
}