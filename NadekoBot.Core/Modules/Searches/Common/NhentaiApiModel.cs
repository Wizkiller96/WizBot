using System.Collections.Generic;
using Newtonsoft.Json;

namespace NadekoBot.Core.Modules.Searches.Common
{
    public static class NhentaiApiModel
    {
        public class Title
        {
            [JsonProperty("english")] public string English { get; set; }

            [JsonProperty("japanese")] public string Japanese { get; set; }

            [JsonProperty("pretty")] public string Pretty { get; set; }
        }

        public class Page
        {
            [JsonProperty("t")] public string T { get; set; }

            [JsonProperty("w")] public int W { get; set; }

            [JsonProperty("h")] public int H { get; set; }
        }

        public class Cover
        {
            [JsonProperty("t")] public string T { get; set; }

            [JsonProperty("w")] public int W { get; set; }

            [JsonProperty("h")] public int H { get; set; }
        }

        public class Thumbnail
        {
            [JsonProperty("t")] public string T { get; set; }

            [JsonProperty("w")] public int W { get; set; }

            [JsonProperty("h")] public int H { get; set; }
        }

        public class Images
        {
            [JsonProperty("pages")] public List<Page> Pages { get; set; }

            [JsonProperty("cover")] public Cover Cover { get; set; }

            [JsonProperty("thumbnail")] public Thumbnail Thumbnail { get; set; }
        }

        public class Tag
        {
            [JsonProperty("id")] public int Id { get; set; }

            [JsonProperty("type")] public string Type { get; set; }

            [JsonProperty("name")] public string Name { get; set; }

            [JsonProperty("url")] public string Url { get; set; }

            [JsonProperty("count")] public int Count { get; set; }
        }

        public class Gallery
        {
            [JsonProperty("id")] public int Id { get; set; }

            [JsonProperty("media_id")] public string MediaId { get; set; }

            [JsonProperty("title")] public Title Title { get; set; }

            [JsonProperty("images")] public Images Images { get; set; }

            [JsonProperty("scanlator")] public string Scanlator { get; set; }

            [JsonProperty("upload_date")] public double UploadDate { get; set; }

            [JsonProperty("tags")] public Tag[] Tags { get; set; }

            [JsonProperty("num_pages")] public int NumPages { get; set; }

            [JsonProperty("num_favorites")] public int NumFavorites { get; set; }
        }

        public class SearchResult
        {
            [JsonProperty("result")] public Gallery[] Result { get; set; }
        }
    }
}