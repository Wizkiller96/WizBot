﻿#nullable disable
using Microsoft.EntityFrameworkCore;
using WizBot.Services.Database.Models;

namespace WizBot.Db;

public static class CurrencyTransactionExtensions
{
    public static List<CurrencyTransaction> GetPageFor(this DbSet<CurrencyTransaction> set, ulong userId, int page)
        => set.AsQueryable()
              .AsNoTracking()
              .Where(x => x.UserId == userId)
              .OrderByDescending(x => x.DateAdded)
              .Skip(15 * page)
              .Take(15)
              .ToList();
}