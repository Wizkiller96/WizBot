#nullable disable
using Newtonsoft.Json;
using System.Collections.Immutable;
using System.Text;

namespace WizBot.Modules.Searches;

public partial class Searches
{
    [Group]
    public partial class MemegenCommands : WizBotModule
    {
        private static readonly ImmutableDictionary<char, string> _map = new Dictionary<char, string>
        {
            { '?', "~q" },
            { '%', "~p" },
            { '#', "~h" },
            { '/', "~s" },
            { ' ', "-" },
            { '-', "--" },
            { '_', "__" },
            { '"', "''" }
        }.ToImmutableDictionary();

        private readonly IHttpClientFactory _httpFactory;

        public MemegenCommands(IHttpClientFactory factory)
            => _httpFactory = factory;

        [Cmd]
        public async Task Memelist(int page = 1)
        {
            if (--page < 0)
                return;

            using var http = _httpFactory.CreateClient("memelist");
            using var res = await http.GetAsync("https://api.memegen.link/templates/");

            var rawJson = await res.Content.ReadAsStringAsync();

            var data = JsonConvert.DeserializeObject<List<MemegenTemplate>>(rawJson)!;

            await Response()
                  .Paginated()
                  .Items(data)
                  .PageSize(15)
                  .CurrentPage(page)
                  .Page((items, curPage) =>
                  {
                      var templates = string.Empty;
                      foreach (var template in items)
                          templates += $"**{template.Name}:**\n key: `{template.Id}`\n";
                      var embed = _sender.CreateEmbed().WithOkColor().WithDescription(templates);

                      return embed;
                  })
                  .SendAsync();
        }

        [Cmd]
        public async Task Memegen(string meme, [Leftover] string memeText = null)
        {
            var memeUrl = $"http://api.memegen.link/{meme}";
            if (!string.IsNullOrWhiteSpace(memeText))
            {
                var memeTextArray = memeText.Split(';');
                foreach (var text in memeTextArray)
                {
                    var newText = Replace(text);
                    memeUrl += $"/{newText}";
                }
            }

            memeUrl += ".png";
            await Response().Text(memeUrl).SendAsync();
        }

        private static string Replace(string input)
        {
            var sb = new StringBuilder();

            foreach (var c in input)
            {
                if (_map.TryGetValue(c, out var tmp))
                    sb.Append(tmp);
                else
                    sb.Append(c);
            }

            return sb.ToString();
        }

        private class MemegenTemplate
        {
            public string Name { get; set; }
            public string Id { get; set; }
        }
    }
}