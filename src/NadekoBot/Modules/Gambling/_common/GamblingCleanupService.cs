using LinqToDB;
using NadekoBot.Db.Models;
using Nadeko.Bot.Db.Models;

namespace Nadeko.Bot.Modules.Gambling.Gambling._Common;

public class GamblingCleanupService : IGamblingCleanupService, INService
{
    private readonly DbService _db;

    public GamblingCleanupService(DbService db)
    {
        _db = db;
    }
    
    public async Task DeleteWaifus()
    {
        await using var ctx = _db.GetDbContext();
        await ctx.Set<WaifuInfo>().DeleteAsync();
        await ctx.Set<WaifuItem>().DeleteAsync();
        await ctx.Set<WaifuUpdate>().DeleteAsync();
        await ctx.SaveChangesAsync();
    }

    public async Task DeleteWaifu(ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        await ctx.Set<WaifuUpdate>()
            .Where(x => x.User.UserId == userId)
            .DeleteAsync();
        await ctx.Set<WaifuItem>()
            .Where(x => x.WaifuInfo.Waifu.UserId == userId)
            .DeleteAsync();
        await ctx.Set<WaifuInfo>()
            .Where(x => x.Claimer.UserId == userId)
            .UpdateAsync(old => new WaifuInfo()
            {
                ClaimerId = null,
            });
        await ctx.Set<WaifuInfo>()
            .Where(x => x.Waifu.UserId == userId)
            .DeleteAsync();
        await ctx.SaveChangesAsync();
    }
    
    public async Task DeleteCurrency()
    {
        await using var uow = _db.GetDbContext();
        await uow.Set<DiscordUser>().UpdateAsync(_ => new DiscordUser()
        {
            CurrencyAmount = 0
        });

        await uow.Set<CurrencyTransaction>().DeleteAsync();
        await uow.Set<PlantedCurrency>().DeleteAsync();
        await uow.Set<BankUser>().DeleteAsync();
        await uow.SaveChangesAsync();
    }
    
}