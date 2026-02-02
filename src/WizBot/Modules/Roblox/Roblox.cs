using Newtonsoft.Json;
using WizBot.Modules.Roblox.Services;

namespace WizBot.Modules.Roblox;

public partial class Roblox : WizModule<RobloxService>
{
    [Cmd]
    [Ratelimit(10)]
    public async Task RbxInfo([Remainder] string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            await Response().Error("Please provide a Roblox username.").SendAsync();
            return;
        }

        try
        {
            var info = await _service.GetUserInfoAsync(username);

            if (info is null)
            {
                await Response().Error("Roblox user not found.").SendAsync();
                return;
            }

            var embed = RobloxEmbedBuilder.BuildUserEmbed(info);

            await Response()
                .Embed(embed)
                .SendAsync();
        }
        catch (HttpRequestException)
        {
            await Response().Error("Roblox API is currently unavailable.").SendAsync();
        }
        catch (JsonException)
        {
            await Response().Error("Received invalid data from the Roblox API.").SendAsync();
        }
        catch (Exception ex)
        {
            await Response().Error($"Unexpected error: {ex.Message}").SendAsync();
        }
    }
}