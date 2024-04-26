#nullable disable
using System.ComponentModel.DataAnnotations;

namespace Nadeko.Bot.Db.Models;

public class DbEntity
{
    [Key]
    public int Id { get; set; }

    public DateTime? DateAdded { get; set; } = DateTime.UtcNow;
}