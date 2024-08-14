namespace WizBot.Modules.Utility;

public partial class Utility
{
    [Group]
    public partial class PromptCommands : WizBotModule<IAiAssistantService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Prompt([Leftover] string query)
        {
            await ctx.Channel.TriggerTypingAsync();
            var res = await _service.TryExecuteAiCommand(ctx.Guild, ctx.Message, (ITextChannel)ctx.Channel, query);
        }

        private string GetCommandString(WizBotCommandCallModel res)
            => $"{_bcs.Data.Prefix}{res.Name} {res.Arguments.Select((x, i) => GetParamString(x, i + 1 == res.Arguments.Count)).Join(" ")}";

        private static string GetParamString(string val, bool isLast)
            => isLast ? val : "\"" + val + "\"";
    }
}