﻿using WizBot.Services.Database.Models;

namespace WizBot.Modules.Administration;

public sealed class DummyLogCommandService : ILogCommandService
{
    public void AddDeleteIgnore(ulong xId)
    {
    }

    public Task LogServer(ulong guildId, ulong channelId, bool actionValue)
        => Task.CompletedTask;

    public bool LogIgnore(ulong guildId, ulong itemId, IgnoredItemType itemType)
        => false;

    public LogSetting? GetGuildLogSettings(ulong guildId)
        => default;

    public bool Log(ulong guildId, ulong? channelId, LogType type)
        => false;
}