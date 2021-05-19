﻿using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using WizBot.Core.Services;
using WizBot.Modules.Administration.Services;

namespace WizBot.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBotStringsServices(this IServiceCollection services)
            => services
                .AddSingleton<IStringsSource, LocalFileStringsSource>()
                .AddSingleton<IBotStringsProvider, LocalBotStringsProvider>()
                .AddSingleton<IBotStrings, BotStrings>();

        public static IServiceCollection AddConfigServices(this IServiceCollection services)
        {
            var baseType = typeof(ConfigServiceBase<>);

            foreach (var type in Assembly.GetCallingAssembly().ExportedTypes.Where(x => x.IsSealed))
            {
                if (type.BaseType?.IsGenericType == true && type.BaseType.GetGenericTypeDefinition() == baseType)
                {
                    services.AddSingleton(type);
                    services.AddSingleton(x => (IConfigService)x.GetRequiredService(type));
                }
            }

            return services;
        }

        public static IServiceCollection AddConfigMigrators(this IServiceCollection services)
            => services.AddSealedSubclassesOf(typeof(IConfigMigrator));
        
        // consider using scrutor, because slightly different versions
        // of this might be needed in several different places
        public static IServiceCollection AddSealedSubclassesOf(this IServiceCollection services, Type baseType)
        {
            var subTypes = Assembly.GetCallingAssembly()
                .ExportedTypes
                .Where(type => type.IsSealed && baseType.IsAssignableFrom(type));

            foreach (var subType in subTypes)
            {
                services.AddSingleton(baseType, subType);
            }

            return services;
        }
    }
}