﻿#nullable disable
using System.Text;
using CommandLine;

namespace NadekoBot.Modules.Games.Common;

public class TicTacToe
{
    public class Options : INadekoCommandOptions
    {
        public void NormalizeOptions()
        {
            if (TurnTimer is < 5 or > 60)
                TurnTimer = 15;
        }

        [Option('t', "turn-timer", Required = false, Default = 15, HelpText = "Turn time in seconds. Default 15.")]
        public int TurnTimer { get; set; } = 15;
    }

    private enum Phase
    {
        Starting,
        Started,
        Ended
    }

    private readonly ITextChannel _channel;
    private readonly IGuildUser[] _users;
    private readonly int?[,] _state;
    private Phase _phase;
    private int _curUserIndex;
    private readonly SemaphoreSlim _moveLock;

    private IGuildUser _winner;

    private readonly string[] _numbers = { ":one:", ":two:", ":three:", ":four:", ":five:", ":six:", ":seven:", ":eight:", ":nine:" };

    public event Action<TicTacToe> OnEnded;

    private IUserMessage _previousMessage;
    private Timer _timeoutTimer;
    private readonly IBotStrings _strings;
    private readonly DiscordSocketClient _client;
    private readonly Options _options;
    private readonly IEmbedBuilderService _eb;

    public TicTacToe(IBotStrings strings, DiscordSocketClient client, ITextChannel channel,
        IGuildUser firstUser, Options options, IEmbedBuilderService eb)
    {
        _channel = channel;
        _strings = strings;
        _client = client;
        _options = options;
        _eb = eb;

        _users = new[] { firstUser, null };
        _state = new int?[,] {
            { null, null, null },
            { null, null, null },
            { null, null, null },
        };

        _phase = Phase.Starting;
        _moveLock = new(1, 1);
    }

    private string GetText(LocStr key)
        => _strings.GetText(key, _channel.GuildId);

    public string GetState()
    {
        var sb = new StringBuilder();
        for (var i = 0; i < _state.GetLength(0); i++)
        {
            for (var j = 0; j < _state.GetLength(1); j++)
            {
                sb.Append(_state[i, j] is null ? _numbers[(i * 3) + j] : GetIcon(_state[i, j]));
                if (j < _state.GetLength(1) - 1)
                    sb.Append("┃");
            }
            if (i < _state.GetLength(0) - 1)
                sb.AppendLine("\n──────────");
        }

        return sb.ToString();
    }

    public IEmbedBuilder GetEmbed(string title = null)
    {
        var embed = _eb.Create()
            .WithOkColor()
            .WithDescription(Environment.NewLine + GetState())
            .WithAuthor(GetText(strs.vs(_users[0], _users[1])));

        if (!string.IsNullOrWhiteSpace(title))
            embed.WithTitle(title);

        if (_winner is null)
        {
            if (_phase == Phase.Ended)
                embed.WithFooter(GetText(strs.ttt_no_moves));
            else
                embed.WithFooter(GetText(strs.ttt_users_move(_users[_curUserIndex])));
        }
        else
            embed.WithFooter(GetText(strs.ttt_has_won(_winner)));

        return embed;
    }

    private static string GetIcon(int? val)
    {
        switch (val)
        {
            case 0:
                return "❌";
            case 1:
                return "⭕";
            case 2:
                return "❎";
            case 3:
                return "🅾";
            default:
                return "⬛";
        }
    }

    public async Task Start(IGuildUser user)
    {
        if (_phase is Phase.Started or Phase.Ended)
        {
            await _channel.SendErrorAsync(_eb, user.Mention + GetText(strs.ttt_already_running));
            return;
        }
        else if (_users[0] == user)
        {
            await _channel.SendErrorAsync(_eb, user.Mention + GetText(strs.ttt_against_yourself));
            return;
        }

        _users[1] = user;

        _phase = Phase.Started;

        _timeoutTimer = new(async _ =>
        {
            await _moveLock.WaitAsync();
            try
            {
                if (_phase == Phase.Ended)
                    return;

                _phase = Phase.Ended;
                if (_users[1] != null)
                {
                    _winner = _users[_curUserIndex ^= 1];
                    var del = _previousMessage?.DeleteAsync();
                    try
                    {
                        await _channel.EmbedAsync(GetEmbed(GetText(strs.ttt_time_expired)));
                        if (del != null)
                            await del;
                    }
                    catch { }
                }

                OnEnded?.Invoke(this);
            }
            catch { }
            finally
            {
                _moveLock.Release();
            }
        }, null, _options.TurnTimer * 1000, Timeout.Infinite);

        _client.MessageReceived += Client_MessageReceived;


        _previousMessage = await _channel.EmbedAsync(GetEmbed(GetText(strs.game_started)));
    }

    private bool IsDraw()
    {
        for (var i = 0; i < 3; i++)
        {
            for (var j = 0; j < 3; j++)
            {
                if (_state[i, j] is null)
                    return false;
            }
        }
        return true;
    }

    private Task Client_MessageReceived(SocketMessage msg)
    {
        var _ = Task.Run(async () =>
        {
            await _moveLock.WaitAsync();
            try
            {
                var curUser = _users[_curUserIndex];
                if (_phase == Phase.Ended || msg.Author?.Id != curUser.Id)
                    return;

                if (int.TryParse(msg.Content, out var index) &&
                    --index >= 0 &&
                    index <= 9 &&
                    _state[index / 3, index % 3] is null)
                {
                    _state[index / 3, index % 3] = _curUserIndex;

                    // i'm lazy
                    if (_state[index / 3, 0] == _state[index / 3, 1] && _state[index / 3, 1] == _state[index / 3, 2])
                    {
                        _state[index / 3, 0] = _curUserIndex + 2;
                        _state[index / 3, 1] = _curUserIndex + 2;
                        _state[index / 3, 2] = _curUserIndex + 2;

                        _phase = Phase.Ended;
                    }
                    else if (_state[0, index % 3] == _state[1, index % 3] && _state[1, index % 3] == _state[2, index % 3])
                    {
                        _state[0, index % 3] = _curUserIndex + 2;
                        _state[1, index % 3] = _curUserIndex + 2;
                        _state[2, index % 3] = _curUserIndex + 2;

                        _phase = Phase.Ended;
                    }
                    else if (_curUserIndex == _state[0, 0] && _state[0, 0] == _state[1, 1] && _state[1, 1] == _state[2, 2])
                    {
                        _state[0, 0] = _curUserIndex + 2;
                        _state[1, 1] = _curUserIndex + 2;
                        _state[2, 2] = _curUserIndex + 2;

                        _phase = Phase.Ended;
                    }
                    else if (_curUserIndex == _state[0, 2] && _state[0, 2] == _state[1, 1] && _state[1, 1] == _state[2, 0])
                    {
                        _state[0, 2] = _curUserIndex + 2;
                        _state[1, 1] = _curUserIndex + 2;
                        _state[2, 0] = _curUserIndex + 2;

                        _phase = Phase.Ended;
                    }
                    var reason = string.Empty;

                    if (_phase == Phase.Ended) // if user won, stop receiving moves
                    {
                        reason = GetText(strs.ttt_matched_three);
                        _winner = _users[_curUserIndex];
                        _client.MessageReceived -= Client_MessageReceived;
                        OnEnded?.Invoke(this);
                    }
                    else if (IsDraw())
                    {
                        reason = GetText(strs.ttt_a_draw);
                        _phase = Phase.Ended;
                        _client.MessageReceived -= Client_MessageReceived;
                        OnEnded?.Invoke(this);
                    }

                    var sendstate = Task.Run(async () =>
                    {
                        var del1 = msg.DeleteAsync();
                        var del2 = _previousMessage?.DeleteAsync();
                        try { _previousMessage = await _channel.EmbedAsync(GetEmbed(reason)); } catch { }
                        try { await del1; } catch { }
                        try { if (del2 != null) await del2; } catch { }
                    });
                    _curUserIndex ^= 1;

                    _timeoutTimer.Change(_options.TurnTimer * 1000, Timeout.Infinite);
                }
            }
            finally
            {
                _moveLock.Release();
            }
        });

        return Task.CompletedTask;
    }
}
