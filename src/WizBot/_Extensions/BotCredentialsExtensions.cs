namespace WizBot.Extensions;

public static class BotCredentialsExtensions
{
    public static bool IsOwner(this IBotCredentials creds, IUser user)
        => creds.OwnerIds.Contains(user.Id);
    
    public static bool IsAdmin(this IBotCredentials creds, IUser user)
        => creds.AdminIds.Contains(user.Id);
}