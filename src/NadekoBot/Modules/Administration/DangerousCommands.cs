using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using System;
using System.Threading.Tasks;
using Discord;
using NadekoBot.Modules.Administration.Services;
using System.Linq;

#if !GLOBAL_NADEKO
namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        [OwnerOnly]
        public class DangerousCommands : NadekoSubmodule<DangerousCommandsService>
        {

            private async Task InternalExecSql(string sql, params object[] reps)
            {
                sql = string.Format(sql, reps);
                try
                {
                    var embed = _eb.Create()
                        .WithTitle(GetText(strs.sql_confirm_exec))
                        .WithDescription(Format.Code(sql));

                    if (!await PromptUserConfirmAsync(embed).ConfigureAwait(false))
                    {
                        return;
                    }

                    var res = await _service.ExecuteSql(sql).ConfigureAwait(false);
                    await SendConfirmAsync(res.ToString()).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await SendErrorAsync(ex.ToString()).ConfigureAwait(false);
                }
            }

            [NadekoCommand, Aliases]
            [OwnerOnly]
            public Task SqlSelect([Leftover]string sql)
            {
                var result = _service.SelectSql(sql);

                return ctx.SendPaginatedConfirmAsync(0, (cur) =>
                {
                    var items = result.Results.Skip(cur * 20).Take(20);

                    if (!items.Any())
                    {
                        return _eb.Create()
                            .WithErrorColor()
                            .WithFooter(sql)
                            .WithDescription("-");
                    }

                    return _eb.Create()
                        .WithOkColor()
                        .WithFooter(sql)
                        .WithTitle(string.Join(" ║ ", result.ColumnNames))
                        .WithDescription(string.Join('\n', items.Select(x => string.Join(" ║ ", x))));

                }, result.Results.Count, 20);
            }

            [NadekoCommand, Aliases]
            [OwnerOnly]
            public Task SqlExec([Leftover]string sql) =>
                InternalExecSql(sql);

            [NadekoCommand, Aliases]
            [OwnerOnly]
            public Task DeleteWaifus() =>
                SqlExec(DangerousCommandsService.WaifusDeleteSql);

            [NadekoCommand, Aliases]
            [OwnerOnly]
            public Task DeleteWaifu(IUser user) =>
                DeleteWaifu(user.Id);

            [NadekoCommand, Aliases]
            [OwnerOnly]
            public Task DeleteWaifu(ulong userId) =>
                InternalExecSql(DangerousCommandsService.WaifuDeleteSql, userId);

            [NadekoCommand, Aliases]
            [OwnerOnly]
            public Task DeleteCurrency() =>
                SqlExec(DangerousCommandsService.CurrencyDeleteSql);

            [NadekoCommand, Aliases]
            [OwnerOnly]
            public Task DeletePlaylists() =>
                SqlExec(DangerousCommandsService.MusicPlaylistDeleteSql);

            [NadekoCommand, Aliases]
            [OwnerOnly]
            public Task DeleteXp() =>
                SqlExec(DangerousCommandsService.XpDeleteSql);

            [NadekoCommand, Aliases]
            [OwnerOnly]
            public async Task PurgeUser(ulong userId)
            {
                var embed = _eb.Create()
                    .WithDescription(GetText(strs.purge_user_confirm(Format.Bold(userId.ToString()))));

                if (!await PromptUserConfirmAsync(embed).ConfigureAwait(false))
                {
                    return;
                }
                
                await _service.PurgeUserAsync(userId);
                await ctx.OkAsync();
            }

            [NadekoCommand, Aliases]
            [OwnerOnly]
            public Task PurgeUser([Leftover]IUser user)
                => PurgeUser(user.Id);
            //[NadekoCommand, Usage, Description, Aliases]
            //[OwnerOnly]
            //public Task DeleteUnusedCrnQ() =>
            //    SqlExec(DangerousCommandsService.DeleteUnusedCustomReactionsAndQuotes);
        }
    }
}
#endif