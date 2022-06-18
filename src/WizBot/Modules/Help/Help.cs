#nullable disable
using Amazon.S3;
using WizBot.Medusa;
using WizBot.Modules.Help.Common;
using WizBot.Modules.Help.Services;
using WizBot.Modules.Permissions.Services;
using Newtonsoft.Json;
using System.Text;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace WizBot.Modules.Help;

public partial class Help : WizBotModule<HelpService>
{
    public const string PATREON_URL = "https://patreon.com/wiznet";
    public const string PAYPAL_URL = "https://paypal.me/Wizkiller96Network";

    private readonly CommandService _cmds;
    private readonly BotConfigService _bss;
    private readonly GlobalPermissionService _perms;
    private readonly IServiceProvider _services;
    private readonly DiscordSocketClient _client;
    private readonly IBotStrings _strings;

    private readonly AsyncLazy<ulong> _lazyClientId;
    private readonly IMedusaLoaderService _medusae;

    public Help(
        GlobalPermissionService perms,
        CommandService cmds,
        BotConfigService bss,
        IServiceProvider services,
        DiscordSocketClient client,
        IBotStrings strings,
        IMedusaLoaderService medusae)
    {
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
        var r = new ReplacementBuilder().WithDefault(Context)
                                        .WithOverride("{0}", () => clientId.ToString())
                                        .WithOverride("{1}", () => prefix)
                                        .WithOverride("%prefix%", () => prefix)
                                        .WithOverride("%bot.prefix%", () => prefix)
                                        .Build();

        var text = SmartText.CreateFrom(botSettings.HelpText);
        return r.Replace(text);
    }

    [Cmd]
    public async partial Task Modules(int page = 1)
    {
        if (--page < 0)
            return;

        var topLevelModules = _cmds.Modules.GroupBy(m => m.GetTopLevelModule())
                                   .Where(m => !_perms.BlockedModules.Contains(m.Key.Name.ToLowerInvariant()))
                                   .Select(x => x.Key)
                                   .ToList();

        await ctx.SendPaginatedConfirmAsync(page,
            cur =>
            {
                var embed = _eb.Create().WithOkColor().WithTitle(GetText(strs.list_of_modules));

                var localModules = topLevelModules.Skip(12 * cur).Take(12).ToList();

                if (!localModules.Any())
                {
                    embed = embed.WithOkColor().WithDescription(GetText(strs.module_page_empty));
                    return embed;
                }

                localModules.OrderBy(module => module.Name)
                            .ToList()
                            .ForEach(module => embed.AddField($"{GetModuleEmoji(module.Name)} {module.Name}",
                                GetText(GetModuleLocStr(module.Name))
                                + "\n"
                                + Format.Code(GetText(strs.module_footer(prefix, module.Name.ToLowerInvariant()))),
                                true));

                return embed;
            },
            topLevelModules.Count(),
            12,
            false);
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
            case "roblox":
                return strs.module_description_roblox;
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
            case "customreactions":
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
            case "roblox":
                return "🟥";
            default:
                return "📖";
        }
    }

    [Cmd]
    [WizBotOptions(typeof(CommandsOptions))]
    public async partial Task Commands(string module = null, params string[] args)
    {
        module = module?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(module))
        {
            await Modules();
            return;
        }

        var (opts, _) = OptionsParser.ParseFrom(new CommandsOptions(), args);

        // Find commands for that module
        // don't show commands which are blocked
        // order by name
        var cmds = _cmds.Commands
                        .Where(c => c.Module.GetTopLevelModule()
                                     .Name.ToUpperInvariant()
                                     .StartsWith(module, StringComparison.InvariantCulture))
                        .Where(c => !_perms.BlockedCommands.Contains(c.Aliases[0].ToLowerInvariant()))
                        .OrderBy(c => c.Aliases[0])
                        .DistinctBy(x => x.Aliases[0])
                        .ToList();


        // check preconditions for all commands, but only if it's not 'all'
        // because all will show all commands anyway, no need to check
        var succ = new HashSet<CommandInfo>();
        if (opts.View != CommandsOptions.ViewType.All)
        {
            succ = new((await cmds.Select(async x =>
                                  {
                                      var pre = await x.CheckPreconditionsAsync(Context, _services);
                                      return (Cmd: x, Succ: pre.IsSuccess);
                                  })
                                  .WhenAll()).Where(x => x.Succ)
                                             .Select(x => x.Cmd));

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
                await ReplyErrorLocalizedAsync(strs.module_not_found);
            else
                await ReplyErrorLocalizedAsync(strs.module_not_found_or_cant_exec);
            return;
        }

        var cnt = 0;
        var groups = cmdsWithGroup.GroupBy(_ => cnt++ / 48).ToArray();
        var embed = _eb.Create().WithOkColor();
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
                                           return
                                               $"{(succ.Contains(x) ? "✅" : "❌")}{prefix + x.Aliases.First(),-15} {"[" + x.Aliases.Skip(1).FirstOrDefault() + "]",-8}";
                                       }

                                       return
                                           $"{prefix + x.Aliases.First(),-15} {"[" + x.Aliases.Skip(1).FirstOrDefault() + "]",-8}";
                                   });

                if (i == last - 1 && (i + 1) % 2 != 0)
                {
                    transformed = transformed.Chunk(2)
                                             .Select(x =>
                                             {
                                                 if (x.Count() == 1)
                                                     return $"{x.First()}";
                                                 return string.Concat(x);
                                             });
                }

                embed.AddField(g.ElementAt(i).Key, "```css\n" + string.Join("\n", transformed) + "\n```", true);
            }
        }

        embed.WithFooter(GetText(strs.commands_instr(prefix)));
        await ctx.Channel.EmbedAsync(embed);
    }
    
    private async Task Group(ModuleInfo group)
    {
        var eb = _eb.Create(ctx)
                    .WithTitle(GetText(strs.cmd_group_commands(group.Name)))
                    .WithOkColor();

        foreach (var cmd in group.Commands)
        {
            eb.AddField(prefix + cmd.Aliases.First(), cmd.RealSummary(_strings, _medusae, Culture, prefix));
        }

        await ctx.Channel.EmbedAsync(eb);
    }

    [Cmd]
    [Priority(0)]
    public async partial Task H([Leftover] string fail)
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

        await ReplyErrorLocalizedAsync(strs.command_not_found);
    }

    [Cmd]
    [Priority(1)]
    public async partial Task H([Leftover] CommandInfo com = null)
    {
        var channel = ctx.Channel;

        if (com is null)
        {
            var ch = channel is ITextChannel ? await ctx.User.CreateDMChannelAsync() : channel;
            try
            {
                var data = await GetHelpString();
                if (data == default)
                    return;
                await ch.SendAsync(data);
                try { await ctx.OkAsync(); }
                catch { } // ignore if bot can't react
            }
            catch (Exception)
            {
                await ReplyErrorLocalizedAsync(strs.cant_dm);
            }

            return;
        }

        var embed = _service.GetCommandHelp(com, ctx.Guild);
        await channel.EmbedAsync(embed);
    }

    [Cmd]
    [OwnerOnly]
    public async partial Task GenCmdList()
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
                                         var opt = ((WizBotOptionsAttribute)com.Attributes.FirstOrDefault(static x
                                                 => x is WizBotOptionsAttribute))
                                             ?.OptionType;
                                         if (opt is not null)
                                             optHelpStr = HelpService.GetCommandOptionHelpList(opt);

                                         return new CommandJsonObject
                                         {
                                             Aliases = com.Aliases.Select(alias => prefix + alias).ToArray(),
                                             Description = com.RealSummary(_strings, _medusae, Culture, prefix),
                                             Usage = com.RealRemarksArr(_strings, _medusae, Culture, prefix),
                                             Submodule = com.Module.Name,
                                             Module = com.Module.GetTopLevelModule().Name,
                                             Options = optHelpStr,
                                             Requirements = HelpService.GetCommandRequirements(com)
                                         };
                                     })
                                     .ToList());

        var readableData = JsonConvert.SerializeObject(cmdData, Formatting.Indented);
        var uploadData = JsonConvert.SerializeObject(cmdData, Formatting.None);

        // for example https://nyc.digitaloceanspaces.com (without your space name)
        var serviceUrl = Environment.GetEnvironmentVariable("do_spaces_address");

        // generate spaces access key on https://cloud.digitalocean.com/account/api/tokens
        // you will get 2 keys, first, shorter one is id, longer one is secret
        var accessKey = Environment.GetEnvironmentVariable("do_access_key_id");
        var secretAcccessKey = Environment.GetEnvironmentVariable("do_access_key_secret");

        // if all env vars are set, upload the unindented file (to save space) there
        if (!(serviceUrl is null || accessKey is null || secretAcccessKey is null))
        {
            var config = new AmazonS3Config
            {
                ServiceURL = serviceUrl
            };

            using var dlClient = new AmazonS3Client(accessKey, secretAcccessKey, config);

            using (var client = new AmazonS3Client(accessKey, secretAcccessKey, config))
            {
                await client.PutObjectAsync(new()
                {
                    BucketName = "wizbot-images",
                    ContentType = "application/json",
                    ContentBody = uploadData,
                    // either use a path provided in the argument or the default one for public wizbot, other/cmds.json
                    Key = $"cmds/{StatsService.BOT_VERSION}.json",
                    CannedACL = S3CannedACL.PublicRead
                });
            }

            var versionListString = "[]";
            try
            {
                using var oldVersionObject = await dlClient.GetObjectAsync(new()
                {
                    BucketName = "wizbot-images",
                    Key = "cmds/versions.json"
                });

                await using var ms = new MemoryStream();
                await oldVersionObject.ResponseStream.CopyToAsync(ms);
                versionListString = Encoding.UTF8.GetString(ms.ToArray());
            }
            catch (Exception)
            {
                Log.Information("No old version list found. Creating a new one.");
            }

            var versionList = JsonSerializer.Deserialize<List<string>>(versionListString);
            if (versionList is not null && !versionList.Contains(StatsService.BOT_VERSION))
            {
                // save the file with new version added
                // versionList.Add(StatsService.BotVersion);
                versionListString = JsonSerializer.Serialize(versionList.Prepend(StatsService.BOT_VERSION),
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                // upload the updated version list
                using var client = new AmazonS3Client(accessKey, secretAcccessKey, config);
                await client.PutObjectAsync(new()
                {
                    BucketName = "wizbot-images",
                    ContentType = "application/json",
                    ContentBody = versionListString,
                    // either use a path provided in the argument or the default one for public wizbot, other/cmds.json
                    Key = "cmds/versions.json",
                    CannedACL = S3CannedACL.PublicRead
                });
            }
            else
            {
                Log.Warning(
                    "Version {Version} already exists in the version file. " + "Did you forget to increment it?",
                    StatsService.BOT_VERSION);
            }
        }

        // also send the file, but indented one, to chat
        await using var rDataStream = new MemoryStream(Encoding.ASCII.GetBytes(readableData));
        await ctx.Channel.SendFileAsync(rDataStream, "cmds.json", GetText(strs.commandlist_regen));
    }
    
    [Cmd]
        public async partial Task Feedback(string type, [Remainder] string message)
        {
        
#if GLOBAL_WIZBOT

            string[] rtypes = { "Bug", "Suggestion", "bug", "suggestion" };

            if (type == "Bug" || type == "bug")
            {
                type = "Bug";
            } else if (type == "Suggestion" || type == "suggestion")
            {
                type = "Suggestion";
            }        

            if (string.IsNullOrWhiteSpace(type))
                return;

            if (string.IsNullOrWhiteSpace(message))
                return;

            if (rtypes.Contains(type))
            {
                await _client.GetGuild(99273784988557312).GetTextChannel(566998481177280512).SendMessageAsync("<@99272781513920512>");
                await _client.GetGuild(99273784988557312).GetTextChannel(566998481177280512).EmbedAsync(_eb.Create().WithOkColor()
                    .WithTitle($"New Bug/Suggestion Report")
                    .WithThumbnailUrl($"{ctx.User.GetAvatarUrl()}")
                    .AddField("Reporter", $"{ctx.User}", true)
                    .AddField("Reporter ID:", $"{ctx.User.Id}", true)
                    .AddField("Server Name:", $"{ctx.Guild.Name}", true)
                    .AddField("Server ID:", $"{ctx.Guild.Id}", true)
                    .AddField("Channel Name:", $"{ctx.Channel.Name}", true)
                    .AddField("Channel ID:", $"{ctx.Channel.Id}", true)
                    .AddField("Report Type:", type, false)
                    .AddField("Message:", $"{message}")).ConfigureAwait(false);

                await ctx.Channel.SendMessageAsync("🆗").ConfigureAwait(false);
            }
            else
                await ctx.Channel.EmbedAsync(_eb.Create().WithErrorColor()
                    .WithTitle($"Error: Report not sent.")
                    .WithDescription("Please make sure you used the correct report types listed below.")
                    .AddField("Report Types:", "`Bug`, `Suggestion`")).ConfigureAwait(false);
#else

            await ctx.Channel.EmbedAsync(_eb.Create().WithErrorColor()
                .WithTitle("Command Restricted")
                .WithDescription("This command is disabled on self-host bots.")).ConfigureAwait(false);
            
#endif
        }

        [Cmd]
        public async partial Task Report(IGuildUser ruser, [Remainder] string rexplaination)
        {
            
#if GLOBAL_WIZBOT

            var user = ruser ?? ctx.User as IGuildUser;

            if (((user == null)) && (string.IsNullOrEmpty(rexplaination)))
            {
                await ctx.Channel.EmbedAsync(_eb.Create().WithErrorColor()
                    .WithTitle($"Error: Abuse report not sent.")
                    .WithDescription("Please make sure you filled out all the fields correctly.")).ConfigureAwait(false);
            }
            else if (user == null)
            {
                await ctx.Channel.EmbedAsync(_eb.Create().WithErrorColor()
                    .WithTitle($"Error: Abuse report not sent.")
                    .WithDescription("Please make sure you provided the username of the person you are reporting.")).ConfigureAwait(false);
            }
            else if (string.IsNullOrEmpty(rexplaination))
            {
                await ctx.Channel.EmbedAsync(_eb.Create().WithErrorColor()
                    .WithTitle($"Error: Abuse report not sent.")
                    .WithDescription("Please make sure you provided and explaination in your report.")).ConfigureAwait(false);
            }
            else
                await _client.GetGuild(99273784988557312).GetTextChannel(590829242690961408).SendMessageAsync("<@&367646195889471499>");
                await _client.GetGuild(99273784988557312).GetTextChannel(590829242690961408).EmbedAsync(_eb.Create().WithOkColor()
                    .WithTitle($"Abuse Report")
                    .WithThumbnailUrl($"{ctx.User.GetAvatarUrl()}")
                    .AddField("Reporter:", $"{ctx.User}", true)
                    .AddField("Reporter ID:", $"{ctx.User.Id}", true)
                    .AddField("Server Name:", $"{ctx.Guild.Name}", true)
                    .AddField("Server ID:", $"{ctx.Guild.Id}", true)
                    .AddField("Channel Name:", $"{ctx.Channel.Name}", true)
                    .AddField("Channel ID:", $"{ctx.Channel.Id}", true)
                    .AddField("Reported User:", $"**{user.Username}**#{user.Discriminator} | {user.Id.ToString()}", false)
                    .AddField("Explaination/Proof:", $"{rexplaination}")).ConfigureAwait(false);

                await ctx.Channel.SendMessageAsync("Report sent to WizBot's Staff.").ConfigureAwait(false);

#else

            await ctx.Channel.EmbedAsync(_eb.Create().WithErrorColor()
                .WithTitle("Command Restricted")
                .WithDescription("This command is disabled on self-host bots.")).ConfigureAwait(false);
            
#endif
        }

    [Cmd]
    public async partial Task Guide()
        => await ConfirmLocalizedAsync(strs.guide("https://commands.wizbot.cc",
            "https://wizbot.readthedocs.io/en/latest/"));


    private Task SelfhostAction(SocketMessageComponent smc)
        => smc.RespondConfirmAsync(_eb,
            @"- In case you don't want or cannot Donate to WizBot project, but you 
- WizBot is a completely free and fully [open source](https://gitlab.com/WizNet/WizBot) project which means you can run your own ""selfhosted"" instance on your computer or server for free.

*Keep in mind that running the bot on your computer means that the bot will be offline when you turn off your computer*

- You can find the selfhosting guides by using the `.guide` command and clicking on the second link that pops up.
- If you decide to selfhost the bot, still consider [supporting the project](https://patreon.com/join/wiznet) to keep the development going :)",
            true);

    [Cmd]
    [OnlyPublicBot]
    public async partial Task Donate()
        {
        var selfhostInter = new DonateSelfhostingInteraction(_client, ctx.User.Id, SelfhostAction);
        
        var eb = _eb.Create(ctx)
                    .WithOkColor()
                    .WithTitle("Thank you for considering to donate to the WizBot project!");

        eb
            .WithDescription("WizBot relies on donations to keep the servers, services and APIs running.\n"
                             + "Donating will give you access to some exclusive features. You can read about them on the [patreon page](https://patreon.com/join/wiznet)")
            .AddField("Donation Instructions",
                $@"
🗒️ Before pledging it is recommended to open your DMs as WizBot will send you a welcome message with instructions after you pledge has been processed and confirmed.

**Step 1:** ❤️ Pledge on Patreon ❤️

`1.` Go to <https://patreon.com/join/wiznet> and choose a tier.
`2.` Make sure your payment is processed and accepted.

**Step 2** 🤝 Connect your Discord account 🤝

`1.` Go to your profile settings on Patreon and connect your Discord account to it.
*please make sure you're logged into the correct Discord account*

If you do not know how to do it, you may follow instructions in this link:
<https://support.patreon.com/hc/en-us/articles/212052266-How-do-I-connect-Discord-to-Patreon-Patron->

**Step 3** ⏰ Wait a short while (usually 1-3 minutes) ⏰
  
WizBot will DM you the welcome instructions, and you may start using the patron-only commands and features!
🎉 **Enjoy!** 🎉
")
            .AddField("Troubleshooting",
                @"
*In case you didn't receive the rewards within 5 minutes:*
`1.` Make sure your DMs are open to everyone. Maybe your pledge was processed successfully but the bot was unable to DM you. Use the `.patron` command to check your status.
`2.` Make sure you've connected the CORRECT Discord account. Quite often users log in to different Discord accounts in their browser. You may also try disconnecting and reconnecting your account.
`3.` Make sure your payment has been processed and not declined by Patreon.
`4.` If any of the previous steps don't help, you can join the WizNet support server <https://wizbot.cc/discord> and ask for help in the #help channel");

        try
        {
            await (await ctx.User.CreateDMChannelAsync()).EmbedAsync(eb, inter: selfhostInter.GetInteraction());
            _ = ctx.OkAsync();
        }
        catch
        {
            await ReplyErrorLocalizedAsync(strs.cant_dm);
        }
    }
}