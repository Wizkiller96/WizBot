using Discord;
using Discord.WebSocket;
using NadekoBot.Common.Replacements;
using NadekoBot.Services.Database.Models;
using NadekoBot.Extensions;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NadekoBot.Modules.CustomReactions.Extensions
{
    public static class CustomReactionExtensions
    {
        private static string ResolveTriggerString(this string str, DiscordSocketClient client)
            => str.Replace("%bot.mention%", client.CurrentUser.Mention, StringComparison.Ordinal);

        public static async Task<IUserMessage> Send(this CustomReaction cr, IUserMessage ctx,
            DiscordSocketClient client, bool sanitize)
        {
            var channel = cr.DmResponse
                ? await ctx.Author.GetOrCreateDMChannelAsync().ConfigureAwait(false)
                : ctx.Channel;

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

            var rep = new ReplacementBuilder()
                .WithDefault(ctx.Author, ctx.Channel, (ctx.Channel as ITextChannel)?.Guild as SocketGuild, client)
                .WithOverride("%target%", () => canMentionEveryone
                    ? ctx.Content.Substring(substringIndex).Trim()
                    : ctx.Content.Substring(substringIndex).Trim().SanitizeMentions(true))
                .Build();

            var text = SmartText.CreateFrom(cr.Response);
            text = rep.Replace(text);

            return await channel.SendAsync(text, sanitize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static WordPosition GetWordPosition(this ReadOnlySpan<char> str, in ReadOnlySpan<char> word)
        {
            var wordIndex = str.IndexOf(word, StringComparison.OrdinalIgnoreCase);
            if (wordIndex == -1)
                return WordPosition.None;

            if (wordIndex == 0)
            {
                if (word.Length < str.Length && str.isValidWordDivider(word.Length))
                    return WordPosition.Start;
            }
            else if ((wordIndex + word.Length) == str.Length)
            {
                if (str.isValidWordDivider(wordIndex - 1))
                    return WordPosition.End;
            }
            else if (str.isValidWordDivider(wordIndex - 1) && str.isValidWordDivider(wordIndex + word.Length))
                return WordPosition.Middle;

            return WordPosition.None;
        }

        private static bool isValidWordDivider(this in ReadOnlySpan<char> str, int index)
        {
            var ch = str[index];
            if (ch >= 'a' && ch <= 'z')
                return false;
            if (ch >= 'A' && ch <= 'Z')
                return false;
            if (ch >= '1' && ch <= '9')
                return false;

            return true;
        }
    }

    public enum WordPosition
    {
        None,
        Start,
        Middle,
        End,
    }
}