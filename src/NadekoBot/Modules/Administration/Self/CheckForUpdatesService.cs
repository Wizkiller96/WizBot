using System.Net.Http.Json;
using System.Text;
using NadekoBot.Common.ModuleBehaviors;

namespace NadekoBot.Modules.Administration.Self;

public sealed class CheckForUpdatesService : INService, IReadyExecutor
{
    private readonly BotConfigService _bcs;
    private readonly IBotCredsProvider _bcp;
    private readonly IHttpClientFactory _httpFactory;
    private readonly DiscordSocketClient _client;
    private readonly IEmbedBuilderService _ebs;

    public CheckForUpdatesService(BotConfigService bcs, IBotCredsProvider bcp, IHttpClientFactory httpFactory,
        DiscordSocketClient client, IEmbedBuilderService ebs)
    {
        _bcs = bcs;
        _bcp = bcp;
        _httpFactory = httpFactory;
        _client = client;
        _ebs = ebs;
    }
    
    public async Task OnReadyAsync()
    {
        if (_client.ShardId != 0)
            return;
        
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync())
        {
            var conf = _bcs.Data;

            if (!conf.CheckForUpdates)
                continue;
            
            try
            {
                const string URL = "https://cdn.nadeko.bot/cmds/versions.json";
                using var http = _httpFactory.CreateClient();
                var versions = await http.GetFromJsonAsync<string[]>(URL);

                if (versions is null)
                    continue;
                
                var latest = versions[0];
                var latestVersion = Version.Parse(latest);
                var lastKnownVersion = GetLastKnownVersion();

                if (lastKnownVersion is null)
                {
                    UpdateLastKnownVersion(latestVersion);
                    continue;
                }
                
                if (latestVersion > lastKnownVersion)
                {
                    UpdateLastKnownVersion(latestVersion);
                    
                    // pull changelog
                    var changelog = await http.GetStringAsync("https://gitlab.com/Kwoth/nadekobot/-/raw/v4/CHANGELOG.md");

                    var thisVersionChangelog = GetVersionChangelog(latestVersion, changelog);

                    if (string.IsNullOrWhiteSpace(thisVersionChangelog))
                    {
                        Log.Warning("New version {BotVersion} was found but changelog is unavailable",
                            thisVersionChangelog);
                        continue;
                    }

                    var creds = _bcp.GetCreds();
                    await creds.OwnerIds
                        .Select(async x =>
                        {
                            var user = await _client.GetUserAsync(x);
                            if (user is null)
                                return;

                            var eb = _ebs.Create()
                                .WithOkColor()
                                .WithAuthor($"NadekoBot v{latestVersion} Released!")
                                .WithTitle("Changelog")
                                .WithUrl("https://gitlab.com/Kwoth/nadekobot/-/blob/v4/CHANGELOG.md")
                                .WithDescription(thisVersionChangelog.TrimTo(4096))
                                .WithFooter("You may disable these messages by typing '.conf bot checkforupdates false'");

                            await user.EmbedAsync(eb);
                        }).WhenAll();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while checking for new bot release: {ErrorMessage}", ex.Message);
            }
        }
    }

    private string? GetVersionChangelog(Version latestVersion, string changelog)
    {
        var clSpan = changelog.AsSpan();

        var sb = new StringBuilder();
        var started = false;
        foreach (var line in clSpan.EnumerateLines())
        {
            // if we're at the current version, keep reading lines and adding to the output
            if (started)
            {
                // if we got to previous version, end
                if (line.StartsWith("## ["))
                    break;
                
                // if we're reading a new segment, reformat it to print it better to discord
                if (line.StartsWith("### "))
                {
                    sb.AppendLine(Format.Bold(line.ToString()));
                }
                else
                {
                    sb.AppendLine(line.ToString());
                }

                continue;
            }

            if (line.StartsWith($"## [{latestVersion.ToString()}]"))
            {
                started = true;
                continue;
            }
        }

        return sb.ToString();
    }

    private const string LAST_KNOWN_VERSION_PATH = "data/last_known_version.txt";
    private Version? GetLastKnownVersion()
    {
        if (!File.Exists(LAST_KNOWN_VERSION_PATH))
            return null;
        
        return Version.TryParse(File.ReadAllText(LAST_KNOWN_VERSION_PATH), out var ver)
            ? ver
            : null;
    }

    private void UpdateLastKnownVersion(Version version)
    {
        File.WriteAllText("data/last_known_version.txt", version.ToString());
    }
}