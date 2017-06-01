using Discord;
using Discord.Commands;
using WizBot.Extensions;
using WizBot.Services;
using NLog;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace WizBot.Modules
{
    public abstract class WizBotTopLevelModule : ModuleBase
    {
        protected readonly Logger _log;
        protected CultureInfo _cultureInfo;
        public readonly string Prefix;
        public readonly string ModuleTypeName;
        public readonly string LowerModuleTypeName;

        //todo :thinking:
        public WizBotStrings _strings { get; set; }
        public ILocalization _localization { get; set; }

        protected WizBotTopLevelModule(bool isTopLevelModule = true)
        {
            //if it's top level module
            ModuleTypeName = isTopLevelModule ? this.GetType().Name : this.GetType().DeclaringType.Name;
            LowerModuleTypeName = ModuleTypeName.ToLowerInvariant();
            Prefix = WizBot.Prefix;
            _log = LogManager.GetCurrentClassLogger();
        }

        protected override void BeforeExecute()
        {
            _cultureInfo = _localization.GetCultureInfo(Context.Guild?.Id);

            _log.Info("Culture info is {0}", _cultureInfo);
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
            _strings.GetText(key, _cultureInfo, LowerModuleTypeName);

        protected string GetText(string key, params object[] replacements) =>
            _strings.GetText(key, _cultureInfo, LowerModuleTypeName, replacements);

        public Task<IUserMessage> ErrorLocalized(string textKey, params object[] replacements)
        {
            var text = GetText(textKey, replacements);
            return Context.Channel.SendErrorAsync(text);
        }

        public Task<IUserMessage> ReplyErrorLocalized(string textKey, params object[] replacements)
        {
            var text = GetText(textKey, replacements);
            return Context.Channel.SendErrorAsync(Context.User.Mention + " " + text);
        }

        public Task<IUserMessage> ConfirmLocalized(string textKey, params object[] replacements)
        {
            var text = GetText(textKey, replacements);
            return Context.Channel.SendConfirmAsync(text);
        }

        public Task<IUserMessage> ReplyConfirmLocalized(string textKey, params object[] replacements)
        {
            var text = GetText(textKey, replacements);
            return Context.Channel.SendConfirmAsync(Context.User.Mention + " " + text);
        }
    }

    public abstract class WizBotSubmodule : WizBotTopLevelModule
    {
        protected WizBotSubmodule() : base(false)
        {
        }
    }
}