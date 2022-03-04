#nullable disable

namespace NadekoBot.Modules.NadekoExpressions;

[Name("Expressions")]
public partial class NadekoExpressions : NadekoModule<NadekoExpressionsService>
{
    public enum All
    {
        All
    }

    private readonly IBotCredentials _creds;
    private readonly IHttpClientFactory _clientFactory;

    public NadekoExpressions(IBotCredentials creds, IHttpClientFactory clientFactory)
    {
        _creds = creds;
        _clientFactory = clientFactory;
    }

    private bool AdminInGuildOrOwnerInDm()
        => (ctx.Guild is null && _creds.IsOwner(ctx.User))
           || (ctx.Guild is not null && ((IGuildUser)ctx.User).GuildPermissions.Administrator);

    [Cmd]
    public async partial Task ExprAdd(string key, [Leftover] string message)
    {
        if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(key))
            return;

        if (!AdminInGuildOrOwnerInDm())
        {
            await ReplyErrorLocalizedAsync(strs.expr_insuff_perms);
            return;
        }

        var ex = await _service.AddAsync(ctx.Guild?.Id, key, message);

        await ctx.Channel.EmbedAsync(_eb.Create()
                                        .WithOkColor()
                                        .WithTitle(GetText(strs.expr_new))
                                        .WithDescription($"#{new kwum(ex.Id)}")
                                        .AddField(GetText(strs.trigger), key)
                                        .AddField(GetText(strs.response),
                                            message.Length > 1024 ? GetText(strs.redacted_too_long) : message));
    }

    [Cmd]
    public async partial Task ExprEdit(kwum id, [Leftover] string message)
    {
        var channel = ctx.Channel as ITextChannel;
        if (string.IsNullOrWhiteSpace(message) || id < 0)
            return;

        if ((channel is null && !_creds.IsOwner(ctx.User))
            || (channel is not null && !((IGuildUser)ctx.User).GuildPermissions.Administrator))
        {
            await ReplyErrorLocalizedAsync(strs.expr_insuff_perms);
            return;
        }

        var ex = await _service.EditAsync(ctx.Guild?.Id, id, message);
        if (ex is not null)
        {
            await ctx.Channel.EmbedAsync(_eb.Create()
                                            .WithOkColor()
                                            .WithTitle(GetText(strs.expr_edited))
                                            .WithDescription($"#{id}")
                                            .AddField(GetText(strs.trigger), ex.Trigger)
                                            .AddField(GetText(strs.response),
                                                message.Length > 1024 ? GetText(strs.redacted_too_long) : message));
        }
        else
            await ReplyErrorLocalizedAsync(strs.expr_no_found_id);
    }

    [Cmd]
    [Priority(1)]
    public async partial Task ExprList(int page = 1)
    {
        if (--page < 0 || page > 999)
            return;

        var expressions = _service.GetExpressionsFor(ctx.Guild?.Id);

        if (expressions is null || !expressions.Any())
        {
            await ReplyErrorLocalizedAsync(strs.expr_no_found);
            return;
        }

        await ctx.SendPaginatedConfirmAsync(page,
            curPage =>
            {
                var desc = expressions.OrderBy(ex => ex.Trigger)
                                      .Skip(curPage * 20)
                                      .Take(20)
                                      .Select(ex => $"{(ex.ContainsAnywhere ? "🗯" : "◾")}"
                                                    + $"{(ex.DmResponse ? "✉" : "◾")}"
                                                    + $"{(ex.AutoDeleteTrigger ? "❌" : "◾")}"
                                                    + $"`{(kwum)ex.Id}` {ex.Trigger}"
                                                    + (string.IsNullOrWhiteSpace(ex.Reactions)
                                                        ? string.Empty
                                                        : " // " + string.Join(" ", ex.GetReactions())))
                                      .Join('\n');

                return _eb.Create().WithOkColor().WithTitle(GetText(strs.expressions)).WithDescription(desc);
            },
            expressions.Length,
            20);
    }

    [Cmd]
    public async partial Task ExprShow(kwum id)
    {
        var found = _service.GetExpression(ctx.Guild?.Id, id);

        if (found is null)
        {
            await ReplyErrorLocalizedAsync(strs.expr_no_found_id);
            return;
        }

        await ctx.Channel.EmbedAsync(_eb.Create()
                                        .WithOkColor()
                                        .WithDescription($"#{id}")
                                        .AddField(GetText(strs.trigger), found.Trigger.TrimTo(1024))
                                        .AddField(GetText(strs.response),
                                            found.Response.TrimTo(1000).Replace("](", "]\\(")));
    }

    [Cmd]
    public async partial Task ExprDelete(kwum id)
    {
        if (!AdminInGuildOrOwnerInDm())
        {
            await ReplyErrorLocalizedAsync(strs.expr_insuff_perms);
            return;
        }

        var ex = await _service.DeleteAsync(ctx.Guild?.Id, id);

        if (ex is not null)
        {
            await ctx.Channel.EmbedAsync(_eb.Create()
                                            .WithOkColor()
                                            .WithTitle(GetText(strs.expr_deleted))
                                            .WithDescription($"#{id}")
                                            .AddField(GetText(strs.trigger), ex.Trigger.TrimTo(1024))
                                            .AddField(GetText(strs.response), ex.Response.TrimTo(1024)));
        }
        else
            await ReplyErrorLocalizedAsync(strs.expr_no_found_id);
    }

    [Cmd]
    public async partial Task ExprReact(kwum id, params string[] emojiStrs)
    {
        if (!AdminInGuildOrOwnerInDm())
        {
            await ReplyErrorLocalizedAsync(strs.expr_insuff_perms);
            return;
        }

        var ex = _service.GetExpression(ctx.Guild?.Id, id);
        if (ex is null)
        {
            await ReplyErrorLocalizedAsync(strs.expr_no_found_id);
            return;
        }

        if (emojiStrs.Length == 0)
        {
            await _service.ResetExprReactions(ctx.Guild?.Id, id);
            await ReplyConfirmLocalizedAsync(strs.expr_reset(Format.Bold(id.ToString())));
            return;
        }

        var succ = new List<string>();
        foreach (var emojiStr in emojiStrs)
        {
            var emote = emojiStr.ToIEmote();

            // i should try adding these emojis right away to the message, to make sure the bot can react with these emojis. If it fails, skip that emoji
            try
            {
                await ctx.Message.AddReactionAsync(emote);
                await Task.Delay(100);
                succ.Add(emojiStr);

                if (succ.Count >= 3)
                    break;
            }
            catch { }
        }

        if (succ.Count == 0)
        {
            await ReplyErrorLocalizedAsync(strs.invalid_emojis);
            return;
        }

        await _service.SetExprReactions(ctx.Guild?.Id, id, succ);


        await ReplyConfirmLocalizedAsync(strs.expr_set(Format.Bold(id.ToString()),
            succ.Select(static x => x.ToString()).Join(", ")));
    }

    [Cmd]
    public partial Task ExprCa(kwum id)
        => InternalExprEdit(id, ExprField.ContainsAnywhere);

    [Cmd]
    public partial Task ExprDm(kwum id)
        => InternalExprEdit(id, ExprField.DmResponse);

    [Cmd]
    public partial Task ExprAd(kwum id)
        => InternalExprEdit(id, ExprField.AutoDelete);

    [Cmd]
    public partial Task ExprAt(kwum id)
        => InternalExprEdit(id, ExprField.AllowTarget);

    [Cmd]
    [OwnerOnly]
    public async partial Task ExprsReload()
    {
        await _service.TriggerReloadExpressions();

        await ctx.OkAsync();
    }

    private async Task InternalExprEdit(kwum id, ExprField option)
    {
        if (!AdminInGuildOrOwnerInDm())
        {
            await ReplyErrorLocalizedAsync(strs.expr_insuff_perms);
            return;
        }

        var (success, newVal) = await _service.ToggleExprOptionAsync(id, option);
        if (!success)
        {
            await ReplyErrorLocalizedAsync(strs.expr_no_found_id);
            return;
        }

        if (newVal)
        {
            await ReplyConfirmLocalizedAsync(strs.option_enabled(Format.Code(option.ToString()),
                Format.Code(id.ToString())));
        }
        else
        {
            await ReplyConfirmLocalizedAsync(strs.option_disabled(Format.Code(option.ToString()),
                Format.Code(id.ToString())));
        }
    }

    [Cmd]
    [RequireContext(ContextType.Guild)]
    [UserPerm(GuildPerm.Administrator)]
    public async partial Task ExprClear()
    {
        if (await PromptUserConfirmAsync(_eb.Create()
                                            .WithTitle("Custom reaction clear")
                                            .WithDescription("This will delete all custom reactions on this server.")))
        {
            var count = _service.DeleteAllExpressions(ctx.Guild.Id);
            await ReplyConfirmLocalizedAsync(strs.exprs_cleared(count));
        }
    }

    [Cmd]
    public async partial Task ExprsExport()
    {
        if (!AdminInGuildOrOwnerInDm())
        {
            await ReplyErrorLocalizedAsync(strs.expr_insuff_perms);
            return;
        }

        _ = ctx.Channel.TriggerTypingAsync();

        var serialized = _service.ExportExpressions(ctx.Guild?.Id);
        await using var stream = await serialized.ToStream();
        await ctx.Channel.SendFileAsync(stream, "exprs-export.yml");
    }

    [Cmd]
#if GLOBAL_NADEKO
    [OwnerOnly]
#endif
    public async partial Task ExprsImport([Leftover] string input = null)
    {
        if (!AdminInGuildOrOwnerInDm())
        {
            await ReplyErrorLocalizedAsync(strs.expr_insuff_perms);
            return;
        }

        input = input?.Trim();

        _ = ctx.Channel.TriggerTypingAsync();

        if (input is null)
        {
            var attachment = ctx.Message.Attachments.FirstOrDefault();
            if (attachment is null)
            {
                await ReplyErrorLocalizedAsync(strs.expr_import_no_input);
                return;
            }

            using var client = _clientFactory.CreateClient();
            input = await client.GetStringAsync(attachment.Url);

            if (string.IsNullOrWhiteSpace(input))
            {
                await ReplyErrorLocalizedAsync(strs.expr_import_no_input);
                return;
            }
        }

        var succ = await _service.ImportExpressionsAsync(ctx.Guild?.Id, input);
        if (!succ)
        {
            await ReplyErrorLocalizedAsync(strs.expr_import_invalid_data);
            return;
        }

        await ctx.OkAsync();
    }
}