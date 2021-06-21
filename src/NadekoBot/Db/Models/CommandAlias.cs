namespace NadekoBot.Services.Database.Models
{
    public class CommandAlias : DbEntity
    {
        public string Trigger { get; set; }
        public string Mapping { get; set; }
    }
}
