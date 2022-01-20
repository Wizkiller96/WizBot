#nullable disable
using LinqToDB;
using NadekoBot.Services.Currency;
using NadekoBot.Services.Database.Models;

namespace NadekoBot.Services;

public class CurrencyService : ICurrencyService, INService
{
    private readonly DbService _db;

    public CurrencyService(DbService db)
        => _db = db;

    public Task<IWallet> GetWalletAsync(ulong userId, CurrencyType type = CurrencyType.Default)
    {
        if (type == CurrencyType.Default)
        {
            return Task.FromResult<IWallet>(new DefaultWallet(userId, _db.GetDbContext()));
        }

        throw new ArgumentOutOfRangeException(nameof(type));
    }

    public async Task AddBulkAsync(
        IReadOnlyCollection<ulong> userIds,
        long amount,
        Extra extra,
        CurrencyType type = CurrencyType.Default)
    {
        if (type == CurrencyType.Default)
        {
            await using var ctx = _db.GetDbContext();
            foreach (var userId in userIds)
            {
                var wallet = new DefaultWallet(userId, ctx);
                await wallet.Add(amount, extra);
            }

            await ctx.SaveChangesAsync();
            return;
        }

        throw new ArgumentOutOfRangeException(nameof(type));
    }

    public async Task RemoveBulkAsync(
        IReadOnlyCollection<ulong> userIds,
        long amount,
        Extra extra,
        CurrencyType type = CurrencyType.Default)
    {
        if (type == CurrencyType.Default)
        {
            await using var ctx = _db.GetDbContext();
            await ctx.DiscordUser
                     .Where(x => userIds.Contains(x.UserId))
                     .UpdateAsync(du => new()
                     {
                         CurrencyAmount = du.CurrencyAmount >= amount
                             ? du.CurrencyAmount - amount
                             : 0
                     });
            return;
        }

        throw new ArgumentOutOfRangeException(nameof(type));
    }

    private CurrencyTransaction GetCurrencyTransaction(ulong userId, string reason, long amount)
        => new() { Amount = amount, UserId = userId, Reason = reason ?? "-" };

    public async Task AddAsync(
        ulong userId,
        long amount,
        Extra extra)
    {
        await using var wallet = await GetWalletAsync(userId);
        await wallet.Add(amount, extra);
    }

    public async Task AddAsync(
        IUser user,
        long amount,
        Extra extra)
    {
        await using var wallet = await GetWalletAsync(user.Id);
        await wallet.Add(amount, extra);
    }

    public async Task<bool> RemoveAsync(
        ulong userId,
        long amount,
        Extra extra)
    {
        await using var wallet = await GetWalletAsync(userId);
        return await wallet.Take(amount, extra);
    }

    public async Task<bool> RemoveAsync(
        IUser user,
        long amount,
        Extra extra)
    {
        await using var wallet = await GetWalletAsync(user.Id);
        return await wallet.Take(amount, extra);
    }

    public async Task<bool> TransferAsync(
        ulong from,
        ulong to,
        long amount,
        string note)
    {
        await using var fromWallet = await GetWalletAsync(@from);
        await using var toWallet = await GetWalletAsync(to);
        
        var extra = new Extra("transfer", "gift", note);
        
        return await fromWallet.Transfer(amount, toWallet, extra);
    }
    
}