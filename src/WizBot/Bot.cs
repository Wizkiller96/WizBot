#nullable disable
using Microsoft.Extensions.DependencyInjection;
using WizBot.Common.Configs;
using WizBot.Common.ModuleBehaviors;
using WizBot.Db;
using WizBot.Modules.Administration;
using WizBot.Modules.Utility;
using WizBot.Services.Database.Models;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using Wiz.Common;
using RunMode = Discord.Commands.RunMode;

namespace WizBot;

public sealed class Bot
{
    public event Func<GuildConfig, Task> JoinedGuild = delegate { return Task.CompletedTask; };

    public DiscordSocketClient Client { get; }
    public ImmutableArray<GuildConfig> AllGuildConfigs { get; private set; }

    private IServiceProvider Services { get; set; }

    public string Mention { get; private set; }
    public bool IsReady { get; private set; }
    public int ShardId { get; set; }

    private readonly IBotCredentials _creds;
    private readonly CommandService _commandService;
    private readonly DbService _db;

    private readonly IBotCredsProvider _credsProvider;
    // private readonly InteractionService _interactionService;

    public Bot(int shardId, int? totalShards, string credPath = null)
    {
        if (shardId < 0)
            throw new ArgumentOutOfRangeException(nameof(shardId));

        ShardId = shardId;
        _credsProvider = new BotCredsProvider(totalShards, credPath);
        _creds = _credsProvider.GetCreds();

        _db = new(_credsProvider);

        var messageCacheSize =
            50;

        if(!_creds.UsePrivilegedIntents)
            Log.Warning("You are not using privileged intents. Some features will not work properly");
        
        Client = new(new()
        {
            MessageCacheSize = messageCacheSize,
            LogLevel = LogSeverity.Warning,
            ConnectionTimeout = int.MaxValue,
            TotalShards = _creds.TotalShards,
            ShardId = shardId,
            AlwaysDownloadUsers = false,
            AlwaysResolveStickers = false,
            AlwaysDownloadDefaultStickers = false,
            GatewayIntents = _creds.UsePrivilegedIntents
                ? GatewayIntents.All
                : GatewayIntents.AllUnprivileged,
            LogGatewayIntentWarnings = false,
            FormatUsersInBidirectionalUnicode = false,
            DefaultRetryMode = RetryMode.AlwaysRetry ^ RetryMode.RetryRatelimit
        });

        _commandService = new(new()
        {
            CaseSensitiveCommands = false,
            DefaultRunMode = RunMode.Sync,
        });

        // _interactionService = new(Client.Rest);

        Client.Log += Client_Log;
    }


    public List<ulong> GetCurrentGuildIds()
        => Client.Guilds.Select(x => x.Id).ToList();

    private void AddServices()
    {
        var startingGuildIdList = GetCurrentGuildIds();
        var sw = Stopwatch.StartNew();
        var bot = Client.CurrentUser;

        using (var uow = _db.GetDbContext())
        {
            uow.EnsureUserCreated(bot.Id, bot.Username, bot.Discriminator, bot.AvatarId);
            AllGuildConfigs = uow.GuildConfigs.GetAllGuildConfigs(startingGuildIdList).ToImmutableArray();
        }

        var svcs = new ServiceCollection().AddTransient(_ => _credsProvider.GetCreds()) // bot creds
                                          .AddSingleton(_credsProvider)
                                          .AddSingleton(_db) // database
                                          .AddSingleton(Client) // discord socket client
                                          .AddSingleton(_commandService)
                                          // .AddSingleton(_interactionService)
                                          .AddSingleton(this)
                                          .AddSingleton<ISeria, JsonSeria>()
                                          .AddSingleton<IConfigSeria, YamlSeria>()
                                          .AddConfigServices()
                                          .AddConfigMigrators()
                                          .AddMemoryCache()
                                          // music
                                          .AddMusic()
                                          // cache
                                          .AddCache(_creds);
        // admin
//#if GLOBAL_WIZBOT
        //svcs.AddSingleton<ILogCommandService, DummyLogCommandService>();
//#endif

        svcs.AddHttpClient();
        svcs.AddHttpClient("memelist")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false
            });
        
        svcs.AddHttpClient("google:search")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });

        if (Environment.GetEnvironmentVariable("WIZBOT_IS_COORDINATED") != "1")
            svcs.AddSingleton<ICoordinator, SingleProcessCoordinator>();
        else
        {
            svcs.AddSingleton<RemoteGrpcCoordinator>()
                .AddSingleton<ICoordinator>(x => x.GetRequiredService<RemoteGrpcCoordinator>())
                .AddSingleton<IReadyExecutor>(x => x.GetRequiredService<RemoteGrpcCoordinator>());
        }

        svcs.Scan(scan => scan.FromAssemblyOf<IReadyExecutor>()
                              .AddClasses(classes => classes.AssignableToAny(
                                                                // services
                                                                typeof(INService),

                                                                // behaviours
                                                                typeof(IExecOnMessage),
                                                                typeof(IInputTransformer),
                                                                typeof(IExecPreCommand),
                                                                typeof(IExecPostCommand),
                                                                typeof(IExecNoCommand))
                                                            .WithoutAttribute<DontAddToIocContainerAttribute>()
#if GLOBAL_WIZBOT
                                                            .WithoutAttribute<NoPublicBotAttribute>()
#endif
                              )
                              .AsSelfWithInterfaces()
                              .WithSingletonLifetime());

        //initialize Services
        Services = svcs.BuildServiceProvider();
        Services.GetRequiredService<IBehaviorHandler>().Initialize();
        Services.GetRequiredService<CurrencyRewardService>();

        if (Client.ShardId == 0)
            ApplyConfigMigrations();

        _ = LoadTypeReaders(typeof(Bot).Assembly);

        sw.Stop();
        Log.Information( "All services loaded in {ServiceLoadTime:F2}s", sw.Elapsed.TotalSeconds);
    }

    private void ApplyConfigMigrations()
    {
        // execute all migrators
        var migrators = Services.GetServices<IConfigMigrator>();
        foreach (var migrator in migrators)
            migrator.EnsureMigrated();
    }

    private IEnumerable<object> LoadTypeReaders(Assembly assembly)
    {
        Type[] allTypes;
        try
        {
            allTypes = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            Log.Warning(ex.LoaderExceptions[0], "Error getting types");
            return Enumerable.Empty<object>();
        }

        var filteredTypes = allTypes.Where(x => x.IsSubclassOf(typeof(TypeReader))
                                                && x.BaseType?.GetGenericArguments().Length > 0
                                                && !x.IsAbstract);

        var toReturn = new List<object>();
        foreach (var ft in filteredTypes)
        {
            var x = (TypeReader)ActivatorUtilities.CreateInstance(Services, ft);
            var baseType = ft.BaseType;
            if (baseType is null)
                continue;
            var typeArgs = baseType.GetGenericArguments();
            _commandService.AddTypeReader(typeArgs[0], x);
            toReturn.Add(x);
        }

        return toReturn;
    }

    private async Task LoginAsync(string token)
    {
        var clientReady = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task SetClientReady()
        {
            clientReady.TrySetResult(true);
            try
            {
                foreach (var chan in await Client.GetDMChannelsAsync())
                    await chan.CloseAsync();
            }
            catch
            {
                // ignored
            }
        }

        //connect
        Log.Information("Shard {ShardId} logging in ...", Client.ShardId);
        try
        {
            Client.Ready += SetClientReady;

            await Client.LoginAsync(TokenType.Bot, token);
            await Client.StartAsync();
        }
        catch (HttpException ex)
        {
            LoginErrorHandler.Handle(ex);
            Helpers.ReadErrorAndExit(3);
        }
        catch (Exception ex)
        {
            LoginErrorHandler.Handle(ex);
            Helpers.ReadErrorAndExit(4);
        }
        
        await clientReady.Task.ConfigureAwait(false);
        Client.Ready -= SetClientReady;
        
        Client.JoinedGuild += Client_JoinedGuild;
        Client.LeftGuild += Client_LeftGuild;

        // _ = Client.SetStatusAsync(UserStatus.Online);
        Log.Information("Shard {ShardId} logged in", Client.ShardId);
    }

    private Task Client_LeftGuild(SocketGuild arg)
    {
        Log.Information("Left server: {GuildName} [{GuildId}]", arg?.Name, arg?.Id);
        return Task.CompletedTask;
    }

    private Task Client_JoinedGuild(SocketGuild arg)
    {
        Log.Information("Joined server: {GuildName} [{GuildId}]", arg.Name, arg.Id);
        _ = Task.Run(async () =>
        {
            GuildConfig gc;
            await using (var uow = _db.GetDbContext())
            {
                gc = uow.GuildConfigsForId(arg.Id, null);
            }

            await JoinedGuild.Invoke(gc);
        });
        return Task.CompletedTask;
    }

    public async Task RunAsync()
    {
        if (ShardId == 0)
            await _db.SetupAsync();

        var sw = Stopwatch.StartNew();

        await LoginAsync(_creds.Token);

        Mention = Client.CurrentUser.Mention;
        Log.Information("Shard {ShardId} loading services...", Client.ShardId);
        try
        {
            AddServices();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error adding services");
            Helpers.ReadErrorAndExit(9);
        }

        sw.Stop();
        Log.Information("Shard {ShardId} connected in {Elapsed:F2}s", Client.ShardId, sw.Elapsed.TotalSeconds);
        var commandHandler = Services.GetRequiredService<CommandHandler>();

        // start handling messages received in commandhandler
        await commandHandler.StartHandling();

        await _commandService.AddModulesAsync(typeof(Bot).Assembly, Services);
        // await _interactionService.AddModulesAsync(typeof(Bot).Assembly, Services);
        IsReady = true;
        _ = Task.Run(ExecuteReadySubscriptions);
        Log.Information("Shard {ShardId} ready", Client.ShardId);
    }

    private Task ExecuteReadySubscriptions()
    {
        var readyExecutors = Services.GetServices<IReadyExecutor>();
        var tasks = readyExecutors.Select(async toExec =>
        {
            try
            {
                await toExec.OnReadyAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    "Failed running OnReadyAsync method on {Type} type: {Message}",
                    toExec.GetType().Name,
                    ex.Message);
            }
        });

        return tasks.WhenAll();
    }

    private Task Client_Log(LogMessage arg)
    {
        if (arg.Message?.Contains("unknown dispatch", StringComparison.InvariantCultureIgnoreCase) ?? false)
            return Task.CompletedTask;

        if (arg.Exception is { InnerException: WebSocketClosedException { CloseCode: 4014 } })
        {
            Log.Error(@"
Login failed.

*** Please enable privileged intents ***

Certain WizBot features require Discord's privileged gateway intents.
These include greeting and goodbye messages, as well as creating the Owner message channels for DM forwarding.

How to enable privileged intents:
1. Head over to the Discord Developer Portal https://discord.com/developers/applications/
2. Select your Application.
3. Click on `Bot` in the left side navigation panel, and scroll down to the intents section.
4. Enable all intents.
5. Restart your bot.

Read this only if your bot is in 100 or more servers:

You'll need to apply to use the intents with Discord, but for small selfhosts, all that is required is enabling the intents in the developer portal.
Yes, this is a new thing from Discord, as of October 2020. No, there's nothing we can do about it. Yes, we're aware it worked before.
While waiting for your bot to be accepted, you can change the 'usePrivilegedIntents' inside your creds.yml to 'false', although this will break many of the wizbot's features");
            return Task.CompletedTask;
        }
#if GLOBAL_WIZBOT || DEBUG
        if (arg.Exception is not null)
            Log.Warning(arg.Exception, "{ErrorSource} | {ErrorMessage}", arg.Source, arg.Message);
        else
            Log.Warning("{ErrorSource} | {ErrorMessage}", arg.Source, arg.Message);
#endif
        return Task.CompletedTask;
    }

    public async Task RunAndBlockAsync()
    {
        await RunAsync();
        await Task.Delay(-1);
    }
}