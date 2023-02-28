#nullable disable
using WizBot.Modules.Administration.Services;

namespace WizBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        [NoPublicBot]
        [OwnerOnly]
        public partial class DangerousCommands : WizBotModule<DangerousCommandsService>
        {
            private async Task ConfirmActionInternalAsync(string name, Func<Task> action)
            {
                try
                {
                    var embed = _eb.Create()
                                   .WithTitle(GetText(strs.sql_confirm_exec))
                                   .WithDescription(name);

                    if (!await PromptUserConfirmAsync(embed))
                        return;

                    await action();
                    await ctx.OkAsync();
                }
                catch (Exception ex)
                {
                    await SendErrorAsync(ex.ToString());
                }
            }

            [Cmd]
            [NoPublicBot]
            [OwnerOnly]
            public Task SqlSelect([Leftover] string sql)
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
            [NoPublicBot]
            [OwnerOnly]
            public async Task SqlExec([Leftover] string sql)
            {
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
            [NoPublicBot]
            [OwnerOnly]
            public Task DeleteWaifus()
                => ConfirmActionInternalAsync("Delete Waifus", () => _service.DeleteWaifus());

            [Cmd]
            [NoPublicBot]
            [OwnerOnly]
            public async Task DeleteWaifu(IUser user)
                => await DeleteWaifu(user.Id);

            [Cmd]
            [NoPublicBot]
            [OwnerOnly]
            public Task DeleteWaifu(ulong userId)
                => ConfirmActionInternalAsync($"Delete Waifu {userId}", () => _service.DeleteWaifu(userId));

            [Cmd]
            [NoPublicBot]
            [OwnerOnly]
            public Task DeleteCurrency()
                => ConfirmActionInternalAsync("Delete Currency", () => _service.DeleteCurrency());

            [Cmd]
            [NoPublicBot]
            [OwnerOnly]
            public Task DeletePlaylists()
                => ConfirmActionInternalAsync("Delete Playlists", () => _service.DeletePlaylists());

            [Cmd]
            [NoPublicBot]
            [OwnerOnly]
            public Task DeleteXp()
                => ConfirmActionInternalAsync("Delete Xp", () => _service.DeleteXp());

            [Cmd]
            [NoPublicBot]
            [OwnerOnly]
            public async Task PurgeUser(ulong userId)
            {
                var embed = _eb.Create()
                               .WithDescription(GetText(strs.purge_user_confirm(Format.Bold(userId.ToString()))));

                if (!await PromptUserConfirmAsync(embed))
                    return;

                await _service.PurgeUserAsync(userId);
                await ctx.OkAsync();
            }

            [Cmd]
            [NoPublicBot]
            [OwnerOnly]
            public Task PurgeUser([Leftover] IUser user)
                => PurgeUser(user.Id);
        }
    }
}