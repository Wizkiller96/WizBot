#nullable disable
using WizBot.Db.Models;
using System.Runtime.CompilerServices;

namespace WizBot.Modules.WizBotExpressions;

public static class WizBotExpressionExtensions
{
    private static string ResolveTriggerString(this string str, DiscordSocketClient client)
        => str.Replace("%bot.mention%", client.CurrentUser.Mention, StringComparison.Ordinal);

    public static async Task<IUserMessage> Send(
        this WizBotExpression cr,
        IUserMessage ctx,
        IReplacementService repSvc,
        DiscordSocketClient client,
        IMessageSenderService sender)
    {
        var channel = cr.DmResponse ? await ctx.Author.CreateDMChannelAsync() : ctx.Channel;

        var trigger = cr.Trigger.ResolveTriggerString(client);
        var substringIndex = trigger.Length;
        if (cr.ContainsAnywhere)
        {
            var pos = ctx.Content.AsSpan().GetWordPosition(trigger);
            if (pos == WordPosition.Start)
                substringIndex += 1;
            else if (pos == WordPosition.End)
                substringIndex = ctx.Content.Length;
            else if (pos == WordPosition.Middle)
                substringIndex += ctx.Content.IndexOf(trigger, StringComparison.InvariantCulture);
        }

        var canMentionEveryone = (ctx.Author as IGuildUser)?.GuildPermissions.MentionEveryone ?? true;

        var repCtx = new ReplacementContext(client: client,
                guild: (ctx.Channel as ITextChannel)?.Guild as SocketGuild,
                channel: ctx.Channel,
                users: ctx.Author
            )
            .WithOverride("%target%",
                () => canMentionEveryone
                    ? ctx.Content[substringIndex..].Trim()
                    : ctx.Content[substringIndex..].Trim().SanitizeMentions(true));
        
        var text = SmartText.CreateFrom(cr.Response);
        text = await repSvc.ReplaceAsync(text, repCtx);

        return await sender.Response(channel).Text(text).Sanitize(false).SendAsync();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WordPosition GetWordPosition(this ReadOnlySpan<char> str, in ReadOnlySpan<char> word)
    {
        var wordIndex = str.IndexOf(word, StringComparison.OrdinalIgnoreCase);
        if (wordIndex == -1)
            return WordPosition.None;

        if (wordIndex == 0)
        {
            if (word.Length < str.Length && str.IsValidWordDivider(word.Length))
                return WordPosition.Start;
        }
        else if (wordIndex + word.Length == str.Length)
        {
            if (str.IsValidWordDivider(wordIndex - 1))
                return WordPosition.End;
        }
        else if (str.IsValidWordDivider(wordIndex - 1) && str.IsValidWordDivider(wordIndex + word.Length))
            return WordPosition.Middle;

        return WordPosition.None;
    }

    private static bool IsValidWordDivider(this in ReadOnlySpan<char> str, int index)
    {
        var ch = str[index];
        if (ch is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '1' and <= '9')
            return false;

        return true;
    }
}

public enum WordPosition
{
    None,
    Start,
    Middle,
    End
}