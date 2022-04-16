using Microsoft.Extensions.DependencyInjection;
using NadekoBot.Modules.Music;
using NadekoBot.Modules.Music.Resolvers;
using NadekoBot.Modules.Music.Services;
using StackExchange.Redis;
using System.Reflection;

namespace NadekoBot.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBotStringsServices(this IServiceCollection services, int totalShards)
        => totalShards <= 1
            ? services.AddSingleton<IStringsSource, LocalFileStringsSource>()
                      .AddSingleton<IBotStringsProvider, LocalBotStringsProvider>()
                      .AddSingleton<IBotStrings, BotStrings>()
            : services.AddSingleton<IStringsSource, LocalFileStringsSource>()
                      .AddSingleton<IBotStringsProvider, RedisBotStringsProvider>()
                      .AddSingleton<IBotStrings, BotStrings>();

    public static IServiceCollection AddConfigServices(this IServiceCollection services)
    {
        services.Scan(x => x.FromCallingAssembly()
                            .AddClasses(f => f.AssignableTo(typeof(ConfigServiceBase<>)))
                            .AsSelfWithInterfaces());
        
        // var baseType = typeof(ConfigServiceBase<>);
        //
        // foreach (var type in Assembly.GetCallingAssembly().ExportedTypes.Where(x => x.IsSealed))
        // {
        //     if (type.BaseType?.IsGenericType == true && type.BaseType.GetGenericTypeDefinition() == baseType)
        //     {
        //         services.AddSingleton(type);
        //         services.AddSingleton(x => (IConfigService)x.GetRequiredService(type));
        //     }
        // }

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
                   .AddSingleton<ITrackCacher, RedisTrackCacher>()
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

    public static IServiceCollection AddRedis(this IServiceCollection services, string redisOptions)
    {
        var conf = ConfigurationOptions.Parse(redisOptions);
        services.AddSingleton(ConnectionMultiplexer.Connect(conf));
        return services;
    }
}