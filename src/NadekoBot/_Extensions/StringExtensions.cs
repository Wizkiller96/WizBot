using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;
using NadekoBot.Common.Yml;

namespace NadekoBot.Extensions;

public static class StringExtensions
{
    public static string PadBoth(this string str, int length)
    {
        var spaces = length - str.Length;
        var padLeft = spaces / 2 + str.Length;
        return str.PadLeft(padLeft).PadRight(length);
    }
        
    public static T MapJson<T>(this string str)
        => JsonConvert.DeserializeObject<T>(str);

    private static readonly HashSet<char> lettersAndDigits = new HashSet<char>(Enumerable.Range(48, 10)
        .Concat(Enumerable.Range(65, 26))
        .Concat(Enumerable.Range(97, 26))
        .Select(x => (char)x));

    public static string StripHTML(this string input)
    {
        return Regex.Replace(input, "<.*?>", String.Empty);
    }

    public static string TrimTo(this string str, int maxLength, bool hideDots = false)
        => hideDots 
            ? str?.Truncate(maxLength, string.Empty)
            : str?.Truncate(maxLength);

    public static string ToTitleCase(this string str)
    {
        var tokens = str.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < tokens.Length; i++)
        {
            var token = tokens[i];
            tokens[i] = token.Substring(0, 1).ToUpperInvariant() + token.Substring(1);
        }

        return string.Join(" ", tokens)
            .Replace(" Of ", " of ")
            .Replace(" The ", " the ");
    }

    //http://www.dotnetperls.com/levenshtein
    public static int LevenshteinDistance(this string s, string t)
    {
        var n = s.Length;
        var m = t.Length;
        var d = new int[n + 1, m + 1];

        // Step 1
        if (n == 0)
        {
            return m;
        }

        if (m == 0)
        {
            return n;
        }

        // Step 2
        for (var i = 0; i <= n; d[i, 0] = i++)
        {
        }

        for (var j = 0; j <= m; d[0, j] = j++)
        {
        }

        // Step 3
        for (var i = 1; i <= n; i++)
        {
            //Step 4
            for (var j = 1; j <= m; j++)
            {
                // Step 5
                var cost = t[j - 1] == s[i - 1] ? 0 : 1;

                // Step 6
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        // Step 7
        return d[n, m];
    }

    public static async Task<Stream> ToStream(this string str)
    {
        var ms = new MemoryStream();
        var sw = new StreamWriter(ms);
        await sw.WriteAsync(str).ConfigureAwait(false);
        await sw.FlushAsync().ConfigureAwait(false);
        ms.Position = 0;
        return ms;
    }

    private static readonly Regex filterRegex = new Regex(@"discord(?:\.gg|\.io|\.me|\.li|(?:app)?\.com\/invite)\/(\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    public static bool IsDiscordInvite(this string str)
        => filterRegex.IsMatch(str);

    public static string Unmention(this string str) => str.Replace("@", "ම", StringComparison.InvariantCulture);

    public static string SanitizeMentions(this string str, bool sanitizeRoleMentions = false)
    {
        str = str.Replace("@everyone", "@everyοne", StringComparison.InvariantCultureIgnoreCase)
            .Replace("@here", "@һere", StringComparison.InvariantCultureIgnoreCase);
        if (sanitizeRoleMentions)
            str = str.SanitizeRoleMentions();

        return str;
    }

    public static string SanitizeRoleMentions(this string str) =>
        str.Replace("<@&", "<ම&", StringComparison.InvariantCultureIgnoreCase);

    public static string SanitizeAllMentions(this string str) =>
        str.SanitizeMentions().SanitizeRoleMentions();

    public static string ToBase64(this string plainText)
    {
        var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(plainTextBytes);
    }

    public static string GetInitials(this string txt, string glue = "") =>
        string.Join(glue, txt.Split(' ').Select(x => x.FirstOrDefault()));

    public static bool IsAlphaNumeric(this string txt) =>
        txt.All(c => lettersAndDigits.Contains(c));

    private static readonly Regex CodePointRegex
        = new Regex(@"(\\U(?<code>[a-zA-Z0-9]{8})|\\u(?<code>[a-zA-Z0-9]{4})|\\x(?<code>[a-zA-Z0-9]{2}))",
            RegexOptions.Compiled);
        
    public static string UnescapeUnicodeCodePoints(this string input)
    { 
        return CodePointRegex.Replace(input, me =>
        {
            var str = me.Groups["code"].Value;
            var newString = YamlHelper.UnescapeUnicodeCodePoint(str);
            return newString;
        });
    }
}