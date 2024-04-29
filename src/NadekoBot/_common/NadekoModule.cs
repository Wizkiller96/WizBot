#nullable disable
using System.Globalization;

// ReSharper disable InconsistentNaming

namespace NadekoBot.Common;

[UsedImplicitly(ImplicitUseTargetFlags.Default
                | ImplicitUseTargetFlags.WithInheritors
                | ImplicitUseTargetFlags.WithMembers)]
public abstract class NadekoModule : ModuleBase
{
    protected CultureInfo Culture { get; set; }

    // Injected by Discord.net
    public IBotStrings Strings { get; set; }
    public ICommandHandler _cmdHandler { get; set; }
    public ILocalization _localization { get; set; }
    public IEmbedBuilderService _eb { get; set; }
    public INadekoInteractionService _inter { get; set; }
    public IReplacementService repSvc { get; set; }
    public IMessageSenderService _sender { get; set; }

    protected string prefix
        => _cmdHandler.GetPrefix(ctx.Guild);

    protected ICommandContext ctx
        => Context;

    public ResponseBuilder Response()
        => new ResponseBuilder(Strings, _eb)
            .Context(ctx);

    protected override void BeforeExecute(CommandInfo command)
        => Culture = _localization.GetCultureInfo(ctx.Guild?.Id);

    protected string GetText(in LocStr data)
        => Strings.GetText(data, Culture);

    // localized normal
    public async Task<bool> PromptUserConfirmAsync(EmbedBuilder embed)
    {
        embed.WithPendingColor()
             .WithFooter("yes/no");

        var msg = await Response().Embed(embed).SendAsync();
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