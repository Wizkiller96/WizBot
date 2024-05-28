#nullable disable

using Microsoft.EntityFrameworkCore;

namespace WizBot.Services;

public abstract class DbService
{
    /// <summary>
    /// Call this to apply all migrations
    /// </summary>
    public abstract Task SetupAsync();

    public abstract DbContext CreateRawDbContext(string dbType, string connString);
    public abstract WizBotContext GetDbContext();
}