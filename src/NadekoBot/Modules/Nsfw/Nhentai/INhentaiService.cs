using NadekoBot.Modules.Searches.Common;

namespace NadekoBot.Modules.Nsfw;

public interface INhentaiService
{
    Task<Gallery?> GetAsync(uint id);
    Task<IReadOnlyList<uint>> GetIdsBySearchAsync(string search);
}