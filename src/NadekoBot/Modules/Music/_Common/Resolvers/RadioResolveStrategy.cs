#nullable disable
using System.Text.RegularExpressions;

namespace NadekoBot.Modules.Music.Resolvers;

public class RadioResolver : IRadioResolver
{
    private readonly Regex _plsRegex = new("File1=(?<url>.*?)\\n", RegexOptions.Compiled);
    private readonly Regex _m3URegex = new("(?<url>^[^#].*)", RegexOptions.Compiled | RegexOptions.Multiline);
    private readonly Regex _asxRegex = new("<ref href=\"(?<url>.*?)\"", RegexOptions.Compiled);
    private readonly Regex _xspfRegex = new("<location>(?<url>.*?)</location>", RegexOptions.Compiled);

    public async Task<ITrackInfo> ResolveByQueryAsync(string query)
    {
        if (IsRadioLink(query))
            query = await HandleStreamContainers(query);

        return new SimpleTrackInfo(query.TrimTo(50),
            query,
            "https://cdn.discordapp.com/attachments/155726317222887425/261850925063340032/1482522097_radio.png",
            TimeSpan.MaxValue,
            MusicPlatform.Radio,
            query);
    }

    public static bool IsRadioLink(string query)
        => (query.StartsWith("http", StringComparison.InvariantCulture)
            || query.StartsWith("ww", StringComparison.InvariantCulture))
           && (query.Contains(".pls") || query.Contains(".m3u") || query.Contains(".asx") || query.Contains(".xspf"));

    private async Task<string> HandleStreamContainers(string query)
    {
        string file = null;
        try
        {
            using var http = new HttpClient();
            file = await http.GetStringAsync(query);
        }
        catch
        {
            return query;
        }

        if (query.Contains(".pls"))
            //File1=http://armitunes.com:8000/
            //Regex.Match(query)
        {
            try
            {
                var m = _plsRegex.Match(file);
                var res = m.Groups["url"]?.ToString();
                return res?.Trim();
            }
            catch
            {
                Log.Warning("Failed reading .pls:\n{PlsFile}", file);
                return null;
            }
        }

        if (query.Contains(".m3u"))
            /* 
    # This is a comment
                   C:\xxx4xx\xxxxxx3x\xx2xxxx\xx.mp3
                   C:\xxx5xx\x6xxxxxx\x7xxxxx\xx.mp3
                */
        {
            try
            {
                var m = _m3URegex.Match(file);
                var res = m.Groups["url"]?.ToString();
                return res?.Trim();
            }
            catch
            {
                Log.Warning("Failed reading .m3u:\n{M3uFile}", file);
                return null;
            }
        }

        if (query.Contains(".asx"))
            //<ref href="http://armitunes.com:8000"/>
        {
            try
            {
                var m = _asxRegex.Match(file);
                var res = m.Groups["url"]?.ToString();
                return res?.Trim();
            }
            catch
            {
                Log.Warning("Failed reading .asx:\n{AsxFile}", file);
                return null;
            }
        }

        if (query.Contains(".xspf"))
            /*
                <?xml version="1.0" encoding="UTF-8"?>
                    <playlist version="1" xmlns="http://xspf.org/ns/0/">
                        <trackList>
                            <track><location>file:///mp3s/song_1.mp3</location></track>
                */
        {
            try
            {
                var m = _xspfRegex.Match(file);
                var res = m.Groups["url"]?.ToString();
                return res?.Trim();
            }
            catch
            {
                Log.Warning("Failed reading .xspf:\n{XspfFile}", file);
                return null;
            }
        }

        return query;
    }
}