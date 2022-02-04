#nullable disable
using NadekoBot.Modules.Nsfw.Common;

namespace NadekoBot.Modules.Searches.Common;

public class ImageCacherObject : IComparable<ImageCacherObject>
{
    public Booru SearchType { get; }
    public string FileUrl { get; }
    public HashSet<string> Tags { get; }
    public string Rating { get; }

    public ImageCacherObject(DapiImageObject obj, Booru type)
    {
        if (type == Booru.Danbooru && !Uri.IsWellFormedUriString(obj.FileUrl, UriKind.Absolute))
            FileUrl = "https://danbooru.donmai.us" + obj.FileUrl;
        else
        {
            FileUrl = obj.FileUrl.StartsWith("http", StringComparison.InvariantCulture)
                ? obj.FileUrl
                : "https:" + obj.FileUrl;
        }

        SearchType = type;
        Rating = obj.Rating;
        Tags = new((obj.Tags ?? obj.TagString).Split(' '));
    }

    public ImageCacherObject(
        string url,
        Booru type,
        string tags,
        string rating)
    {
        SearchType = type;
        FileUrl = url;
        Tags = new(tags.Split(' '));
        Rating = rating;
    }

    public override string ToString()
        => FileUrl;

    public int CompareTo(ImageCacherObject other)
        => string.Compare(FileUrl, other.FileUrl, StringComparison.InvariantCulture);
}