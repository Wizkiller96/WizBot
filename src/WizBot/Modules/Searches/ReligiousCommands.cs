using WizBot.Modules.Searches.Common;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace WizBot.Modules.Searches;

public partial class Searches
{
    public partial class ReligiousCommands : WizBotModule
    {
        private readonly IHttpClientFactory _httpFactory;
        
        public ReligiousCommands(IHttpClientFactory httpFactory)
        {
            _httpFactory = httpFactory;
        }
        
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Bible(string book, string chapterAndVerse)
        {
            var obj = new BibleVerses();
            try
            {
                using var http = _httpFactory.CreateClient();
                obj = await http.GetFromJsonAsync<BibleVerses>($"https://bible-api.com/{book} {chapterAndVerse}");
            }
            catch
            {
            }

            if (obj.Error is not null || obj.Verses is null || obj.Verses.Length == 0)
                await Response().Error(obj.Error ?? "No verse found.").SendAsync();
            else
            {
                var v = obj.Verses[0];
                await Response()
                      .Embed(_sender.CreateEmbed()
                                    .WithOkColor()
                                    .WithTitle($"{v.BookName} {v.Chapter}:{v.Verse}")
                                    .WithDescription(v.Text))
                      .SendAsync();
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Quran(string ayah)
        {
            using var http = _httpFactory.CreateClient(); 
            
            var obj = await http.GetFromJsonAsync<QuranResponse<QuranAyah>>($"https://api.alquran.cloud/v1/ayah/{Uri.EscapeDataString(ayah)}/editions/en.asad,ar.alafasy");
            if(obj is null or not { Code: 200 })
            {
                await Response().Error("No verse found.").SendAsync();
                return;
            }

            var english = obj.Data[0];
            var arabic = obj.Data[1];

            await using var audio = await http.GetStreamAsync(arabic.Audio);

            await Response()
                  .Embed(_sender.CreateEmbed()
                                .WithOkColor()
                                .AddField("Arabic", arabic.Text)
                                .AddField("English", english.Text)
                                .WithFooter(arabic.Number.ToString()))
                  .File(audio, Uri.EscapeDataString(ayah) + ".mp3")
                  .SendAsync();
        }
    }
}

public sealed class QuranResponse<T>
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; }
    
    [JsonPropertyName("data")]
    public T[] Data { get; set; }
}

public sealed class QuranAyah
{
    [JsonPropertyName("number")]
    public int Number { get; set; }
    
    [JsonPropertyName("audio")]
    public string Audio { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("text")]
    public string Text { get; set; }
    
}