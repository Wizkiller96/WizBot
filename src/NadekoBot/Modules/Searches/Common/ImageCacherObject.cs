using System;
using System.Collections.Generic;
using NadekoBot.Modules.Nsfw.Common;

namespace NadekoBot.Modules.Searches.Common
{
    public class ImageCacherObject : IComparable<ImageCacherObject>
    {
        public Booru SearchType { get; }
        public string FileUrl { get; }
        public HashSet<string> Tags { get; }
        public string Rating { get; }

        public ImageCacherObject(DapiImageObject obj, Booru type)
        {
            if (type == Booru.Danbooru && !Uri.IsWellFormedUriString(obj.FileUrl, UriKind.Absolute))
            {
                this.FileUrl = "https://danbooru.donmai.us" + obj.FileUrl;
            }
            else
            {
                this.FileUrl = obj.FileUrl.StartsWith("http", StringComparison.InvariantCulture) ? obj.FileUrl : "https:" + obj.FileUrl;
            }
            this.SearchType = type;
            this.Rating = obj.Rating;
            this.Tags = new HashSet<string>((obj.Tags ?? obj.TagString).Split(' '));
        }

        public ImageCacherObject(string url, Booru type, string tags, string rating)
        {
            this.SearchType = type;
            this.FileUrl = url;
            this.Tags = new HashSet<string>(tags.Split(' '));
            this.Rating = rating;
        }

        public override string ToString()
        {
            return FileUrl;
        }

        public int CompareTo(ImageCacherObject other)
        {
            return string.Compare(FileUrl, other.FileUrl, StringComparison.InvariantCulture);
        }
    }
}