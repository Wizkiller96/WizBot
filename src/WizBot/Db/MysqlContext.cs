﻿using Microsoft.EntityFrameworkCore;
using WizBot.Db.Models;

namespace WizBot.Db;

public sealed class MysqlContext : WizBotContext
{
    private readonly string _connStr;
    private readonly string _version;

    protected override string CurrencyTransactionOtherIdDefaultValue
        => "NULL";

    public MysqlContext(string connStr = "Server=localhost", string version = "8.0")
    {
        _connStr = connStr;
        _version = version;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder
            .UseLowerCaseNamingConvention()
            .UseMySql(_connStr, ServerVersion.Parse(_version));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // mysql is case insensitive by default
        // we can set binary collation to change that
        modelBuilder.Entity<ClubInfo>()
                    .Property(x => x.Name)
                    .UseCollation("utf8mb4_bin");
    }
}