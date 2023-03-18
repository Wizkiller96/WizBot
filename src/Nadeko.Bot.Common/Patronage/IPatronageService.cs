using NadekoBot.Db.Models;
using OneOf;

namespace NadekoBot.Modules.Utility.Patronage;

/// <summary>
/// Manages patrons and provides access to their data  
/// </summary>
public interface IPatronageService
{
    /// <summary>
    /// Called when the payment is made.
    /// Either as a single payment for that patron,
    /// or as a recurring monthly donation.
    /// </summary>
    public event Func<Patron, Task> OnNewPatronPayment;
    
    /// <summary>
    /// Called when the patron changes the pledge amount
    /// (Patron old, Patron new) => Task
    /// </summary>
    public event Func<Patron, Patron, Task> OnPatronUpdated;
    
    /// <summary>
    /// Called when the patron refunds the purchase or it's marked as fraud
    /// </summary>
    public event Func<Patron, Task> OnPatronRefunded;

    /// <summary>
    /// Gets a Patron with the specified userId
    /// </summary>
    /// <param name="userId">UserId for which to get the patron data for.</param>
    /// <returns>A patron with the specifeid userId</returns>
    public Task<Patron> GetPatronAsync(ulong userId);
    
    /// <summary>
    /// Gets the quota statistic for the user/patron specified by the userId
    /// </summary>
    /// <param name="userId">UserId of the user for which to get the quota statistic for</param>
    /// <returns>Quota stats for the specified user</returns>
    Task<UserQuotaStats> GetUserQuotaStatistic(ulong userId);

    
    Task<FeatureLimit> TryGetFeatureLimitAsync(FeatureLimitKey key, ulong userId, int? defaultValue);

    ValueTask<OneOf<(uint Hourly, uint Daily, uint Monthly), QuotaLimit>> TryIncrementQuotaCounterAsync(
        ulong userId,
        bool isSelf,
        FeatureType featureType,
        string featureName,
        uint? maybeHourly,
        uint? maybeDaily,
        uint? maybeMonthly);

    PatronConfigData GetConfig();
}