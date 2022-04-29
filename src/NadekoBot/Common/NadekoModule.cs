#nullable disable
using System.Globalization;
using MessageType = NadekoBot.Extensions.MessageType;

// ReSharper disable InconsistentNaming

namespace NadekoBot.Modules;

[UsedImplicitly(ImplicitUseTargetFlags.Default
                | ImplicitUseTargetFlags.WithInheritors
                | ImplicitUseTargetFlags.WithMembers)]
public abstract class NadekoModule : ModuleBase
{
    protected CultureInfo Culture { get; set; }

    // Injected by Discord.net
    public IBotStrings Strings { get; set; }
    public CommandHandler _cmdHandler { get; set; }
    public ILocalization _localization { get; set; }
    public IEmbedBuilderService _eb { get; set; }

    protected string prefix
        => _cmdHandler.GetPrefix(ctx.Guild);

    protected ICommandContext ctx
        => Context;

    protected override void BeforeExecute(CommandInfo command)
        => Culture = _localization.GetCultureInfo(ctx.Guild?.Id);

    protected string GetText(in LocStr data)
        => Strings.GetText(data, Culture);
    
    public Task<IUserMessage> SendErrorAsync(
        string title,
        string error,
        string url = null,
        string footer = null, 
        NadekoInteraction inter = null)
        => ctx.Channel.SendErrorAsync(_eb, title, error, url, footer);
    
    public Task<IUserMessage> SendConfirmAsync(
        string title,
        string text,
        string url = null,
        string footer = null)
        => ctx.Channel.SendConfirmAsync(_eb, title, text, url, footer);

    // 
    public Task<IUserMessage> SendErrorAsync(string text, NadekoInteraction inter = null)
        => ctx.Channel.SendAsync(_eb, text, MessageType.Error, inter);
    public Task<IUserMessage> SendConfirmAsync(string text, NadekoInteraction inter = null)
        => ctx.Channel.SendAsync(_eb, text, MessageType.Ok, inter);
    public Task<IUserMessage> SendPendingAsync(string text, NadekoInteraction inter = null)
        => ctx.Channel.SendAsync(_eb, text, MessageType.Pending, inter);

    
    // localized normal
    public Task<IUserMessage> ErrorLocalizedAsync(LocStr str, NadekoInteraction inter = null)
        => SendErrorAsync(GetText(str), inter);

    public Task<IUserMessage> PendingLocalizedAsync(LocStr str, NadekoInteraction inter = null)
        => SendPendingAsync(GetText(str), inter);

    public Task<IUserMessage> ConfirmLocalizedAsync(LocStr str, NadekoInteraction inter = null)
        => SendConfirmAsync(GetText(str), inter);

    // localized replies
    public Task<IUserMessage> ReplyErrorLocalizedAsync(LocStr str, NadekoInteraction inter = null)
        => SendErrorAsync($"{Format.Bold(ctx.User.ToString())} {GetText(str)}");

    public Task<IUserMessage> ReplyPendingLocalizedAsync(LocStr str, NadekoInteraction inter = null)
        => SendPendingAsync($"{Format.Bold(ctx.User.ToString())} {GetText(str)}");

    public Task<IUserMessage> ReplyConfirmLocalizedAsync(LocStr str, NadekoInteraction inter = null)
        => SendConfirmAsync($"{Format.Bold(ctx.User.ToString())} {GetText(str)}");

    public async Task<bool> PromptUserConfirmAsync(IEmbedBuilder embed)
    {
        embed.WithPendingColor().WithFooter("yes/no");

        var msg = await ctx.Channel.EmbedAsync(embed);
        try
        {
            var input = await GetUserInputAsync(ctx.User.Id, ctx.Channel.Id);
            input = input?.ToUpperInvariant();

            if (input != "YES" && input != "Y")
                return false;

            return true;
        }
        finally
        {
            _ = Task.Run(() => msg.DeleteAsync());
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

            if (await Task.WhenAny(userInputTask.Task, Task.Delay(10000)) != userInputTask.Task)
                return null;

            return await userInputTask.Task;
        }
        finally
        {
            dsc.MessageReceived -= MessageReceived;
        }

        Task MessageReceived(SocketMessage arg)
        {
            _ = Task.Run(() =>
            {
                if (arg is not SocketUserMessage userMsg
                    || userMsg.Channel is not ITextChannel
                    || userMsg.Author.Id != userId
                    || userMsg.Channel.Id != channelId)
                    return Task.CompletedTask;

                if (userInputTask.TrySetResult(arg.Content))
                    userMsg.DeleteAfter(1);

                return Task.CompletedTask;
            });
            return Task.CompletedTask;
        }
    }
}

public abstract class NadekoModule<TService> : NadekoModule
{
    public TService _service { get; set; }
}