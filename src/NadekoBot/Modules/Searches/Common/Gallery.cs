using System;

namespace NadekoBot.Core.Modules.Searches.Common
{
    public sealed class Tag
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }

    public sealed class Gallery
    {
        public string Id { get; }
        public string Url { get; }
        public string FullTitle { get; }
        public string Title { get; }
        public string Thumbnail { get; }
        public int PageCount { get; }
        public int Likes { get; }
        public DateTime UploadedAt { get; }
        public Tag[] Tags { get; }


        public Gallery(
            string id,
            string url,
            string fullTitle,
            string title,
            string thumbnail,
            int pageCount,
            int likes,
            DateTime uploadedAt,
            Tag[] tags)
        {
            Id = id;
            Url = url;
            FullTitle = fullTitle;
            Title = title;
            Thumbnail = thumbnail;
            PageCount = pageCount;
            Likes = likes;
            UploadedAt = uploadedAt;
            Tags = tags;
        }
    }
}