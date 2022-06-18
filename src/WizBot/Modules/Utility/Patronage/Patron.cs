﻿namespace WizBot.Modules.Utility.Patronage;

public readonly struct Patron
{
    /// <summary>
    /// Unique id assigned to this patron by the payment platform
    /// </summary>
    public string UnqiuePlatformUserId { get; init; }

    /// <summary>
    /// Discord UserId to which this <see cref="UnqiuePlatformUserId"/> is connected to
    /// </summary>
    public ulong UserId { get; init; }

    /// <summary>
    /// Amount the Patron is currently pledging or paid
    /// </summary>
    public int Amount { get; init; }

    /// <summary>
    /// Current Tier of the patron
    /// (do not question it in consumer classes, as the calculation should be always internal and may change)
    /// </summary>
    public PatronTier Tier { get; init; }

    /// <summary>
    /// When was the last time this <see cref="Amount"/> was paid
    /// </summary>
    public DateTime PaidAt { get; init; }

    /// <summary>
    /// After which date does the user's Patronage benefit end
    /// </summary>
    public DateTime ValidThru { get; init; }

    public bool IsActive
        => !ValidThru.IsBeforeToday();
}