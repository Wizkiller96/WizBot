#nullable disable
namespace NadekoBot.Modules.Searches;

public partial class Searches
{
    [Group]
    public partial class PlaceCommands : NadekoModule
    {
        public enum PlaceType
        {
            Cage, //http://www.placecage.com
            Steven, //http://www.stevensegallery.com
            Beard, //http://placebeard.it
            Fill, //http://www.fillmurray.com
            Bear, //https://www.placebear.com
            Kitten, //http://placekitten.com
            Bacon, //http://baconmockup.com
            Xoart //http://xoart.link
        }

        private static readonly string _typesStr = string.Join(", ", Enum.GetNames(typeof(PlaceType)));

        [Cmd]
        public async partial Task Placelist()
            => await SendConfirmAsync(GetText(strs.list_of_place_tags(prefix)), _typesStr);

        [Cmd]
        public async partial Task Place(PlaceType placeType, uint width = 0, uint height = 0)
        {
            var url = string.Empty;
            switch (placeType)
            {
                case PlaceType.Cage:
                    url = "http://www.placecage.com";
                    break;
                case PlaceType.Steven:
                    url = "http://www.stevensegallery.com";
                    break;
                case PlaceType.Beard:
                    url = "http://placebeard.it";
                    break;
                case PlaceType.Fill:
                    url = "http://www.fillmurray.com";
                    break;
                case PlaceType.Bear:
                    url = "https://www.placebear.com";
                    break;
                case PlaceType.Kitten:
                    url = "http://placekitten.com";
                    break;
                case PlaceType.Bacon:
                    url = "http://baconmockup.com";
                    break;
                case PlaceType.Xoart:
                    url = "http://xoart.link";
                    break;
            }

            var rng = new NadekoRandom();
            if (width is <= 0 or > 1000)
                width = (uint)rng.Next(250, 850);

            if (height is <= 0 or > 1000)
                height = (uint)rng.Next(250, 850);

            url += $"/{width}/{height}";

            await ctx.Channel.SendMessageAsync(url);
        }
    }
}