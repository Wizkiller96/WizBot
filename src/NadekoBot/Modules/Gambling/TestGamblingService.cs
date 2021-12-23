using Discord.Interactions;

namespace NadekoBot.Modules.Gambling;

public class TestGamblingService : InteractionModuleBase
{
    [SlashCommand("test", "uwu")]
    public async Task Test(string input1, int input2)
    {
        await RespondAsync("Bravo " + input1 + input2);
    }
}