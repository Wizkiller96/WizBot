#nullable disable
using Humanizer.Localisation;
using NadekoBot.Common.ModuleBehaviors;
using System.Diagnostics;

namespace NadekoBot.Services;

public class StatsService : IStatsService, IReadyExecutor, INService, IDisposable
{
    public const string BOT_VERSION = "4.0.1";

    public string Author
        => "Kwoth#2452";

    public string Library
        => "Discord.Net";

    public double MessagesPerSecond
        => MessageCounter / GetUptime().TotalSeconds;

    public long TextChannels
        => Interlocked.Read(ref textChannels);

    public long VoiceChannels
        => Interlocked.Read(ref voiceChannels);

    public long MessageCounter
        => Interlocked.Read(ref messageCounter);

    public long CommandsRan
        => Interlocked.Read(ref commandsRan);

    private readonly Process _currentProcess = Process.GetCurrentProcess();
    private readonly DiscordSocketClient _client;
    private readonly IBotCredentials _creds;
    private readonly DateTime _started;

    private long textChannels;
    private long voiceChannels;
    private long messageCounter;
    private long commandsRan;

    private readonly IHttpClientFactory _httpFactory;

    public StatsService(
        DiscordSocketClient client,
        CommandHandler cmdHandler,
        IBotCredentials creds,
        IHttpClientFactory factory)
    {
        _client = client;
        _creds = creds;
        _httpFactory = factory;

        _started = DateTime.UtcNow;
        _client.MessageReceived += _ => Task.FromResult(Interlocked.Increment(ref messageCounter));
        cmdHandler.CommandExecuted += (_, _) => Task.FromResult(Interlocked.Increment(ref commandsRan));

        _client.ChannelCreated += c =>
        {
            _ = Task.Run(() =>
            {
                if (c is ITextChannel)
                    Interlocked.Increment(ref textChannels);
                else if (c is IVoiceChannel)
                    Interlocked.Increment(ref voiceChannels);
            });

            return Task.CompletedTask;
        };

        _client.ChannelDestroyed += c =>
        {
            _ = Task.Run(() =>
            {
                if (c is ITextChannel)
                    Interlocked.Decrement(ref textChannels);
                else if (c is IVoiceChannel)
                    Interlocked.Decrement(ref voiceChannels);
            });

            return Task.CompletedTask;
        };

        _client.GuildAvailable += g =>
        {
            _ = Task.Run(() =>
            {
                var tc = g.Channels.Count(cx => cx is ITextChannel);
                var vc = g.Channels.Count - tc;
                Interlocked.Add(ref textChannels, tc);
                Interlocked.Add(ref voiceChannels, vc);
            });
            return Task.CompletedTask;
        };

        _client.JoinedGuild += g =>
        {
            _ = Task.Run(() =>
            {
                var tc = g.Channels.Count(cx => cx is ITextChannel);
                var vc = g.Channels.Count - tc;
                Interlocked.Add(ref textChannels, tc);
                Interlocked.Add(ref voiceChannels, vc);
            });
            return Task.CompletedTask;
        };

        _client.GuildUnavailable += g =>
        {
            _ = Task.Run(() =>
            {
                var tc = g.Channels.Count(cx => cx is ITextChannel);
                var vc = g.Channels.Count - tc;
                Interlocked.Add(ref textChannels, -tc);
                Interlocked.Add(ref voiceChannels, -vc);
            });

            return Task.CompletedTask;
        };

        _client.LeftGuild += g =>
        {
            _ = Task.Run(() =>
            {
                var tc = g.Channels.Count(cx => cx is ITextChannel);
                var vc = g.Channels.Count - tc;
                Interlocked.Add(ref textChannels, -tc);
                Interlocked.Add(ref voiceChannels, -vc);
            });

            return Task.CompletedTask;
        };
    }

    public async Task OnReadyAsync()
    {
        var guilds = _client.Guilds;
        textChannels = guilds.Sum(g => g.Channels.Count(cx => cx is ITextChannel));
        voiceChannels = guilds.Sum(g => g.Channels.Count(cx => cx is IVoiceChannel));

        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            if (string.IsNullOrWhiteSpace(_creds.BotListToken))
                continue;

            try
            {
                using var http = _httpFactory.CreateClient();
                using var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "shard_count", _creds.TotalShards.ToString() },
                    { "shard_id", _client.ShardId.ToString() },
                    { "server_count", _client.Guilds.Count().ToString() }
                });
                content.Headers.Clear();
                content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                http.DefaultRequestHeaders.Add("Authorization", _creds.BotListToken);

                using var res = await http.PostAsync(
                    new Uri($"https://discordbots.org/api/bots/{_client.CurrentUser.Id}/stats"),
                    content);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in botlist post");
            }
        } while (await timer.WaitForNextTickAsync());
    }

    public TimeSpan GetUptime()
        => DateTime.UtcNow - _started;

    public string GetUptimeString(string separator = ", ")
    {
        var time = GetUptime();
        return time.Humanize(3, maxUnit: TimeUnit.Day, minUnit: TimeUnit.Minute);
    }

    public double GetPrivateMemory()
    {
        _currentProcess.Refresh();
        return _currentProcess.PrivateMemorySize64 / (double)1.MiB();
    }

    public void Dispose()
    {
        _currentProcess.Dispose();
        GC.SuppressFinalize(this);
    }
}