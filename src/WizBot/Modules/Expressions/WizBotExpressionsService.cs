﻿#nullable disable
using Microsoft.EntityFrameworkCore;
using WizBot.Common.ModuleBehaviors;
using WizBot.Common.Yml;
using WizBot.Db;
using WizBot.Modules.Permissions.Common;
using WizBot.Modules.Permissions.Services;
using WizBot.Services.Database.Models;
using System.Runtime.CompilerServices;
using LinqToDB.EntityFrameworkCore;
using Wiz.Common;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WizBot.Modules.WizBotExpressions;

public sealed class WizBotExpressionsService : IExecOnMessage, IReadyExecutor
{
    private const string MENTION_PH = "%bot.mention%";

    private const string PREPEND_EXPORT =
        @"# Keys are triggers, Each key has a LIST of expressions in the following format:
# - res: Response string
#   id: Alphanumeric id used for commands related to the expression. (Note, when using .exprsimport, a new id will be generated.)
#   react: 
#     - <List
#     -  of
#     - reactions>
#   at: Whether expression allows targets (see .h .exprat) 
#   ca: Whether expression expects trigger anywhere (see .h .exprca) 
#   dm: Whether expression DMs the response (see .h .exprdm) 
#   ad: Whether expression automatically deletes triggering message (see .h .exprad) 

";

    private static readonly ISerializer _exportSerializer = new SerializerBuilder()
        .WithEventEmitter(args
            => new MultilineScalarFlowStyleEmitter(args))
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .WithIndentedSequences()
        .ConfigureDefaultValuesHandling(DefaultValuesHandling
            .OmitDefaults)
        .DisableAliases()
        .Build();

    public int Priority
        => 0;

    private readonly object _gexprWriteLock = new();

    private readonly TypedKey<WizBotExpression> _gexprAddedKey = new("gexpr.added");
    private readonly TypedKey<int> _gexprDeletedkey = new("gexpr.deleted");
    private readonly TypedKey<WizBotExpression> _gexprEditedKey = new("gexpr.edited");
    private readonly TypedKey<bool> _exprsReloadedKey = new("exprs.reloaded");

    // it is perfectly fine to have global expressions as an array
    // 1. expressions are almost never added (compared to how many times they are being looped through)
    // 2. only need write locks for this as we'll rebuild+replace the array on every edit
    // 3. there's never many of them (at most a thousand, usually < 100)
    private WizBotExpression[] globalExpressions;
    private ConcurrentDictionary<ulong, WizBotExpression[]> newguildExpressions;

    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly PermissionService _perms;
    private readonly CommandHandler _cmd;
    private readonly IBotStrings _strings;
    private readonly Bot _bot;
    private readonly GlobalPermissionService _gperm;
    private readonly CmdCdService _cmdCds;
    private readonly IPubSub _pubSub;
    private readonly IEmbedBuilderService _eb;
    private readonly Random _rng;

    private bool ready;
    private ConcurrentHashSet<ulong> _disabledGlobalExpressionGuilds;

    public WizBotExpressionsService(
        PermissionService perms,
        DbService db,
        IBotStrings strings,
        Bot bot,
        DiscordSocketClient client,
        CommandHandler cmd,
        GlobalPermissionService gperm,
        CmdCdService cmdCds,
        IPubSub pubSub,
        IEmbedBuilderService eb)
    {
        _db = db;
        _client = client;
        _perms = perms;
        _cmd = cmd;
        _strings = strings;
        _bot = bot;
        _gperm = gperm;
        _cmdCds = cmdCds;
        _pubSub = pubSub;
        _eb = eb;
        _rng = new WizBotRandom();

        _pubSub.Sub(_exprsReloadedKey, OnExprsShouldReload);
        pubSub.Sub(_gexprAddedKey, OnGexprAdded);
        pubSub.Sub(_gexprDeletedkey, OnGexprDeleted);
        pubSub.Sub(_gexprEditedKey, OnGexprEdited);

        bot.JoinedGuild += OnJoinedGuild;
        _client.LeftGuild += OnLeftGuild;
    }

    private async Task ReloadInternal(IReadOnlyList<ulong> allGuildIds)
    {
        await using var uow = _db.GetDbContext();
        var guildItems = await uow.Expressions.AsNoTracking()
            .Where(x => allGuildIds.Contains(x.GuildId.Value))
            .ToListAsync();

        newguildExpressions = guildItems.GroupBy(k => k.GuildId!.Value)
            .ToDictionary(g => g.Key,
                g => g.Select(x =>
                    {
                        x.Trigger = x.Trigger.Replace(MENTION_PH, _bot.Mention);
                        return x;
                    })
                    .ToArray())
            .ToConcurrent();

        _disabledGlobalExpressionGuilds = new(await uow.GuildConfigs
            .Where(x => x.DisableGlobalExpressions)
            .Select(x => x.GuildId)
            .ToListAsyncLinqToDB());

        lock (_gexprWriteLock)
        {
            var globalItems = uow.Expressions.AsNoTracking()
                .Where(x => x.GuildId == null || x.GuildId == 0)
                .AsEnumerable()
                .Select(x =>
                {
                    x.Trigger = x.Trigger.Replace(MENTION_PH, _bot.Mention);
                    return x;
                })
                .ToArray();

            globalExpressions = globalItems;
        }

        ready = true;
    }

    private WizBotExpression TryGetExpression(IUserMessage umsg)
    {
        if (!ready)
            return null;

        if (umsg.Channel is not SocketTextChannel channel)
            return null;

        var content = umsg.Content.Trim().ToLowerInvariant();

        if (newguildExpressions.TryGetValue(channel.Guild.Id, out var expressions) && expressions.Length > 0)
        {
            var expr = MatchExpressions(content, expressions);
            if (expr is not null)
                return expr;
        }

        if (_disabledGlobalExpressionGuilds.Contains(channel.Guild.Id))
            return null;

        var localGrs = globalExpressions;

        return MatchExpressions(content, localGrs);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private WizBotExpression MatchExpressions(in ReadOnlySpan<char> content, WizBotExpression[] exprs)
    {
        var result = new List<WizBotExpression>(1);
        for (var i = 0; i < exprs.Length; i++)
        {
            var expr = exprs[i];
            var trigger = expr.Trigger;
            if (content.Length > trigger.Length)
            {
                // if input is greater than the trigger, it can only work if:
                // it has CA enabled
                if (expr.ContainsAnywhere)
                {
                    // if ca is enabled, we have to check if it is a word within the content
                    var wp = content.GetWordPosition(trigger);

                    // if it is, then that's valid
                    if (wp != WordPosition.None)
                        result.Add(expr);

                    // if it's not, then it cant' work under any circumstance,
                    // because content is greater than the trigger length
                    // so it can't be equal, and it's not contained as a word
                    continue;
                }

                // if CA is disabled, and expr has AllowTarget, then the
                // content has to start with the trigger followed by a space
                if (expr.AllowTarget
                    && content.StartsWith(trigger, StringComparison.OrdinalIgnoreCase)
                    && content[trigger.Length] == ' ')
                    result.Add(expr);
            }
            else if (content.Length < expr.Trigger.Length)
            {
                // if input length is less than trigger length, it means
                // that the reaction can never be triggered
            }
            else
            {
                // if input length is the same as trigger length
                // reaction can only trigger if the strings are equal
                if (content.SequenceEqual(expr.Trigger))
                    result.Add(expr);
            }
        }

        if (result.Count == 0)
            return null;

        var cancelled = result.FirstOrDefault(x => x.Response == "-");
        if (cancelled is not null)
            return cancelled;

        return result[_rng.Next(0, result.Count)];
    }

    public async Task<bool> ExecOnMessageAsync(IGuild guild, IUserMessage msg)
    {
        // maybe this message is an expression
        var expr = TryGetExpression(msg);

        if (expr is null || expr.Response == "-")
            return false;

        if (await _cmdCds.TryBlock(guild, msg.Author, expr.Trigger))
            return false;

        try
        {
            if (_gperm.BlockedModules.Contains("ACTUALEXPRESSIONS"))
            {
                Log.Information(
                    "User {UserName} [{UserId}] tried to use an expression but 'ActualExpressions' are globally disabled",
                    msg.Author.ToString(),
                    msg.Author.Id);

                return true;
            }

            if (guild is SocketGuild sg)
            {
                var pc = _perms.GetCacheFor(guild.Id);
                if (!pc.Permissions.CheckPermissions(msg, expr.Trigger, "ACTUALEXPRESSIONS", out var index))
                {
                    if (pc.Verbose)
                    {
                        var permissionMessage = _strings.GetText(strs.perm_prevent(index + 1,
                                Format.Bold(pc.Permissions[index].GetCommand(_cmd.GetPrefix(guild), sg))),
                            sg.Id);

                        try
                        {
                            await msg.Channel.SendErrorAsync(_eb, permissionMessage);
                        }
                        catch
                        {
                        }

                        Log.Information("{PermissionMessage}", permissionMessage);
                    }

                    return true;
                }
            }

            var sentMsg = await expr.Send(msg, _client, false);

            var reactions = expr.GetReactions();
            foreach (var reaction in reactions)
            {
                try
                {
                    await sentMsg.AddReactionAsync(reaction.ToIEmote());
                }
                catch
                {
                    Log.Warning("Unable to add reactions to message {Message} in server {GuildId}",
                        sentMsg.Id,
                        expr.GuildId);
                    break;
                }

                await Task.Delay(1000);
            }

            if (expr.AutoDeleteTrigger)
            {
                try
                {
                    await msg.DeleteAsync();
                }
                catch
                {
                }
            }

            Log.Information("s: {GuildId} c: {ChannelId} u: {UserId} | {UserName} executed expression {Expr}",
                guild.Id,
                msg.Channel.Id,
                msg.Author.Id,
                msg.Author.ToString(),
                expr.Trigger);

            return true;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error in Expression RunBehavior: {ErrorMessage}", ex.Message);
        }

        return false;
    }

    public async Task ResetExprReactions(ulong? maybeGuildId, int id)
    {
        WizBotExpression expr;
        await using var uow = _db.GetDbContext();
        expr = uow.Expressions.GetById(id);
        if (expr is null)
            return;

        expr.Reactions = string.Empty;

        await uow.SaveChangesAsync();
    }

    private Task UpdateInternalAsync(ulong? maybeGuildId, WizBotExpression expr)
    {
        if (maybeGuildId is { } guildId)
            UpdateInternal(guildId, expr);
        else
            return _pubSub.Pub(_gexprEditedKey, expr);

        return Task.CompletedTask;
    }

    private void UpdateInternal(ulong? maybeGuildId, WizBotExpression expr)
    {
        if (maybeGuildId is { } guildId)
        {
            newguildExpressions.AddOrUpdate(guildId,
                new[] { expr },
                (_, old) =>
                {
                    var newArray = old.ToArray();
                    for (var i = 0; i < newArray.Length; i++)
                    {
                        if (newArray[i].Id == expr.Id)
                            newArray[i] = expr;
                    }

                    return newArray;
                });
        }
        else
        {
            lock (_gexprWriteLock)
            {
                var exprs = globalExpressions;
                for (var i = 0; i < exprs.Length; i++)
                {
                    if (exprs[i].Id == expr.Id)
                        exprs[i] = expr;
                }
            }
        }
    }

    private Task AddInternalAsync(ulong? maybeGuildId, WizBotExpression expr)
    {
        // only do this for perf purposes
        expr.Trigger = expr.Trigger.Replace(MENTION_PH, _client.CurrentUser.Mention);

        if (maybeGuildId is { } guildId)
            newguildExpressions.AddOrUpdate(guildId, new[] { expr }, (_, old) => old.With(expr));
        else
            return _pubSub.Pub(_gexprAddedKey, expr);

        return Task.CompletedTask;
    }

    private Task DeleteInternalAsync(ulong? maybeGuildId, int id)
    {
        if (maybeGuildId is { } guildId)
        {
            newguildExpressions.AddOrUpdate(guildId,
                Array.Empty<WizBotExpression>(),
                (key, old) => DeleteInternal(old, id, out _));

            return Task.CompletedTask;
        }

        lock (_gexprWriteLock)
        {
            var expr = Array.Find(globalExpressions, item => item.Id == id);
            if (expr is not null)
                return _pubSub.Pub(_gexprDeletedkey, expr.Id);
        }

        return Task.CompletedTask;
    }

    private WizBotExpression[] DeleteInternal(
        IReadOnlyList<WizBotExpression> exprs,
        int id,
        out WizBotExpression deleted)
    {
        deleted = null;
        if (exprs is null || exprs.Count == 0)
            return exprs as WizBotExpression[] ?? exprs?.ToArray();

        var newExprs = new WizBotExpression[exprs.Count - 1];
        for (int i = 0, k = 0; i < exprs.Count; i++, k++)
        {
            if (exprs[i].Id == id)
            {
                deleted = exprs[i];
                k--;
                continue;
            }

            newExprs[k] = exprs[i];
        }

        return newExprs;
    }

    public async Task SetExprReactions(ulong? guildId, int id, IEnumerable<string> emojis)
    {
        WizBotExpression expr;
        await using (var uow = _db.GetDbContext())
        {
            expr = uow.Expressions.GetById(id);
            if (expr is null)
                return;

            expr.Reactions = string.Join("@@@", emojis);

            await uow.SaveChangesAsync();
        }

        await UpdateInternalAsync(guildId, expr);
    }

    public async Task<(bool Sucess, bool NewValue)> ToggleExprOptionAsync(ulong? guildId, int id, ExprField field)
    {
        var newVal = false;
        WizBotExpression expr;
        await using (var uow = _db.GetDbContext())
        {
            expr = uow.Expressions.GetById(id);

            if (expr is null || expr.GuildId != guildId)
                return (false, false);
            if (field == ExprField.AutoDelete)
                newVal = expr.AutoDeleteTrigger = !expr.AutoDeleteTrigger;
            else if (field == ExprField.ContainsAnywhere)
                newVal = expr.ContainsAnywhere = !expr.ContainsAnywhere;
            else if (field == ExprField.DmResponse)
                newVal = expr.DmResponse = !expr.DmResponse;
            else if (field == ExprField.AllowTarget)
                newVal = expr.AllowTarget = !expr.AllowTarget;

            await uow.SaveChangesAsync();
        }

        await UpdateInternalAsync(guildId, expr);

        return (true, newVal);
    }

    public WizBotExpression GetExpression(ulong? guildId, int id)
    {
        using var uow = _db.GetDbContext();
        var expr = uow.Expressions.GetById(id);
        if (expr is null || expr.GuildId != guildId)
            return null;

        return expr;
    }

    public int DeleteAllExpressions(ulong guildId)
    {
        using var uow = _db.GetDbContext();
        var count = uow.Expressions.ClearFromGuild(guildId);
        uow.SaveChanges();

        newguildExpressions.TryRemove(guildId, out _);

        return count;
    }

    public bool ExpressionExists(ulong? guildId, string input)
    {
        input = input.ToLowerInvariant();

        var gexprs = globalExpressions;
        foreach (var t in gexprs)
        {
            if (t.Trigger == input)
                return true;
        }

        if (guildId is ulong gid && newguildExpressions.TryGetValue(gid, out var guildExprs))
        {
            foreach (var t in guildExprs)
            {
                if (t.Trigger == input)
                    return true;
            }
        }

        return false;
    }

    public string ExportExpressions(ulong? guildId)
    {
        var exprs = GetExpressionsFor(guildId);

        var exprsDict = exprs.GroupBy(x => x.Trigger).ToDictionary(x => x.Key, x => x.Select(ExportedExpr.FromModel));

        return PREPEND_EXPORT + _exportSerializer.Serialize(exprsDict).UnescapeUnicodeCodePoints();
    }

    public async Task<bool> ImportExpressionsAsync(ulong? guildId, string input)
    {
        Dictionary<string, List<ExportedExpr>> data;
        try
        {
            data = Yaml.Deserializer.Deserialize<Dictionary<string, List<ExportedExpr>>>(input);
            if (data.Sum(x => x.Value.Count) == 0)
                return false;
        }
        catch
        {
            return false;
        }

        await using var uow = _db.GetDbContext();
        foreach (var entry in data)
        {
            var trigger = entry.Key;
            await uow.Expressions.AddRangeAsync(entry.Value.Where(expr => !string.IsNullOrWhiteSpace(expr.Res))
                .Select(expr => new WizBotExpression
                {
                    GuildId = guildId,
                    Response = expr.Res,
                    Reactions = expr.React?.Join("@@@"),
                    Trigger = trigger,
                    AllowTarget = expr.At,
                    ContainsAnywhere = expr.Ca,
                    DmResponse = expr.Dm,
                    AutoDeleteTrigger = expr.Ad
                }));
        }

        await uow.SaveChangesAsync();
        await TriggerReloadExpressions();
        return true;
    }

    #region Event Handlers

    public Task OnReadyAsync()
        => ReloadInternal(_bot.GetCurrentGuildIds());

    private ValueTask OnExprsShouldReload(bool _)
        => new(ReloadInternal(_bot.GetCurrentGuildIds()));

    private ValueTask OnGexprAdded(WizBotExpression c)
    {
        lock (_gexprWriteLock)
        {
            var newGlobalReactions = new WizBotExpression[globalExpressions.Length + 1];
            Array.Copy(globalExpressions, newGlobalReactions, globalExpressions.Length);
            newGlobalReactions[globalExpressions.Length] = c;
            globalExpressions = newGlobalReactions;
        }

        return default;
    }

    private ValueTask OnGexprEdited(WizBotExpression c)
    {
        lock (_gexprWriteLock)
        {
            for (var i = 0; i < globalExpressions.Length; i++)
            {
                if (globalExpressions[i].Id == c.Id)
                {
                    globalExpressions[i] = c;
                    return default;
                }
            }

            // if edited expr is not found?!
            // add it
            OnGexprAdded(c);
        }

        return default;
    }

    private ValueTask OnGexprDeleted(int id)
    {
        lock (_gexprWriteLock)
        {
            var newGlobalReactions = DeleteInternal(globalExpressions, id, out _);
            globalExpressions = newGlobalReactions;
        }

        return default;
    }

    public Task TriggerReloadExpressions()
        => _pubSub.Pub(_exprsReloadedKey, true);

    #endregion

    #region Client Event Handlers

    private Task OnLeftGuild(SocketGuild arg)
    {
        newguildExpressions.TryRemove(arg.Id, out _);

        return Task.CompletedTask;
    }

    private async Task OnJoinedGuild(GuildConfig gc)
    {
        await using var uow = _db.GetDbContext();
        var exprs = await uow.Expressions.AsNoTracking().Where(x => x.GuildId == gc.GuildId).ToArrayAsync();

        newguildExpressions[gc.GuildId] = exprs;
    }

    #endregion

    #region Basic Operations

    public async Task<WizBotExpression> AddAsync(ulong? guildId, string key, string message)
    {
        key = key.ToLowerInvariant();
        var expr = new WizBotExpression
        {
            GuildId = guildId,
            Trigger = key,
            Response = message
        };

        if (expr.Response.Contains("%target%", StringComparison.OrdinalIgnoreCase))
            expr.AllowTarget = true;

        await using (var uow = _db.GetDbContext())
        {
            uow.Expressions.Add(expr);
            await uow.SaveChangesAsync();
        }

        await AddInternalAsync(guildId, expr);

        return expr;
    }

    public async Task<WizBotExpression> EditAsync(ulong? guildId, int id, string message)
    {
        await using var uow = _db.GetDbContext();
        var expr = uow.Expressions.GetById(id);

        if (expr is null || expr.GuildId != guildId)
            return null;

        // disable allowtarget if message had target, but it was removed from it
        if (!message.Contains("%target%", StringComparison.OrdinalIgnoreCase)
            && expr.Response.Contains("%target%", StringComparison.OrdinalIgnoreCase))
            expr.AllowTarget = false;

        expr.Response = message;

        // enable allow target if message is edited to contain target
        if (expr.Response.Contains("%target%", StringComparison.OrdinalIgnoreCase))
            expr.AllowTarget = true;

        await uow.SaveChangesAsync();
        await UpdateInternalAsync(guildId, expr);

        return expr;
    }


    public async Task<WizBotExpression> DeleteAsync(ulong? guildId, int id)
    {
        await using var uow = _db.GetDbContext();
        var toDelete = uow.Expressions.GetById(id);

        if (toDelete is null)
            return null;

        if ((toDelete.IsGlobal() && guildId is null) || guildId == toDelete.GuildId)
        {
            uow.Expressions.Remove(toDelete);
            await uow.SaveChangesAsync();
            await DeleteInternalAsync(guildId, id);
            return toDelete;
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WizBotExpression[] GetExpressionsFor(ulong? maybeGuildId)
    {
        if (maybeGuildId is { } guildId)
            return newguildExpressions.TryGetValue(guildId, out var exprs) ? exprs : Array.Empty<WizBotExpression>();

        return globalExpressions;
    }

    #endregion

    public async Task<bool> ToggleGlobalExpressionsAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var gc = ctx.GuildConfigsForId(guildId, set => set);
        var toReturn = gc.DisableGlobalExpressions = !gc.DisableGlobalExpressions;
        await ctx.SaveChangesAsync();

        if (toReturn)
            _disabledGlobalExpressionGuilds.Add(guildId);
        else
            _disabledGlobalExpressionGuilds.TryRemove(guildId);

        return toReturn;
    }
}