#nullable disable
using WizBot.Modules.Searches.Common;

namespace WizBot.Modules.Nsfw;

public interface ISearchImagesService
{
    // ConcurrentDictionary<ulong, Timer> AutoHentaiTimers { get; }
    ConcurrentDictionary<ulong, Timer> AutoBoobTimers { get; }
    ConcurrentDictionary<ulong, Timer> AutoButtTimers { get; }
    Task<UrlReply> Gelbooru(ulong? guildId, bool forceExplicit, string[] tags);
    Task<UrlReply> Danbooru(ulong? guildId, bool forceExplicit, string[] tags);
    Task<UrlReply> Konachan(ulong? guildId, bool forceExplicit, string[] tags);
    Task<UrlReply> Yandere(ulong? guildId, bool forceExplicit, string[] tags);
    Task<UrlReply> Rule34(ulong? guildId, bool forceExplicit, string[] tags);
    Task<UrlReply> E621(ulong? guildId, bool forceExplicit, string[] tags);
    Task<UrlReply> DerpiBooru(ulong? guildId, bool forceExplicit, string[] tags);
    Task<UrlReply> Sankaku(ulong? guildId, bool forceExplicit, string[] tags);
    Task<UrlReply> SafeBooru(ulong? guildId, bool forceExplicit, string[] tags);
    Task<UrlReply> Hentai(ulong? guildId, bool forceExplicit, string[] tags);
    Task<UrlReply> Boobs();
    ValueTask<bool> ToggleBlacklistTag(ulong guildId, string tag);
    ValueTask<string[]> GetBlacklistedTags(ulong guildId);
    Task<UrlReply> Butts();
    // Task<Gallery> GetNhentaiByIdAsync(uint id);
    // Task<Gallery> GetNhentaiBySearchAsync(string search);
}