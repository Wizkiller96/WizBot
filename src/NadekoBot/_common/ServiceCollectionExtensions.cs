using DryIoc;
using Microsoft.Extensions.DependencyInjection;
using NadekoBot.Modules.Music;
using NadekoBot.Modules.Music.Resolvers;
using NadekoBot.Modules.Music.Services;
using Ninject.Extensions.Conventions.Syntax;
using StackExchange.Redis;
using System.Net;
using System.Reflection;
using NadekoBot.Common.ModuleBehaviors;
using Ninject.Infrastructure.Language;

namespace NadekoBot.Extensions;

public static class ServiceCollectionExtensions
{
    public static IContainer AddBotStringsServices(this IContainer svcs, BotCacheImplemenation botCache)
    {
        if (botCache == BotCacheImplemenation.Memory)
        {
            svcs.AddSingleton<IStringsSource, LocalFileStringsSource>();
            svcs.AddSingleton<IBotStringsProvider, MemoryBotStringsProvider>();
        }
        else
        {
            svcs.AddSingleton<IStringsSource, LocalFileStringsSource>();
            svcs.AddSingleton<IBotStringsProvider, RedisBotStringsProvider>();
        }

        svcs.AddSingleton<IBotStrings, BotStrings>();

        return svcs;
    }

    public static IContainer AddConfigServices(this IContainer kernel, Assembly a)
    {
        // kernel.RegisterMany([typeof(ConfigServiceBase<>)]);
        
        foreach (var type in a.GetTypes()
                           .Where(x => !x.IsAbstract && x.IsAssignableToGenericType(typeof(ConfigServiceBase<>))))
        {
            kernel.RegisterMany([type],
                getServiceTypes: type => type.GetImplementedTypes(ReflectionTools.AsImplementedType.SourceType),
                getImplFactory: type => ReflectionFactory.Of(type, Reuse.Singleton));
        }

        //
        // kernel.Bind(x =>
        // {
        //     var configs = x.From(a)
        //                    .SelectAllClasses()
        //                    .Where(f => f.IsAssignableToGenericType(typeof(ConfigServiceBase<>)));
        //
        //     configs.BindToSelfWithInterfaces()
        //            .Configure(c => c.InSingletonScope());
        // });

        return kernel;
    }

    public static IContainer AddConfigMigrators(this IContainer kernel, Assembly a)
        => kernel.AddSealedSubclassesOf(typeof(IConfigMigrator), a);

    public static IContainer AddMusic(this IContainer kernel)
    {
        kernel.RegisterMany<MusicService>(Reuse.Singleton);

        kernel.AddSingleton<ITrackResolveProvider, TrackResolveProvider>();
        kernel.AddSingleton<IYoutubeResolver, YtdlYoutubeResolver>();
        kernel.AddSingleton<ILocalTrackResolver, LocalTrackResolver>();
        kernel.AddSingleton<IRadioResolver, RadioResolver>();
        kernel.AddSingleton<ITrackCacher, TrackCacher>();

        return kernel;
    }

    public static IContainer AddSealedSubclassesOf(this IContainer cont, Type baseType, Assembly a)
    {
        var classes = a.GetExportedTypes()
                       .Where(x => x.IsClass && !x.IsAbstract && x.IsPublic)
                       .Where(x => x.IsNested && baseType.IsAssignableFrom(x));

        foreach (var c in classes)
        {
            cont.RegisterMany([c], Reuse.Singleton);
            // var inters = c.GetInterfaces();

            // cont.RegisterMany(inters, c);
        }

        return cont;
    }

    public static IContainer AddCache(this IContainer cont, IBotCredentials creds)
    {
        if (creds.BotCache == BotCacheImplemenation.Redis)
        {
            var conf = ConfigurationOptions.Parse(creds.RedisOptions);
            cont.AddSingleton<ConnectionMultiplexer>(ConnectionMultiplexer.Connect(conf));
            cont.AddSingleton<IBotCache, RedisBotCache>();
            cont.AddSingleton<IPubSub, RedisPubSub>();
        }
        else
        {
            cont.AddSingleton<IBotCache, MemoryBotCache>();
            cont.AddSingleton<IPubSub, EventPubSub>();
        }

        return cont
            .AddBotStringsServices(creds.BotCache);
    }

    public static IContainer AddHttpClients(this IContainer kernel)
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
        kernel.RegisterDelegate<IHttpClientFactory>(_ => prov.GetRequiredService<IHttpClientFactory>());
        kernel.RegisterDelegate<HttpClient>(_ => prov.GetRequiredService<HttpClient>());

        return kernel;
    }

    public static IConfigureSyntax BindToSelfWithInterfaces(this IJoinExcludeIncludeBindSyntax matcher)
        => matcher.BindSelection((type, types) => types.Append(type));

    public static IContainer AddLifetimeServices(this IContainer kernel, Assembly a)
    {
        Type[] types =
        [
            typeof(IExecOnMessage),
            typeof(IExecPreCommand),
            typeof(IExecPostCommand),
            typeof(IExecNoCommand),
            typeof(IInputTransformer),
            typeof(INService)
        ];

        foreach (var svc in a.GetTypes()
                           .Where(type => type.IsClass && types.Any(t => type.IsAssignableTo(t)) && !type.HasAttribute<DIIgnoreAttribute>()))
        {
            kernel.RegisterMany([svc],
                getServiceTypes: type => type.GetImplementedTypes(ReflectionTools.AsImplementedType.SourceType),
                getImplFactory: type => ReflectionFactory.Of(type, Reuse.Singleton));
        }
//
//         kernel.RegisterMany(
//             [a],
// #if GLOBAL_NADEKO
//                             && !c.HasAttribute<NoPublicBotAttribute>()
// #endif
//         ),
//         reuse:
//         Reuse.Singleton
//             );


        // todo maybe self is missing
        // todo maybe attribute doesn't work

        return kernel;
    }
}