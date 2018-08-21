namespace WizBot.Core.Modules.Searches.Common
{
    public class OmdbMovie
    {
        public string Title { get; set; }
        public string Year { get; set; }
        public string ImdbRating { get; set; }
        public string ImdbId { get; set; }
        public string Genre { get; set; }
        public string Plot { get; set; }
        public string Poster { get; set; }

        public override string ToString() =>
$@"`Title:` {Title}
`Year:` {Year}
`Rating:` {ImdbRating}
`Genre:` {Genre}
`Link:` http://www.imdb.com/title/{ImdbId}/
`Plot:` {Plot}";
    }
}