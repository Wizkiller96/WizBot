namespace NadekoBot;

public interface IBotCredsProvider
{
    public void Reload();
    public IBotCredentials GetCreds();
    public void ModifyCredsFile(Action<IBotCredentials> func);
}