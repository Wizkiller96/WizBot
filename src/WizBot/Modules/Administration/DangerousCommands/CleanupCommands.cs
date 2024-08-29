﻿using WizBot.Modules.Administration.DangerousCommands;

namespace WizBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class CleanupCommands : CleanupModuleBase
    {
        private readonly ICleanupService _svc;
        private readonly IBotCredsProvider _creds;

        public CleanupCommands(ICleanupService svc, IBotCredsProvider creds)
        {
            _svc = svc;
            _creds = creds;
        }

        [Cmd]
        [OwnerOnly]
        [RequireContext(ContextType.DM)]
        public async Task CleanupGuildData()
        {
            var result = await _svc.DeleteMissingGuildDataAsync();

            if (result is null)
            {
                await ctx.ErrorAsync();
                return;
            }

            await Response()
                  .Confirm($"{result.GuildCount} guilds' data remain in the database.")
                  .SendAsync();
        }
        
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task Keep()
        {
            var result = await _svc.KeepGuild(Context.Guild.Id);

            await Response().Text("This guild's bot data will be saved.").SendAsync();
        }
        
        [Cmd]
        [OwnerOnly]
        public async Task LeaveUnkeptServers(int startShardId)
        {
            var keptGuildCount = await _svc.GetKeptGuildCount();

            var response = await PromptUserConfirmAsync(new EmbedBuilder()
                .WithDescription($"""
                                  Do you want the bot to leave all unkept servers?

                                  There are currently {keptGuildCount} kept servers.   

                                  **This is a highly destructive and irreversible action.**
                                  """));

            if (!response)
                return;

            for (var i = startShardId; i < _creds.GetCreds().TotalShards; i++)
            {
                await _svc.LeaveUnkeptServers(startShardId);
                await Task.Delay(2250 * 1000);
            }

            await ctx.OkAsync();
        }
    }
}