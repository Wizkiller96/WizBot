#nullable disable
using System.Globalization;
using NadekoBot.Services;

namespace NadekoBot.Common;

/// <summary>
///     Defines methods to retrieve and reload bot strings
/// </summary>
public interface IBotStrings
{
    string GetText(string key, ulong? guildId = null, params object[] data);
    string GetText(string key, CultureInfo locale, params object[] data);
    void Reload();
    ICommandStrings GetCommandStrings(string commandName, ulong? guildId = null);
    ICommandStrings GetCommandStrings(string commandName, CultureInfo cultureInfo);
}