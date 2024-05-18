#nullable disable
using NadekoBot.Modules.Help.Common;
using NadekoBot.Modules.Help.Services;
using Newtonsoft.Json;
using System.Text;
using Nadeko.Common.Medusa;

namespace NadekoBot.Modules.Help;

public sealed partial class Help : NadekoModule<HelpService>
{
    public const string PATREON_URL = "https://patreon.com/nadekobot";
    public const string PAYPAL_URL = "https://paypal.me/Kwoth";

    private readonly ICommandsUtilityService _cus;
    private readonly CommandService _cmds;
    private readonly BotConfigService _bss;
    private readonly IPermissionChecker _perms;
    private readonly IServiceProvider _services;
    private readonly DiscordSocketClient _client;
    private readonly IBotStrings _strings;

    private readonly AsyncLazy<ulong> _lazyClientId;
    private readonly IMedusaLoaderService _medusae;

    public Help(
        ICommandsUtilityService _cus,
        IPermissionChecker perms,
        CommandService cmds,
        BotConfigService bss,
        IServiceProvider services,
        DiscordSocketClient client,
        IBotStrings strings,
        IMedusaLoaderService medusae)
    {
        this._cus = _cus;
        _cmds = cmds;
        _bss = bss;
        _perms = perms;
        _services = services;
        _client = client;
        _strings = strings;
        _medusae = medusae;

        _lazyClientId = new(async () => (await _client.GetApplicationInfoAsync()).Id);
    }

    public async Task<SmartText> GetHelpString()
    {
        var botSettings = _bss.Data;
        if (string.IsNullOrWhiteSpace(botSettings.HelpText) || botSettings.HelpText == "-")
            return default;

        var clientId = await _lazyClientId.Value;
        var repCtx = new ReplacementContext(Context)
                     .WithOverride("{0}", () => clientId.ToString())
                     .WithOverride("{1}", () => prefix)
                     .WithOverride("%prefix%", () => prefix)
                     .WithOverride("%bot.prefix%", () => prefix);

        var text = SmartText.CreateFrom(botSettings.HelpText);
        return await repSvc.ReplaceAsync(text, repCtx);
    }

    [Cmd]
    public async Task Modules(int page = 1)
    {
        if (--page < 0)
            return;

        var topLevelModules = new List<ModuleInfo>();
        foreach (var m in _cmds.Modules.GroupBy(x => x.GetTopLevelModule()).OrderBy(x => x.Key.Name).Select(x => x.Key))
        {
            var result = await _perms.CheckPermsAsync(ctx.Guild,
                ctx.Channel,
                ctx.User,
                m.Name,
                null);

#if GLOBAL_NADEKO
            if (m.Preconditions.Any(x => x is NoPublicBotAttribute))
                continue;
#endif

            if (result.IsAllowed)
                topLevelModules.Add(m);
        }

        await Response()
              .Paginated()
              .Items(topLevelModules)
              .PageSize(12)
              .CurrentPage(page)
              .AddFooter(false)
              .Page((items, _) =>
              {
                  var embed = _sender.CreateEmbed().WithOkColor().WithTitle(GetText(strs.list_of_modules));

                  if (!items.Any())
                  {
                      embed = embed.WithOkColor().WithDescription(GetText(strs.module_page_empty));
                      return embed;
                  }

                  items
                      .ToList()
                      .ForEach(module => embed.AddField($"{GetModuleEmoji(module.Name)} {module.Name}",
                          GetModuleDescription(module.Name)
                          + "\n"
                          + Format.Code(GetText(strs.module_footer(prefix, module.Name.ToLowerInvariant()))),
                          true));

                  return embed;
              })
              .SendAsync();
    }

    private string GetModuleDescription(string moduleName)
    {
        var key = GetModuleLocStr(moduleName);

        if (key.Key == strs.module_description_missing.Key)
        {
            var desc = _medusae
                       .GetLoadedMedusae(Culture)
                       .FirstOrDefault(m => m.Sneks
                                             .Any(x => x.Name.Equals(moduleName,
                                                 StringComparison.InvariantCultureIgnoreCase)))
                       ?.Description;

            if (desc is not null)
                return desc;
        }

        return GetText(key);
    }

    private LocStr GetModuleLocStr(string moduleName)
    {
        switch (moduleName.ToLowerInvariant())
        {
            case "help":
                return strs.module_description_help;
            case "administration":
                return strs.module_description_administration;
            case "expressions":
                return strs.module_description_expressions;
            case "searches":
                return strs.module_description_searches;
            case "utility":
                return strs.module_description_utility;
            case "games":
                return strs.module_description_games;
            case "gambling":
                return strs.module_description_gambling;
            case "music":
                return strs.module_description_music;
            case "nsfw":
                return strs.module_description_nsfw;
            case "permissions":
                return strs.module_description_permissions;
            case "xp":
                return strs.module_description_xp;
            case "medusa":
                return strs.module_description_medusa;
            case "patronage":
                return strs.module_description_patronage;
            default:
                return strs.module_description_missing;
        }
    }

    private string GetModuleEmoji(string moduleName)
    {
        moduleName = moduleName.ToLowerInvariant();
        switch (moduleName)
        {
            case "help":
                return "❓";
            case "administration":
                return "🛠️";
            case "expressions":
                return "🗣️";
            case "searches":
                return "🔍";
            case "utility":
                return "🔧";
            case "games":
                return "🎲";
            case "gambling":
                return "💰";
            case "music":
                return "🎶";
            case "nsfw":
                return "😳";
            case "permissions":
                return "🚓";
            case "xp":
                return "📝";
            case "patronage":
                return "💝";
            default:
                return "📖";
        }
    }

    [Cmd]
    [NadekoOptions<CommandsOptions>]
    public async Task Commands(string module = null, params string[] args)
    {
        if (string.IsNullOrWhiteSpace(module))
        {
            await Modules();
            return;
        }

        var (opts, _) = OptionsParser.ParseFrom(new CommandsOptions(), args);

        // Find commands for that module
        // don't show commands which are blocked
        // order by name
        var allowed = new List<CommandInfo>();

        foreach (var cmd in _cmds.Commands
                                 .Where(c => c.Module.GetTopLevelModule()
                                              .Name
                                              .StartsWith(module, StringComparison.InvariantCultureIgnoreCase)))
        {
            var result = await _perms.CheckPermsAsync(ctx.Guild,
                ctx.Channel,
                ctx.User,
                cmd.Module.GetTopLevelModule().Name,
                cmd.Name);

            if (result.IsAllowed)
                allowed.Add(cmd);
        }

        var cmds = allowed.OrderBy(c => c.Aliases[0])
                          .DistinctBy(x => x.Aliases[0])
                          .ToList();


        // check preconditions for all commands, but only if it's not 'all'
        // because all will show all commands anyway, no need to check
        var succ = new HashSet<CommandInfo>();
        if (opts.View != CommandsOptions.ViewType.All)
        {
            succ =
            [
                ..(await cmds.Select(async x =>
                             {
                                 var pre = await x.CheckPreconditionsAsync(Context, _services);
                                 return (Cmd: x, Succ: pre.IsSuccess);
                             })
                             .WhenAll()).Where(x => x.Succ)
                                        .Select(x => x.Cmd)
            ];

            if (opts.View == CommandsOptions.ViewType.Hide)
                // if hidden is specified, completely remove these commands from the list
                cmds = cmds.Where(x => succ.Contains(x)).ToList();
        }

        var cmdsWithGroup = cmds.GroupBy(c => c.Module.GetGroupName())
                                .OrderBy(x => x.Key == x.First().Module.Name ? int.MaxValue : x.Count())
                                .ToList();

        if (cmdsWithGroup.Count == 0)
        {
            if (opts.View != CommandsOptions.ViewType.Hide)
                await Response().Error(strs.module_not_found).SendAsync();
            else
                await Response().Error(strs.module_not_found_or_cant_exec).SendAsync();
            return;
        }

        var cnt = 0;
        var groups = cmdsWithGroup.GroupBy(_ => cnt++ / 48).ToArray();
        var embed = _sender.CreateEmbed().WithOkColor();
        foreach (var g in groups)
        {
            var last = g.Count();
            for (var i = 0; i < last; i++)
            {
                var transformed = g.ElementAt(i)
                                   .Select(x =>
                                   {
                                       //if cross is specified, and the command doesn't satisfy the requirements, cross it out
                                       if (opts.View == CommandsOptions.ViewType.Cross)
                                       {
                                           return $"{(succ.Contains(x) ? "✅" : "❌")} {prefix + x.Aliases[0]}";
                                       }

                                       if (x.Aliases.Count == 1)
                                           return prefix + x.Aliases[0];

                                       return prefix + x.Aliases[0] + " | " + prefix + x.Aliases[1];
                                   });

                embed.AddField(g.ElementAt(i).Key, "" + string.Join("\n", transformed) + "", true);
            }
        }

        embed.WithFooter(GetText(strs.commands_instr(prefix)));
        await Response().Embed(embed).SendAsync();
    }

    private async Task Group(ModuleInfo group)
    {
        var eb = _sender.CreateEmbed()
                        .WithTitle(GetText(strs.cmd_group_commands(group.Name)))
                        .WithOkColor();

        foreach (var cmd in group.Commands.DistinctBy(x => x.Aliases[0]))
        {
            string cmdName;
            if (cmd.Aliases.Count > 1)
                cmdName = Format.Code(prefix + cmd.Aliases[0]) + " | " + Format.Code(prefix + cmd.Aliases[1]);
            else
                cmdName = Format.Code(prefix + cmd.Aliases.First());

            eb.AddField(cmdName, cmd.RealSummary(_strings, _medusae, Culture, prefix));
        }

        await Response().Embed(eb).SendAsync();
    }

    [Cmd]
    [Priority(0)]
    public async Task H([Leftover] string fail)
    {
        var prefixless =
            _cmds.Commands.FirstOrDefault(x => x.Aliases.Any(cmdName => cmdName.ToLowerInvariant() == fail));
        if (prefixless is not null)
        {
            await H(prefixless);
            return;
        }

        if (fail.StartsWith(prefix))
            fail = fail.Substring(prefix.Length);

        var group = _cmds.Modules
                         .SelectMany(x => x.Submodules)
                         .Where(x => !string.IsNullOrWhiteSpace(x.Group))
                         .FirstOrDefault(x => x.Group.Equals(fail, StringComparison.InvariantCultureIgnoreCase));

        if (group is not null)
        {
            await Group(group);
            return;
        }

        await Response().Error(strs.command_not_found).SendAsync();
    }

    [Cmd]
    [Priority(1)]
    public async Task H([Leftover] CommandInfo com = null)
    {
        var channel = ctx.Channel;
        if (com is null)
        {
            try
            {
                var ch = channel is ITextChannel ? await ctx.User.CreateDMChannelAsync() : channel;
                var data = await GetHelpString();
                if (data == default)
                    return;

                await Response().Channel(ch).Text(data).SendAsync();
                try
                {
                    await ctx.OkAsync();
                }
                catch
                {
                } // ignore if bot can't react
            }
            catch (Exception)
            {
                await Response().Error(strs.cant_dm).SendAsync();
            }

            return;
        }

        var embed = _cus.GetCommandHelp(com, ctx.Guild);
        await _sender.Response(channel).Embed(embed).SendAsync();
    }

    [Cmd]
    [OwnerOnly]
    public async Task GenCmdList()
    {
        _ = ctx.Channel.TriggerTypingAsync();

        // order commands by top level module name
        // and make a dictionary of <ModuleName, Array<JsonCommandData>>
        var cmdData = _cmds.Commands.GroupBy(x => x.Module.GetTopLevelModule().Name)
                           .OrderBy(x => x.Key)
                           .ToDictionary(x => x.Key,
                               x => x.DistinctBy(c => c.Aliases.First())
                                     .Select(com =>
                                     {
                                         List<string> optHelpStr = null;

                                         var opt = CommandsUtilityService.GetNadekoOptionType(com.Attributes);
                                         if (opt is not null)
                                             optHelpStr = CommandsUtilityService.GetCommandOptionHelpList(opt);

                                         return new CommandJsonObject
                                         {
                                             Aliases = com.Aliases.Select(alias => prefix + alias).ToArray(),
                                             Description = com.RealSummary(_strings, _medusae, Culture, prefix),
                                             Usage = com.RealRemarksArr(_strings, _medusae, Culture, prefix),
                                             Submodule = com.Module.Name,
                                             Module = com.Module.GetTopLevelModule().Name,
                                             Options = optHelpStr,
                                             Requirements = CommandsUtilityService.GetCommandRequirements(com)
                                         };
                                     })
                                     .ToList());

        var readableData = JsonConvert.SerializeObject(cmdData, Formatting.Indented);
        
        // send the indented file to chat
        await using var rDataStream = new MemoryStream(Encoding.ASCII.GetBytes(readableData));
        await ctx.Channel.SendFileAsync(rDataStream, "cmds.json", GetText(strs.commandlist_regen));
    }

    [Cmd]
    public async Task Guide()
        => await Response()
                 .Confirm(strs.guide("https://nadeko.bot/commands",
                     "https://nadekobot.readthedocs.io/en/latest/"))
                 .SendAsync();


    private Task SelfhostAction(SocketMessageComponent smc, object _)
        => smc.RespondConfirmAsync(_sender,
            """
            - In case you don't want or cannot Donate to NadekoBot project, but you
            - NadekoBot is a free and [open source](https://gitlab.com/kwoth/nadekobot) project which means you can run your own "selfhosted" instance on your computer.

            *Keep in mind that running the bot on your computer means that the bot will be offline when you turn off your computer*

            - You can find the selfhosting guides by using the `.guide` command and clicking on the second link that pops up.
            - If you decide to selfhost the bot, still consider [supporting the project](https://patreon.com/join/nadekobot) to keep the development going :)
            """,
            true);

    [Cmd]
    [OnlyPublicBot]
    public async Task Donate()
    {
        var selfhostInter = _inter.Create(ctx.User.Id,
            new SimpleInteraction<object>(new ButtonBuilder(
                    emote: new Emoji("🖥️"),
                    customId: "donate:selfhosting",
                    label: "Selfhosting"),
                SelfhostAction));

        var eb = _sender.CreateEmbed()
                        .WithOkColor()
                        .WithTitle("Thank you for considering to donate to the NadekoBot project!");

        eb
            .WithDescription("""
                             NadekoBot relies on donations to keep the servers, services and APIs running.
                             Donating will give you access to some exclusive features. You can read about them on the [patreon page](https://patreon.com/join/nadekobot)
                             """)
            .AddField("Donation Instructions",
                $"""
                 🗒️ Before pledging it is recommended to open your DMs as Nadeko will send you a welcome message with instructions after you pledge has been processed and confirmed.

                 **Step 1:** ❤️ Pledge on Patreon ❤️

                 `1.` Go to <https://patreon.com/join/nadekobot> and choose a tier.
                 `2.` Make sure your payment is processed and accepted.

                 **Step 2** 🤝 Connect your Discord account 🤝

                 `1.` Go to your profile settings on Patreon and connect your Discord account to it.
                 *please make sure you're logged into the correct Discord account*

                 If you do not know how to do it, you may [follow instructions here](https://support.patreon.com/hc/en-us/articles/212052266-How-do-I-connect-Discord-to-Patreon-Patron-)

                 **Step 3** ⏰ Wait a short while (usually 1-3 minutes) ⏰
                   
                 Nadeko will DM you the welcome instructions, and you will receive your rewards!
                 🎉 **Enjoy!** 🎉
                 """);

        try
        {
            await Response()
                  .Channel(await ctx.User.CreateDMChannelAsync())
                  .Embed(eb)
                  .Interaction(selfhostInter)
                  .SendAsync();

            _ = ctx.OkAsync();
        }
        catch
        {
            await Response().Error(strs.cant_dm).SendAsync();
        }
    }
}