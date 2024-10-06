namespace WizBot.Extensions;

public static class BotCredentialsExtensions
{
    public static bool IsOwner(this IBotCreds creds, IUser user)
        => creds.IsOwner(user.Id);
    
    public static bool IsOwner(this IBotCreds creds, ulong userId)
        => creds.OwnerIds.Contains(userId);
    
    public static bool IsAdmin(this IBotCreds creds, IUser user)
        => creds.IsAdmin(user.Id);
    
    public static bool IsAdmin(this IBotCreds creds, ulong userId)
        => creds.AdminIds.Contains(userId);
}