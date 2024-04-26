#nullable disable
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using NadekoBot.Modules.Administration.Services;

#if !GLOBAL_NADEKO
namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        [OwnerOnly]
        public partial class DangerousCommands : NadekoModule<DangerousCommandsService>
        {
            [Cmd]
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
            [OwnerOnly]
            public async Task SqlSelectCsv([Leftover] string sql)
            {
                var result = _service.SelectSql(sql);

                // create a file stream and write the data as csv
                using var ms = new MemoryStream();
                await using var sw = new StreamWriter(ms);
                await using var csv = new CsvWriter(sw,
                    new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = "," });

                foreach (var cn in result.ColumnNames)
                {
                    csv.WriteField(cn);
                }

                await csv.NextRecordAsync();

                foreach (var row in result.Results)
                {
                    foreach (var field in row)
                    {
                        csv.WriteField(field);
                    }

                    await csv.NextRecordAsync();
                }


                await csv.FlushAsync();
                ms.Position = 0;

                // send the file
                await ctx.Channel.SendFileAsync(ms, $"query_result_{DateTime.UtcNow.Ticks}.csv");
            }

            [Cmd]
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
            [OwnerOnly]
            public Task PurgeUser([Leftover] IUser user)
                => PurgeUser(user.Id);
        }
    }
}
#endif