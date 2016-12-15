using System.ComponentModel.DataAnnotations;

namespace WizBot.Services.Database.Models
{
    public class Quote : DbEntity
    {
        public ulong GuildId { get; set; }
        [Required]
        public string Keyword { get; set; }
        [Required]
        public string AuthorName { get; set; }
        public ulong AuthorId { get; set; }
        [Required]
        public string Text { get; set; }
    }
}
