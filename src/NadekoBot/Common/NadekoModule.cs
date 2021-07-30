using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Services;
using NadekoBot.Extensions;
using System.Globalization;
using System.Threading.Tasks;

namespace NadekoBot.Modules
{
    public abstract class NadekoModule : ModuleBase
    {
        protected CultureInfo _cultureInfo { get; set; }
        public IBotStrings Strings { get; set; }
        public CommandHandler CmdHandler { get; set; }
        public ILocalization Localization { get; set; }
        public IEmbedBuilderService _eb { get; set; }

        public string Prefix => CmdHandler.GetPrefix(ctx.Guild);

        protected ICommandContext ctx => Context;

        protected NadekoModule()
        {
        }

        protected override void BeforeExecute(CommandInfo cmd)
        {
            _cultureInfo = Localization.GetCultureInfo(ctx.Guild?.Id);
        }
        
        protected string GetText(in LocStr data) =>
            Strings.GetText(data, _cultureInfo);

        public Task<IUserMessage> SendErrorAsync(string error)
            => ctx.Channel.SendErrorAsync(_eb, error);
        
        public Task<IUserMessage> SendErrorAsync(string title, string error, string url = null, string footer = null)
            => ctx.Channel.SendErrorAsync(_eb, title, error, url, footer);
        
        public Task<IUserMessage> SendConfirmAsync(string text)
            => ctx.Channel.SendConfirmAsync(_eb, text);
        
        public Task<IUserMessage> SendConfirmAsync(string title, string text, string url = null, string footer = null)
            => ctx.Channel.SendConfirmAsync(_eb, title, text, url, footer);
        
        public Task<IUserMessage> SendPendingAsync(string text)
            => ctx.Channel.SendPendingAsync(_eb, text);
        
        public Task<IUserMessage> ErrorLocalizedAsync(LocStr str) 
            => SendErrorAsync(GetText(str));

        public Task<IUserMessage> PendingLocalizedAsync(LocStr str) 
            => SendPendingAsync(GetText(str));
        
        public Task<IUserMessage> ConfirmLocalizedAsync(LocStr str) 
            => SendConfirmAsync(GetText(str));

        public Task<IUserMessage> ReplyErrorLocalizedAsync(LocStr str) 
            => SendErrorAsync($"{Format.Bold(ctx.User.ToString())} {GetText(str)}");

        public Task<IUserMessage> ReplyPendingLocalizedAsync(LocStr str) 
            => SendPendingAsync($"{Format.Bold(ctx.User.ToString())} {GetText(str)}");

        public Task<IUserMessage> ReplyConfirmLocalizedAsync(LocStr str)
            => SendConfirmAsync($"{Format.Bold(ctx.User.ToString())} {GetText(str)}");

        public async Task<bool> PromptUserConfirmAsync(IEmbedBuilder embed)
        {
            embed
                .WithPendingColor()
                .WithFooter("yes/no");

            var msg = await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
            try
            {
                var input = await GetUserInputAsync(ctx.User.Id, ctx.Channel.Id).ConfigureAwait(false);
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
            var dsc = (DiscordSocketClient)ctx.Client;
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

    public abstract class NadekoModule<TService> : NadekoModule
    {
        public TService _service { get; set; }

        protected NadekoModule() : base()
        {
        }
    }

    public abstract class NadekoSubmodule : NadekoModule
    {
        protected NadekoSubmodule() : base() { }
    }

    public abstract class NadekoSubmodule<TService> : NadekoModule<TService>
    {
        protected NadekoSubmodule() : base()
        {
        }
    }
}