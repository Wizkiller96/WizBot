using Nadeko.Medusa;

namespace NadekoBot.Modules;

[OwnerOnly]
public partial class Medusa : NadekoModule<IMedusaLoaderService>
{
    [Cmd]
    [OwnerOnly]
    public async partial Task MedusaLoad(string? name = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            var loaded = _service.GetLoadedMedusae()
                                 .Select(x => x.Name)
                                 .ToHashSet();
            
            var unloaded = _service.GetAllMedusae()
                    .Where(x => !loaded.Contains(x))
                    .Select(x => Format.Code(x.ToString()))
                    .ToArray();

            if (unloaded.Length == 0)
            {
                await ReplyPendingLocalizedAsync(strs.no_medusa_available);
                return;
            }

            await ctx.SendPaginatedConfirmAsync(0,
                page =>
                {
                    return _eb.Create(ctx)
                              .WithOkColor()
                              .WithTitle(GetText(strs.list_of_unloaded))
                              .WithDescription(unloaded.Skip(10 * page).Take(10).Join('\n'));
                },
                unloaded.Length,
                10);
            return;
        }

        var res = await _service.LoadMedusaAsync(name);
        if (res == MedusaLoadResult.Success)
            await ReplyConfirmLocalizedAsync(strs.medusa_loaded(Format.Code(name)));
        else
        {
            var locStr = res switch
            {
                MedusaLoadResult.Empty => strs.medusa_empty,
                MedusaLoadResult.AlreadyLoaded => strs.medusa_already_loaded(Format.Code(name)),
                MedusaLoadResult.NotFound => strs.medusa_invalid_not_found,
                MedusaLoadResult.UnknownError => strs.error_occured,
                _ => strs.error_occured
            };

            await ReplyErrorLocalizedAsync(locStr);
        }
    }
    
    [Cmd]
    [OwnerOnly]
    public async partial Task MedusaUnload(string? name = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            var loaded = _service.GetLoadedMedusae();
            if (loaded.Count == 0)
            {
                await ReplyPendingLocalizedAsync(strs.no_medusa_loaded);
                return;
            }

            await ctx.Channel.EmbedAsync(_eb.Create(ctx)
                                            .WithOkColor()
                                            .WithTitle(GetText(strs.loaded_medusae))
                                            .WithDescription(loaded.Select(x => x.Name)
                                                                   .Join("\n")));
            
            return;
        }
        
        var res = await _service.UnloadMedusaAsync(name);
        if (res == MedusaUnloadResult.Success)
            await ReplyConfirmLocalizedAsync(strs.medusa_unloaded(Format.Code(name)));
        else
        {
            var locStr = res switch
            {
                MedusaUnloadResult.NotLoaded => strs.medusa_not_loaded,
                MedusaUnloadResult.PossiblyUnable => strs.medusa_possibly_cant_unload,
                _ => strs.error_occured
            };

            await ReplyErrorLocalizedAsync(locStr);
        }
    }

    [Cmd]
    [OwnerOnly]
    public async partial Task MedusaList()
    {
        var all = _service.GetAllMedusae();

        if (all.Count == 0)
        {
            await ReplyPendingLocalizedAsync(strs.no_medusa_available);
            return;
        }
        
        var loaded = _service.GetLoadedMedusae()
                             .Select(x => x.Name)
                             .ToHashSet();

        var output = all
            .Select(m =>
            {
                var emoji = loaded.Contains(m) ? "`✅`" : "`🔴`";
                return $"{emoji} `{m}`";
            })
            .ToArray();


        await ctx.SendPaginatedConfirmAsync(0,
            page => _eb.Create(ctx)
                       .WithOkColor()
                       .WithTitle(GetText(strs.list_of_medusae))
                       .WithDescription(output.Skip(page * 10).Take(10).Join('\n')),
            output.Length,
            10);
    }

    [Cmd]
    [OwnerOnly]
    public async partial Task MedusaInfo(string? name = null)
    {
        var medusae = _service.GetLoadedMedusae();

        if (name is not null)
        {
            var found = medusae.FirstOrDefault(x => string.Equals(x.Name,
                name,
                StringComparison.InvariantCultureIgnoreCase));
            
            if (found is null)
            {
                await ReplyErrorLocalizedAsync(strs.medusa_name_not_found);
                return;
            }

            var cmdCount = found.Sneks.Sum(x => x.Commands.Count);
            var cmdNames = found.Sneks
                                .SelectMany(x => x.Commands)
                                   .Select(x => Format.Code(x.Name))
                                   .Join(" | ");

            var eb = _eb.Create(ctx)
                        .WithOkColor()
                        .WithAuthor(GetText(strs.medusa_info))
                        .WithTitle(found.Name)
                        .WithDescription(found.Description)
                        .AddField(GetText(strs.sneks_count(found.Sneks.Count)),
                            found.Sneks.Count == 0
                                ? "-"
                                : found.Sneks.Select(x => x.Name).Join('\n'),
                            true)
                        .AddField(GetText(strs.commands_count(cmdCount)),
                            string.IsNullOrWhiteSpace(cmdNames)
                                ? "-"
                                : cmdNames,
                            true);

            await ctx.Channel.EmbedAsync(eb);
            return;
        }

        if (medusae.Count == 0)
        {
            await ReplyPendingLocalizedAsync(strs.no_medusa_loaded);
            return;
        }
        
        await ctx.SendPaginatedConfirmAsync(0,
            page =>
            {
                var eb = _eb.Create(ctx)
                            .WithOkColor();

                foreach (var medusa in medusae.Skip(page * 9).Take(9))
                {
                    eb.AddField(medusa.Name,
                        $@"`Sneks:` {medusa.Sneks.Count}
`Commands:` {medusa.Sneks.Sum(x => x.Commands.Count)}
--
{medusa.Description}");
                }

                return eb;
            }, medusae.Count, 9);
    }
}