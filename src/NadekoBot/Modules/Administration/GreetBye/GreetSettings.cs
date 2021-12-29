using NadekoBot.Services.Database.Models;

namespace NadekoBot.Services;

public class GreetSettings
{
    public int AutoDeleteGreetMessagesTimer { get; set; }
    public int AutoDeleteByeMessagesTimer { get; set; }

    public ulong GreetMessageChannelId { get; set; }
    public ulong ByeMessageChannelId { get; set; }

    public bool SendDmGreetMessage { get; set; }
    public string? DmGreetMessageText { get; set; }

    public bool SendChannelGreetMessage { get; set; }
    public string? ChannelGreetMessageText { get; set; }

    public bool SendChannelByeMessage { get; set; }
    public string? ChannelByeMessageText { get; set; }

    public bool SendBoostMessage { get; set; }
    public string? BoostMessage { get; set; }
    public int BoostMessageDeleteAfter { get; set; }
    public ulong BoostMessageChannelId { get; set; }

    public static GreetSettings Create(GuildConfig g)
        => new()
        {
            AutoDeleteByeMessagesTimer = g.AutoDeleteByeMessagesTimer,
            AutoDeleteGreetMessagesTimer = g.AutoDeleteGreetMessagesTimer,
            GreetMessageChannelId = g.GreetMessageChannelId,
            ByeMessageChannelId = g.ByeMessageChannelId,
            SendDmGreetMessage = g.SendDmGreetMessage,
            DmGreetMessageText = g.DmGreetMessageText,
            SendChannelGreetMessage = g.SendChannelGreetMessage,
            ChannelGreetMessageText = g.ChannelGreetMessageText,
            SendChannelByeMessage = g.SendChannelByeMessage,
            ChannelByeMessageText = g.ChannelByeMessageText,
            SendBoostMessage = g.SendBoostMessage,
            BoostMessage = g.BoostMessage,
            BoostMessageDeleteAfter = g.BoostMessageDeleteAfter,
            BoostMessageChannelId = g.BoostMessageChannelId
        };
}