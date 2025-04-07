#nullable disable
namespace WizBot.Common;

public abstract class CleanupModuleBase : WizModule
{
    protected async Task ConfirmActionInternalAsync(string name, Func<Task> action)
    {
        try
        {
            var embed = CreateEmbed()
                .WithTitle(GetText(strs.sql_confirm_exec))
                .WithDescription(name);

            if (!await PromptUserConfirmAsync(embed))
                return;

            await action();
            await ctx.OkAsync();
        }
        catch (Exception ex)
        {
            await Response().Error(ex.ToString()).SendAsync();
        }
    }
}