using Cloneable;
using NadekoBot.Common.Yml;

namespace NadekoBot.Modules.Searches;

[Cloneable]
public partial class SearchesConfig : ICloneable<SearchesConfig>
{
    [Comment("DO NOT CHANGE")]
    public int Version { get; set; } = 0;
    
    [Comment(@"Which engine should .search command
'google_scrape' - default. Scrapes the webpage for results. May break. Requires no api keys.
'google' - official google api. Requires googleApiKey and google.searchId set in creds.yml
'searx' - requires at least one searx instance specified in the 'searxInstances' property below")]
    public WebSearchEngine WebSearchEngine { get; set; } = WebSearchEngine.Google_Scrape;
    
    [Comment(@"Which engine should .image command use
'google'- official google api. googleApiKey and google.imageSearchId set in creds.yml
'searx' requires at least one searx instance specified in the 'searxInstances' property below")]
    public ImgSearchEngine ImgSearchEngine { get; set; } = ImgSearchEngine.Google;
    

    [Comment(@"Which search provider will be used for the `.youtube` command.

- `ytDataApiv3` - uses google's official youtube data api. Requires `GoogleApiKey` set in creds and youtube data api enabled in developers console

- `ytdl` - default, uses youtube-dl. Requires `youtube-dl` to be installed and it's path added to env variables. Slow.

- `ytdlp` - recommended easy, uses `yt-dlp`. Requires `yt-dlp` to be installed and it's path added to env variables

- `invidious` - recommended advanced, uses invidious api. Requires at least one invidious instance specified in the `invidiousInstances` property")]
    public YoutubeSearcher YtProvider { get; set; } = YoutubeSearcher.Ytdl;

    [Comment(@"Set the searx instance urls in case you want to use 'searx' for either img or web search.
Nadeko will use a random one for each request.
Use a fully qualified url. Example: `https://my-searx-instance.mydomain.com`
Instances specified must support 'format=json' query parameter.
- In case you're running your own searx instance, set 

search:
  formats:
    - json

in 'searxng/settings.yml' on your server 

- If you're using a public instance, make sure that the instance you're using supports it (they usually don't)")]
    public List<string> SearxInstances { get; set; } = new List<string>();

    [Comment(@"Set the invidious instance urls in case you want to use 'invidious' for `.youtube` search
Nadeko will use a random one for each request.
These instances may be used for music queue functionality in the future.
Use a fully qualified url. Example: https://my-invidious-instance.mydomain.com

Instances specified must have api available.
You check that by opening an api endpoint in your browser. For example: https://my-invidious-instance.mydomain.com/api/v1/trending")]
    public List<string> InvidiousInstances { get; set; } = new List<string>();
}

public enum YoutubeSearcher
{
    YtDataApiv3,
    Ytdl,
    Ytdlp,
    Invid,
    Invidious = 3
}