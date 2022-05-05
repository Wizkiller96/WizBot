using Humanizer.Localisation;
using Nadeko.Medusa;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NadekoBot.Extensions;

public static class Extensions
{
    private static readonly Regex _urlRegex =
        new(@"^(https?|ftp)://(?<path>[^\s/$.?#].[^\s]*)$", RegexOptions.Compiled);

    public static IEmbedBuilder WithAuthor(this IEmbedBuilder eb, IUser author)
        => eb.WithAuthor(author.ToString()!, author.RealAvatarUrl().ToString());

    public static Task EditAsync(this IUserMessage msg, SmartText text)
        => text switch
        {
            SmartEmbedText set => msg.ModifyAsync(x =>
            {
                x.Embed = set.GetEmbed().Build();
                x.Content = set.PlainText?.SanitizeMentions() ?? "";
            }),
            SmartEmbedTextArray set => msg.ModifyAsync(x =>
            {
                x.Embeds = set.GetEmbedBuilders().Map(eb => eb.Build());
                x.Content = set.Content?.SanitizeMentions() ?? "";
            }),
            SmartPlainText spt => msg.ModifyAsync(x =>
            {
                x.Content = spt.Text.SanitizeMentions();
                x.Embed = null;
            }),
            _ => throw new ArgumentOutOfRangeException(nameof(text))
        };

    public static List<ulong> GetGuildIds(this DiscordSocketClient client)
        => client.Guilds.Select(x => x.Id).ToList();

    /// <summary>
    ///     Generates a string in the format HHH:mm if timespan is &gt;= 2m.
    ///     Generates a string in the format 00:mm:ss if timespan is less than 2m.
    /// </summary>
    /// <param name="span">Timespan to convert to string</param>
    /// <returns>Formatted duration string</returns>
    public static string ToPrettyStringHm(this TimeSpan span)
        => span.Humanize(2, minUnit: TimeUnit.Second);

    public static bool TryGetUrlPath(this string input, out string path)
    {
        var match = _urlRegex.Match(input);
        if (match.Success)
        {
            path = match.Groups["path"].Value;
            return true;
        }

        path = string.Empty;
        return false;
    }

    public static IEmote ToIEmote(this string emojiStr)
        => Emote.TryParse(emojiStr, out var maybeEmote) ? maybeEmote : new Emoji(emojiStr);


    /// <summary>
    ///     First 10 characters of teh bot token.
    /// </summary>
    public static string RedisKey(this IBotCredentials bc)
        => bc.Token[..10];

    public static bool IsAuthor(this IMessage msg, IDiscordClient client)
        => msg.Author?.Id == client.CurrentUser.Id;

    public static string RealSummary(
        this CommandInfo cmd,
        IBotStrings strings,
        IMedusaLoaderService medusae,
        CultureInfo culture,
        string prefix)
    {
        string description;
        if (cmd.Remarks?.StartsWith("medusa///") ?? false)
        {
            // command method name is kept in Summary
            // medusa///<medusa-name-here> is kept in remarks
            // this way I can find the name of the medusa, and then name of the command for which
            // the description should be loaded
            var medusaName = cmd.Remarks.Split("///")[1];
            description = medusae.GetCommandDescription(medusaName, cmd.Summary, culture);
        }
        else
        {
            description = strings.GetCommandStrings(cmd.Summary, culture).Desc;
        }
        
        return string.Format(description, prefix);
    }

    public static string[] RealRemarksArr(
        this CommandInfo cmd,
        IBotStrings strings,
        IMedusaLoaderService medusae,
        CultureInfo culture,
        string prefix)
    {
        string[] args;
        if (cmd.Remarks?.StartsWith("medusa///") ?? false)
        {
            // command method name is kept in Summary
            // medusa///<medusa-name-here> is kept in remarks
            // this way I can find the name of the medusa,
            // and command for which data should be loaded
            var medusaName = cmd.Remarks.Split("///")[1];
            args = medusae.GetCommandExampleArgs(medusaName, cmd.Summary, culture);
        }
        else
        {
            args = strings.GetCommandStrings(cmd.Summary, culture).Args;
        }
        
        return args.Map(arg => GetFullUsage(cmd.Aliases.First(), arg, prefix));
    }

    private static string GetFullUsage(string commandName, string args, string prefix)
        => $"{prefix}{commandName} {string.Format(args, prefix)}".TrimEnd();

    public static IEmbedBuilder AddPaginatedFooter(this IEmbedBuilder embed, int curPage, int? lastPage)
    {
        if (lastPage is not null)
            return embed.WithFooter($"{curPage + 1} / {lastPage + 1}");
        return embed.WithFooter(curPage.ToString());
    }

    public static IEmbedBuilder WithOkColor(this IEmbedBuilder eb)
        => eb.WithColor(EmbedColor.Ok);

    public static IEmbedBuilder WithPendingColor(this IEmbedBuilder eb)
        => eb.WithColor(EmbedColor.Pending);

    public static IEmbedBuilder WithErrorColor(this IEmbedBuilder eb)
        => eb.WithColor(EmbedColor.Error);

    public static HttpClient AddFakeHeaders(this HttpClient http)
    {
        AddFakeHeaders(http.DefaultRequestHeaders);
        return http;
    }

    public static void AddFakeHeaders(this HttpHeaders dict)
    {
        dict.Clear();
        dict.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        dict.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/535.1 (KHTML, like Gecko) Chrome/14.0.835.202 Safari/535.1");
    }

    public static IMessage DeleteAfter(this IUserMessage msg, int seconds, ILogCommandService? logService = null)
    {
        Task.Run(async () =>
        {
            await Task.Delay(seconds * 1000);
            if (logService is not null)
                logService.AddDeleteIgnore(msg.Id);

            try { await msg.DeleteAsync(); }
            catch { }
        });
        return msg;
    }

    public static ModuleInfo GetTopLevelModule(this ModuleInfo module)
    {
        while (module.Parent is not null)
            module = module.Parent;

        return module;
    }

    public static async Task<IEnumerable<IGuildUser>> GetMembersAsync(this IRole role)
    {
        var users = await role.Guild.GetUsersAsync(CacheMode.CacheOnly);
        return users.Where(u => u.RoleIds.Contains(role.Id));
    }

    public static string ToJson<T>(this T any, JsonSerializerOptions? options = null)
        => JsonSerializer.Serialize(any, options);

    public static Stream ToStream(this IEnumerable<byte> bytes, bool canWrite = false)
    {
        var ms = new MemoryStream(bytes as byte[] ?? bytes.ToArray(), canWrite);
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }

    public static IEnumerable<IRole> GetRoles(this IGuildUser user)
        => user.RoleIds.Select(r => user.Guild.GetRole(r)).Where(r => r is not null);

    public static bool IsImage(this HttpResponseMessage msg)
        => IsImage(msg, out _);

    public static bool IsImage(this HttpResponseMessage msg, out string? mimeType)
    {
        mimeType = msg.Content.Headers.ContentType?.MediaType;
        if (mimeType is "image/png" or "image/jpeg" or "image/gif")
            return true;

        return false;
    }

    public static long GetContentLength(this HttpResponseMessage msg)
        => msg.Content.Headers.ContentLength is long length
            ? length
            : long.MaxValue;
}