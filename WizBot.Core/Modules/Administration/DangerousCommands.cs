using Discord.Commands;
using WizBot.Common.Attributes;
using WizBot.Extensions;
using System;
using System.Threading.Tasks;
using Discord;
using WizBot.Core.Modules.Administration.Services;
using System.Linq;

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
                    await ctx.Channel.SendConfirmAsync(res.ToString()).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await ctx.Channel.SendErrorAsync(ex.ToString()).ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task SqlSelect([Leftover]string sql)
            {
                var result = _service.SelectSql(sql);

                return ctx.SendPaginatedConfirmAsync(0, (cur) =>
                {
                    var items = result.Results.Skip(cur * 20).Take(20);

                    if (!items.Any())
                    {
                        return new EmbedBuilder()
                            .WithErrorColor()
                            .WithFooter(sql)
                            .WithDescription("-");
                    }

                    return new EmbedBuilder()
                        .WithOkColor()
                        .WithFooter(sql)
                        .WithTitle(string.Join(" ║ ", result.ColumnNames))
                        .WithDescription(string.Join('\n', items.Select(x => string.Join(" ║ ", x))));

                }, result.Results.Count, 20);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task SqlExec([Leftover]string sql) =>
                InternalExecSql(sql);

            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task DeleteWaifus() =>
                SqlExec(DangerousCommandsService.WaifusDeleteSql);

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
                SqlExec(DangerousCommandsService.CurrencyDeleteSql);

            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task DeletePlaylists() =>
                SqlExec(DangerousCommandsService.MusicPlaylistDeleteSql);

            [WizBotCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public Task DeleteExp() =>
                SqlExec(DangerousCommandsService.XpDeleteSql);

            //[WizBotCommand, Usage, Description, Aliases]
            //[OwnerOnly]
            //public Task DeleteUnusedCrnQ() =>
            //    SqlExec(DangerousCommandsService.DeleteUnusedCustomReactionsAndQuotes);
        }
    }
}