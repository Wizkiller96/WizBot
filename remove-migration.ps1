dotnet ef migrations remove -c SqliteContext -f -p src/NadekoBot/NadekoBot.csproj
dotnet ef migrations remove -c PostgreSqlContext -f -p src/NadekoBot/NadekoBot.csproj
dotnet ef migrations remove -c MysqlContext -f -p src/NadekoBot/NadekoBot.csproj

