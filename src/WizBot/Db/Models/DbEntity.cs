#nullable disable
using System.ComponentModel.DataAnnotations;

namespace WizBot.Services.Database.Models;

public class DbEntity
{
    [Key]
    public int Id { get; set; }

    public DateTime? DateAdded { get; set; } = DateTime.UtcNow;
}