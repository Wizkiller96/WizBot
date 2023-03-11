using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NadekoBot.Modules.Music;
using NadekoBot.Modules.Music.Resolvers;
using NadekoBot.Modules.Music.Services;
using Ninject;
using Ninject.Extensions.Conventions;
using Ninject.Extensions.Conventions.Syntax;
using StackExchange.Redis;
using System.Net;
using System.Reflection;
using NadekoBot.Common.ModuleBehaviors;
using Ninject.Infrastructure.Language;

namespace NadekoBot.Extensions;

public static class ServiceCollectionExtensions
{
    public static IKernel AddBotStringsServices(this IKernel kernel, BotCacheImplemenation botCache)
    {
        if (botCache == BotCacheImplemenation.Memory)
        {
            kernel.Bind<IStringsSource>().To<LocalFileStringsSource>().InSingletonScope();
            kernel.Bind<IBotStringsProvider>().To<MemoryBotStringsProvider>().InSingletonScope();
            kernel.Bind<IBotStrings>().To<BotStrings>().InSingletonScope();
        }
        else
        {
            kernel.Bind<IStringsSource>().To<LocalFileStringsSource>().InSingletonScope();
            kernel.Bind<IBotStringsProvider>().To<RedisBotStringsProvider>().InSingletonScope();
            kernel.Bind<IBotStrings>().To<BotStrings>().InSingletonScope();
        }

        return kernel;
    }

    public static IKernel AddConfigServices(this IKernel kernel)
    {
        kernel.Bind(x =>
        {
            var configs = x.FromThisAssembly()
                           .SelectAllClasses()
                           .Where(f => f.IsAssignableToGenericType(typeof(ConfigServiceBase<>)));

            configs.BindToSelfWithInterfaces()
                   .Configure(c => c.InSingletonScope());
        });

        return kernel;
    }

    public static IKernel AddConfigMigrators(this IKernel kernel)
        => kernel.AddSealedSubclassesOf(typeof(IConfigMigrator));

    public static IKernel AddMusic(this IKernel kernel)
    {
        kernel.Bind<IMusicService, IPlaceholderProvider>()
              .To<MusicService>()
              .InSingletonScope();

        kernel.Bind<ITrackResolveProvider>().To<TrackResolveProvider>().InSingletonScope();
        kernel.Bind<IYoutubeResolver>().To<YtdlYoutubeResolver>().InSingletonScope();
        kernel.Bind<ISoundcloudResolver>().To<SoundcloudResolver>().InSingletonScope();
        kernel.Bind<ILocalTrackResolver>().To<LocalTrackResolver>().InSingletonScope();
        kernel.Bind<IRadioResolver>().To<RadioResolver>().InSingletonScope();
        kernel.Bind<ITrackCacher>().To<TrackCacher>().InSingletonScope();
        // kernel.Bind<YtLoader>().ToSelf().InSingletonScope();

        return kernel;
    }

    public static IKernel AddSealedSubclassesOf(this IKernel kernel, Type baseType)
    {
        kernel.Bind(x =>
        {
            var classes = x.FromThisAssembly()
                           .SelectAllClasses()
                           .Where(c => c.IsPublic && c.IsNested && baseType.IsAssignableFrom(baseType));

            classes.BindToSelfWithInterfaces().Configure(x => x.InSingletonScope());
        });

        return kernel;
    }

    public static IKernel AddCache(this IKernel kernel, IBotCredentials creds)
    {
        if (creds.BotCache == BotCacheImplemenation.Redis)
        {
            var conf = ConfigurationOptions.Parse(creds.RedisOptions);
            kernel.Bind<ConnectionMultiplexer>().ToConstant(ConnectionMultiplexer.Connect(conf)).InSingletonScope();
            kernel.Bind<IBotCache>().To<RedisBotCache>().InSingletonScope();
            kernel.Bind<IPubSub>().To<RedisPubSub>().InSingletonScope();
        }
        else
        {
            kernel.Bind<IBotCache>().To<MemoryBotCache>().InSingletonScope();
            kernel.Bind<IPubSub>().To<EventPubSub>().InSingletonScope();
        }

        return kernel
            .AddBotStringsServices(creds.BotCache);
    }

    public static IKernel AddHttpClients(this IKernel kernel)
    {
        IServiceCollection svcs = new ServiceCollection();
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

        var prov = svcs.BuildServiceProvider();
        kernel.Bind<IHttpClientFactory>().ToMethod(_ => prov.GetRequiredService<IHttpClientFactory>());
        kernel.Bind<HttpClient>().ToMethod(_ => prov.GetRequiredService<HttpClient>());

        return kernel;
    }

    public static IConfigureSyntax BindToSelfWithInterfaces(this IJoinExcludeIncludeBindSyntax matcher)
        => matcher.BindSelection((type, types) => types.Append(type));

    public static IKernel AddLifetimeServices(this IKernel kernel)
    {
        kernel.Bind(scan =>
        {
            scan.FromThisAssembly()
                .SelectAllClasses()
                .Where(c => (c.IsAssignableTo(typeof(INService))
                             || c.IsAssignableTo(typeof(IExecOnMessage))
                             || c.IsAssignableTo(typeof(IInputTransformer))
                             || c.IsAssignableTo(typeof(IExecPreCommand))
                             || c.IsAssignableTo(typeof(IExecPostCommand))
                             || c.IsAssignableTo(typeof(IExecNoCommand)))
                            && !c.HasAttribute<DIIgnoreAttribute>()
#if GLOBAL_NADEK
                            && !c.HasAttribute<NoPublicBotAttribute>()
#endif
                )
                .BindToSelfWithInterfaces()
                .Configure(c => c.InSingletonScope());
        });

        return kernel;
    }
}