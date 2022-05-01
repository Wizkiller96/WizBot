using WizBot.Services.Database.Models;

namespace WizBot.Db.Models;

public class BankUser : DbEntity
{
    public ulong UserId { get; set; }
    public long Balance { get; set; }
}