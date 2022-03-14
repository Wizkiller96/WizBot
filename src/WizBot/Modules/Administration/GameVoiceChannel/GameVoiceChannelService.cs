#nullable disable
using WizBot.Db;

namespace WizBot.Modules.Administration.Services;

public class GameVoiceChannelService : INService
{
    public ConcurrentHashSet<ulong> GameVoiceChannels { get; }

    private readonly DbService _db;
    private readonly DiscordSocketClient _client;

    public GameVoiceChannelService(DiscordSocketClient client, DbService db, Bot bot)
    {
        _db = db;
        _client = client;

        GameVoiceChannels = new(bot.AllGuildConfigs
                                   .Where(gc => gc.GameVoiceChannel is not null)
                                   .Select(gc => gc.GameVoiceChannel!.Value));

        _client.UserVoiceStateUpdated += OnUserVoiceStateUpdated;
        _client.PresenceUpdated += OnPresenceUpdate;
    }

    private Task OnPresenceUpdate(SocketUser socketUser, SocketPresence before, SocketPresence after)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (socketUser is not SocketGuildUser newUser)
                    return;
                // if the user is in the voice channel and that voice channel is gvc

                if (newUser.VoiceChannel is not { } vc
                    || !GameVoiceChannels.Contains(vc.Id))
                    return;

                //if the activity has changed, and is a playi1ng activity
                foreach (var activity in after.Activities)
                {
                    if (activity is { Type: ActivityType.Playing })
                        //trigger gvc
                    {
                        if (await TriggerGvc(newUser, activity.Name))
                            return;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error running GuildMemberUpdated in gvc");
            }
        });
        return Task.CompletedTask;
    }

    public ulong? ToggleGameVoiceChannel(ulong guildId, ulong vchId)
    {
        ulong? id;
        using var uow = _db.GetDbContext();
        var gc = uow.GuildConfigsForId(guildId, set => set);

        if (gc.GameVoiceChannel == vchId)
        {
            GameVoiceChannels.TryRemove(vchId);
            id = gc.GameVoiceChannel = null;
        }
        else
        {
            if (gc.GameVoiceChannel is not null)
                GameVoiceChannels.TryRemove(gc.GameVoiceChannel.Value);
            GameVoiceChannels.Add(vchId);
            id = gc.GameVoiceChannel = vchId;
        }

        uow.SaveChanges();
        return id;
    }

    private Task OnUserVoiceStateUpdated(SocketUser usr, SocketVoiceState oldState, SocketVoiceState newState)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (usr is not SocketGuildUser gUser)
                    return;

                if (newState.VoiceChannel is null)
                    return;

                if (!GameVoiceChannels.Contains(newState.VoiceChannel.Id))
                    return;

                foreach (var game in gUser.Activities.Select(x => x.Name))
                {
                    if (await TriggerGvc(gUser, game))
                        return;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error running VoiceStateUpdate in gvc");
            }
        });

        return Task.CompletedTask;
    }

    private async Task<bool> TriggerGvc(SocketGuildUser gUser, string game)
    {
        if (string.IsNullOrWhiteSpace(game))
            return false;

        game = game.TrimTo(50)!.ToLowerInvariant();
        var vch = gUser.Guild.VoiceChannels.FirstOrDefault(x => x.Name.ToLowerInvariant() == game);

        if (vch is null)
            return false;

        await Task.Delay(1000);
        await gUser.ModifyAsync(gu => gu.Channel = vch);
        return true;
    }
}