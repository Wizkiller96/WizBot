namespace WizBot.Modules.Xp.Services;

public enum BuyResult
{
    Success,
    XpShopDisabled,
    AlreadyOwned,
    InsufficientFunds,
    InsufficientPatronTier,
    UnknownItem,
}