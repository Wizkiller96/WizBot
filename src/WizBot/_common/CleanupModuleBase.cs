#nullable disable
namespace Wiz.Common;

public abstract class CleanupModuleBase : WizBotModule
{
    protected async Task ConfirmActionInternalAsync(string name, Func<Task> action)
    {
        try
        {
            var embed = _sender.CreateEmbed()
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