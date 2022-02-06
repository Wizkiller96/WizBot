namespace NadekoBot.Modules.Searches;

public interface IStockDataService
{
    public Task<IReadOnlyCollection<StockData>> GetStockDataAsync(string query);
    Task<IReadOnlyCollection<SymbolData>> SearchSymbolAsync(string query);
}