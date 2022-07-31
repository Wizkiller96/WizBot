using Microsoft.EntityFrameworkCore;

namespace WizBot.Services.Database;

public sealed class PostgreSqlContext : WizBotContext
{
    private readonly string _connStr;

    protected override string CurrencyTransactionOtherIdDefaultValue
        => "NULL";
    protected override string DiscordUserLastXpGainDefaultValue
        => "timezone('utc', now()) - interval '-1 year'";
    protected override string LastLevelUpDefaultValue
        => "timezone('utc', now())";

    public PostgreSqlContext(string connStr = "Host=localhost")
    {
        _connStr = connStr;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        
        base.OnConfiguring(optionsBuilder);
        optionsBuilder
            .UseLowerCaseNamingConvention()
            .UseNpgsql(_connStr);
    }
}