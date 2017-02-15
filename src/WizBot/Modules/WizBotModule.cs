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

        public Task<IUserMessage> ConfirmLocalized(string titleKey, string textKey, string url = null, string footer = null)
        {
            var title = WizBot.ResponsesResourceManager.GetString(titleKey, cultureInfo);
            var text = WizBot.ResponsesResourceManager.GetString(textKey, cultureInfo);
            return Context.Channel.SendConfirmAsync(title, text, url, footer);
        }

        public Task<IUserMessage> ConfirmLocalized(string textKey)
        {
            var text = WizBot.ResponsesResourceManager.GetString(textKey, cultureInfo);
            return Context.Channel.SendConfirmAsync(textKey);
        }

        public Task<IUserMessage> ErrorLocalized(string titleKey, string textKey, string url = null, string footer = null)
        {
            var title = WizBot.ResponsesResourceManager.GetString(titleKey, cultureInfo);
            var text = WizBot.ResponsesResourceManager.GetString(textKey, cultureInfo);
            return Context.Channel.SendErrorAsync(title, text, url, footer);
        }

        public Task<IUserMessage> ErrorLocalized(string textKey)
        {
            var text = WizBot.ResponsesResourceManager.GetString(textKey, cultureInfo);
            return Context.Channel.SendErrorAsync(textKey);
        }
    }

    public abstract class WizBotSubmodule : WizBotModule
    {
        public WizBotSubmodule() : base(false)
        {
        }
    }
}