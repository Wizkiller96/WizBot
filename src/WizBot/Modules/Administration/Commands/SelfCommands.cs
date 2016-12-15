﻿using Discord;
using Discord.Commands;
using WizBot.Attributes;
using WizBot.Extensions;
using System.Linq;
using System.Threading.Tasks;

namespace WizBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        class SelfCommands
        {
            private ShardedDiscordClient _client;

            public SelfCommands()
            {
                this._client = WizBot.Client;
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task Leave(IUserMessage umsg, [Remainder] string guildStr)
            {
                var channel = (ITextChannel)umsg.Channel;

                guildStr = guildStr.Trim().ToUpperInvariant();
                var server = _client.GetGuilds().FirstOrDefault(g => g.Id.ToString().Trim().ToUpperInvariant() == guildStr) ?? 
                    _client.GetGuilds().FirstOrDefault(g => g.Name.Trim().ToUpperInvariant() == guildStr);

                if (server == null)
                {
                    await channel.SendErrorAsync("⚠️ Cannot find that server").ConfigureAwait(false);
                    return;
                }
                if (server.OwnerId != _client.GetCurrentUser().Id)
                {
                    await server.LeaveAsync().ConfigureAwait(false);
                    await channel.SendConfirmAsync("✅ Left server " + server.Name).ConfigureAwait(false);
                }
                else
                {
                    await server.DeleteAsync().ConfigureAwait(false);
                    await channel.SendConfirmAsync("Deleted server " + server.Name).ConfigureAwait(false);
                }
            }
        }
    }
}
