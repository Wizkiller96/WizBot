#nullable disable
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Modules.Gambling.Common.Events;

public class GameStatusEvent : ICurrencyEvent
{
    private readonly DiscordSocketClient _client;
    private readonly IGuild _guild;
    private IUserMessage _msg;
    private readonly ICurrencyService _cs;
    private readonly long _amount;

    private long PotSize { get; set; }
    public bool Stopped { get; private set; }
    public bool PotEmptied { get; private set; } = false;

    private readonly Func<CurrencyEvent.Type, EventOptions, long, IEmbedBuilder> _embedFunc;
    private readonly bool _isPotLimited;
    private readonly ITextChannel _channel;
    private readonly ConcurrentHashSet<ulong> _awardedUsers = new();
    private readonly ConcurrentQueue<ulong> _toAward = new();
    private readonly Timer _t;
    private readonly Timer _timeout = null;
    private readonly EventOptions _opts;

    private readonly string _code;

    public event Func<ulong, Task> OnEnded;

    private readonly char[] _sneakyGameStatusChars = Enumerable.Range(48, 10)
        .Concat(Enumerable.Range(65, 26))
        .Concat(Enumerable.Range(97, 26))
        .Select(x => (char)x)
        .ToArray();

    public GameStatusEvent(DiscordSocketClient client, ICurrencyService cs,SocketGuild g, ITextChannel ch,
        EventOptions opt, Func<CurrencyEvent.Type, EventOptions, long, IEmbedBuilder> embedFunc)
    {
        _client = client;
        _guild = g;
        _cs = cs;
        _amount = opt.Amount;
        PotSize = opt.PotSize;
        _embedFunc = embedFunc;
        _isPotLimited = PotSize > 0;
        _channel = ch;
        _opts = opt;
        // generate code
        _code = new(_sneakyGameStatusChars.Shuffle().Take(5).ToArray());

        _t = new(OnTimerTick, null, Timeout.InfiniteTimeSpan, TimeSpan.FromSeconds(2));
        if (_opts.Hours > 0)
        {
            _timeout = new(EventTimeout, null, TimeSpan.FromHours(_opts.Hours), Timeout.InfiniteTimeSpan);
        }
    }

    private void EventTimeout(object state)
    {
        var _ = StopEvent();
    }

    private async void OnTimerTick(object state)
    {
        var potEmpty = PotEmptied;
        var toAward = new List<ulong>();
        while (_toAward.TryDequeue(out var x))
        {
            toAward.Add(x);
        }

        if (!toAward.Any())
            return;

        try
        {
            await _cs.AddBulkAsync(toAward,
                toAward.Select(x => "GameStatus Event"),
                toAward.Select(x => _amount),
                gamble: true).ConfigureAwait(false);

            if (_isPotLimited)
            {
                await _msg.ModifyAsync(m =>
                {
                    m.Embed = GetEmbed(PotSize).Build();
                }, new() { RetryMode = RetryMode.AlwaysRetry }).ConfigureAwait(false);
            }

            Log.Information("Awarded {0} users {1} currency.{2}",
                toAward.Count,
                _amount,
                _isPotLimited ? $" {PotSize} left." : "");

            if (potEmpty)
            {
                var _ = StopEvent();
            }

        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error in OnTimerTick in gamestatusevent");
        }
    }

    public async Task StartEvent()
    {
        _msg = await _channel.EmbedAsync(GetEmbed(_opts.PotSize)).ConfigureAwait(false);
        await _client.SetGameAsync(_code).ConfigureAwait(false);
        _client.MessageDeleted += OnMessageDeleted;
        _client.MessageReceived += HandleMessage;
        _t.Change(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
    }

    private IEmbedBuilder GetEmbed(long pot)
        => _embedFunc(CurrencyEvent.Type.GameStatus, _opts, pot);

    private async Task OnMessageDeleted(Cacheable<IMessage, ulong> msg, Cacheable<IMessageChannel, ulong> cacheable)
    {
        if (msg.Id == _msg.Id)
        {
            await StopEvent().ConfigureAwait(false);
        }
    }

    private readonly object stopLock = new();
    public async Task StopEvent()
    {
        await Task.Yield();
        lock (stopLock)
        {
            if (Stopped)
                return;
            Stopped = true;
            _client.MessageDeleted -= OnMessageDeleted;
            _client.MessageReceived -= HandleMessage;
            _client.SetGameAsync(null);
            _t.Change(Timeout.Infinite, Timeout.Infinite);
            _timeout?.Change(Timeout.Infinite, Timeout.Infinite);
            try { var _ = _msg.DeleteAsync(); } catch { }
            var os = OnEnded(_guild.Id);
        }
    }

    private Task HandleMessage(SocketMessage msg)
    {
        var _ = Task.Run(async () =>
        {
            if (msg.Author is not IGuildUser gu // no unknown users, as they could be bots, or alts
                || gu.IsBot // no bots
                || msg.Content != _code // code has to be the same
                || (DateTime.UtcNow - gu.CreatedAt).TotalDays <= 5) // no recently created accounts
            {
                return;
            }
            // there has to be money left in the pot
            // and the user wasn't rewarded
            if (_awardedUsers.Add(msg.Author.Id) && TryTakeFromPot())
            {
                _toAward.Enqueue(msg.Author.Id);
                if (_isPotLimited && PotSize < _amount)
                    PotEmptied = true;
            }

            try
            {
                await msg.DeleteAsync(new()
                {
                    RetryMode = RetryMode.AlwaysFail
                });
            }
            catch { }
        });
        return Task.CompletedTask;
    }

    private readonly object potLock = new();
    private bool TryTakeFromPot()
    {
        if (_isPotLimited)
        {
            lock (potLock)
            {
                if (PotSize < _amount)
                    return false;

                PotSize -= _amount;
                return true;
            }
        }
        return true;
    }
}
