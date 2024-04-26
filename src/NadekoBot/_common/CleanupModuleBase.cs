#nullable disable
namespace NadekoBot.Common;

public abstract class CleanupModuleBase : NadekoModule
{
    protected async Task ConfirmActionInternalAsync(string name, Func<Task> action)
    {
        try
        {
            var embed = _eb.Create()
                .WithTitle(GetText(strs.sql_confirm_exec))
                .WithDescription(name);

            if (!await PromptUserConfirmAsync(embed))
                return;

            await action();
            await ctx.OkAsync();
        }
        catch (Exception ex)
        {
            await SendErrorAsync(ex.ToString());
        }
    }
}