#nullable disable
using NadekoBot.Modules.Administration.Services;

#if !GLOBAL_NADEKO
namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        [OwnerOnly]
        public partial class DangerousCommands : NadekoSubmodule<DangerousCommandsService>
        {
            private async Task InternalExecSql(string sql, params object[] reps)
            {
                sql = string.Format(sql, reps);
                try
                {
                    var embed = _eb.Create()
                                   .WithTitle(GetText(strs.sql_confirm_exec))
                                   .WithDescription(Format.Code(sql));

                    if (!await PromptUserConfirmAsync(embed))
                        return;

                    var res = await _service.ExecuteSql(sql);
                    await SendConfirmAsync(res.ToString());
                }
                catch (Exception ex)
                {
                    await SendErrorAsync(ex.ToString());
                }
            }

            [Cmd]
            [OwnerOnly]
            public partial Task SqlSelect([Leftover] string sql)
            {
                var result = _service.SelectSql(sql);

                return ctx.SendPaginatedConfirmAsync(0,
                    cur =>
                    {
                        var items = result.Results.Skip(cur * 20).Take(20).ToList();

                        if (!items.Any())
                            return _eb.Create().WithErrorColor().WithFooter(sql).WithDescription("-");

                        return _eb.Create()
                                  .WithOkColor()
                                  .WithFooter(sql)
                                  .WithTitle(string.Join(" ║ ", result.ColumnNames))
                                  .WithDescription(string.Join('\n', items.Select(x => string.Join(" ║ ", x))));
                    },
                    result.Results.Count,
                    20);
            }

            [Cmd]
            [OwnerOnly]
            public partial Task SqlExec([Leftover] string sql)
                => InternalExecSql(sql);

            [Cmd]
            [OwnerOnly]
            public partial Task DeleteWaifus()
                => SqlExec(DangerousCommandsService.WaifusDeleteSql);

            [Cmd]
            [OwnerOnly]
            public partial Task DeleteWaifu(IUser user)
                => DeleteWaifu(user.Id);

            [Cmd]
            [OwnerOnly]
            public partial Task DeleteWaifu(ulong userId)
                => InternalExecSql(DangerousCommandsService.WaifuDeleteSql, userId);

            [Cmd]
            [OwnerOnly]
            public partial Task DeleteCurrency()
                => SqlExec(DangerousCommandsService.CurrencyDeleteSql);

            [Cmd]
            [OwnerOnly]
            public partial Task DeletePlaylists()
                => SqlExec(DangerousCommandsService.MusicPlaylistDeleteSql);

            [Cmd]
            [OwnerOnly]
            public partial Task DeleteXp()
                => SqlExec(DangerousCommandsService.XpDeleteSql);

            [Cmd]
            [OwnerOnly]
            public async partial Task PurgeUser(ulong userId)
            {
                var embed = _eb.Create()
                               .WithDescription(GetText(strs.purge_user_confirm(Format.Bold(userId.ToString()))));

                if (!await PromptUserConfirmAsync(embed)) return;

                await _service.PurgeUserAsync(userId);
                await ctx.OkAsync();
            }

            [Cmd]
            [OwnerOnly]
            public partial Task PurgeUser([Leftover] IUser user)
                => PurgeUser(user.Id);
            //[NadekoCommand, Usage, Description, Aliases]
            //[OwnerOnly]
            //public partial Task DeleteUnusedCrnQ() =>
            //    SqlExec(DangerousCommandsService.DeleteUnusedCustomReactionsAndQuotes);
        }
    }
}
#endif