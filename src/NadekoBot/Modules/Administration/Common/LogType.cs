namespace NadekoBot.Modules.Administration
{
    public enum LogType
    {
        Other,
        MessageUpdated,
        MessageDeleted,
        UserJoined,
        UserLeft,
        UserBanned,
        UserUnbanned,
        UserUpdated,
        ChannelCreated,
        ChannelDestroyed,
        ChannelUpdated,
        UserPresence,
        VoicePresence,
        VoicePresenceTTS,
        UserMuted
    }
}