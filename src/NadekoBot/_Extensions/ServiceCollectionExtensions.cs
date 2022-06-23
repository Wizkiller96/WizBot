using Microsoft.Extensions.DependencyInjection;
using NadekoBot.Modules.Music;
using NadekoBot.Modules.Music.Resolvers;
using NadekoBot.Modules.Music.Services;
using StackExchange.Redis;
using System.Reflection;

namespace NadekoBot.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBotStringsServices(this IServiceCollection services, BotCacheImplemenation botCache)
        => botCache == BotCacheImplemenation.Memory
            ? services.AddSingleton<IStringsSource, LocalFileStringsSource>()
                      .AddSingleton<IBotStringsProvider, MemoryBotStringsProvider>()
                      .AddSingleton<IBotStrings, BotStrings>()
            : services.AddSingleton<IStringsSource, LocalFileStringsSource>()
                      .AddSingleton<IBotStringsProvider, RedisBotStringsProvider>()
                      .AddSingleton<IBotStrings, BotStrings>();

    public static IServiceCollection AddConfigServices(this IServiceCollection services)
    {
        services.Scan(x => x.FromCallingAssembly()
                            .AddClasses(f => f.AssignableTo(typeof(ConfigServiceBase<>)))
                            .AsSelfWithInterfaces());

        return services;
    }

    public static IServiceCollection AddConfigMigrators(this IServiceCollection services)
        => services.AddSealedSubclassesOf(typeof(IConfigMigrator));

    public static IServiceCollection AddMusic(this IServiceCollection services)
        => services.AddSingleton<IMusicService, MusicService>()
                   .AddSingleton<ITrackResolveProvider, TrackResolveProvider>()
                   .AddSingleton<IYoutubeResolver, YtdlYoutubeResolver>()
                   .AddSingleton<ISoundcloudResolver, SoundcloudResolver>()
                   .AddSingleton<ILocalTrackResolver, LocalTrackResolver>()
                   .AddSingleton<IRadioResolver, RadioResolver>()
                   .AddSingleton<ITrackCacher, TrackCacher>()
                   .AddSingleton<YtLoader>()
                   .AddSingleton<IPlaceholderProvider>(svc => svc.GetRequiredService<IMusicService>());

    // consider using scrutor, because slightly different versions
    // of this might be needed in several different places
    public static IServiceCollection AddSealedSubclassesOf(this IServiceCollection services, Type baseType)
    {
        var subTypes = Assembly.GetCallingAssembly()
                               .ExportedTypes.Where(type => type.IsSealed && baseType.IsAssignableFrom(type));

        foreach (var subType in subTypes)
            services.AddSingleton(baseType, subType);

        return services;
    }

    public static IServiceCollection AddCache(this IServiceCollection services, IBotCredentials creds)
    {
        if (creds.BotCache == BotCacheImplemenation.Redis)
        {
            var conf = ConfigurationOptions.Parse(creds.RedisOptions);
            services.AddSingleton(ConnectionMultiplexer.Connect(conf))
                    .AddSingleton<IBotCache, RedisBotCache>()
                    .AddSingleton<IPubSub, RedisPubSub>();
        }
        else
        {
            services.AddSingleton<IBotCache, MemoryBotCache>()
                    .AddSingleton<IPubSub, EventPubSub>();

        }

        return services
            .AddBotStringsServices(creds.BotCache);
    }
}