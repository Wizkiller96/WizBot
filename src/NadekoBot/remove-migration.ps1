dotnet ef migrations remove -c SqliteContext
dotnet ef migrations remove -c PostgreSqlContext -f
dotnet ef migrations remove -c MysqlContext -f

