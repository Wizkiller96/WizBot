namespace WizBot.Modules.Searches.Youtube;

public interface IYoutubeSearchService
{
    Task<VideoInfo[]?> SearchAsync(string query);
}