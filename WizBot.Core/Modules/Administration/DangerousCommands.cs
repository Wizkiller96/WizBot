using Discord.Commands;
using WizBot.Common.Attributes;
using WizBot.Extensions;
using System;
using System.Threading.Tasks;
using Discord;
using WizBot.Core.Modules.Administration.Services;

namespace WizBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        [OwnerOnly]
        public class DangerousCommands : WizBotSubmodule<DangerousCommandsService>
        {

            private async Task InternalExecSql(string sql, params object[] reps)
            {
                sql = string.Format(sql, reps);
                try
                {
                    var embed = new EmbedBuilder()
                        .WithTitle(GetText("sql_confirm_exec"))
                        .WithDescription(Format.Code(sql));

                    if (!await PromptUserConfirmAsync(embed).ConfigureAwait(false))
                    {
                        return;
                    }

                    var res = await _service.ExecuteSql(sql).ConfigureAwait(false);
                    await Context.Channel.SendConfirmAsync(res.ToString()).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendErrorAsync(ex.ToString()).ConfigureAwait(false);
                }
            }
            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task ExecSql([Remainder]string sql) =>
                InternalExecSql(sql);

            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task DeleteWaifus() =>
                ExecSql(DangerousCommandsService.WaifusDeleteSql);

            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task DeleteWaifu(IUser user) =>
                DeleteWaifu(user.Id);

            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task DeleteWaifu(ulong userId) =>
                InternalExecSql(DangerousCommandsService.WaifuDeleteSql, userId);

            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task DeleteCurrency() =>
                ExecSql(DangerousCommandsService.CurrencyDeleteSql);

            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task DeletePlaylists() =>
                ExecSql(DangerousCommandsService.MusicPlaylistDeleteSql);

            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task DeleteExp() =>
                ExecSql(DangerousCommandsService.XpDeleteSql);
        }
    }
}