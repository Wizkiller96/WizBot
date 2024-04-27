using System.Reflection;
using Ninject;
using Ninject.Activation;
using Ninject.Activation.Caching;
using Ninject.Modules;
using Ninject.Planning;
using System.Text.Json;

namespace NadekoBot.Medusa;

public sealed class MedusaNinjectModule : NinjectModule
{
    public override string Name { get; }
    private volatile bool isLoaded = false;
    private readonly Dictionary<Type, Type[]> _types;

    public MedusaNinjectModule(Assembly assembly, string name)
    {
        Name = name;
        _types = assembly.GetExportedTypes()
            .Where(t => t.IsClass)
            .Where(t => t.GetCustomAttribute<svcAttribute>() is not null)
            .ToDictionary(x => x,
                type => type.GetInterfaces().ToArray());
    }

    public override void Load()
    {
        if (isLoaded)
            return;

        foreach (var (type, data) in _types)
        {
            var attribute = type.GetCustomAttribute<svcAttribute>()!;
            var scope = GetScope(attribute.Lifetime);

            Bind(type)
                .ToSelf()
                .InScope(scope);
            
            foreach (var inter in data)
            {
                Bind(inter)
                    .ToMethod(x => x.Kernel.Get(type))
                    .InScope(scope);
            }
        }

        isLoaded = true;
    }

    private Func<IContext, object?> GetScope(Lifetime lt)
        => _ => lt switch
        {
            Lifetime.Singleton => this,
            Lifetime.Transient => null,
            _ => null,
        };

    public override void Unload()
    {
        if (!isLoaded)
            return;

        var planner = (RemovablePlanner)Kernel!.Components.Get<IPlanner>();
        var cache = Kernel.Components.Get<ICache>();
        foreach (var binding in this.Bindings)
        {
            Kernel.RemoveBinding(binding);
        }

        foreach (var type in _types.SelectMany(x => x.Value).Concat(_types.Keys))
        {
            var binds = Kernel.GetBindings(type);

            if (!binds.Any())
            {
                Unbind(type);
                
                planner.RemovePlan(type);
            }
        }


        Bindings.Clear();

        cache.Clear(this);
        _types.Clear();
        
        // in case the library uses System.Text.Json
        var assembly = typeof(JsonSerializerOptions).Assembly;
        var updateHandlerType = assembly.GetType("System.Text.Json.JsonSerializerOptionsUpdateHandler");
        var clearCacheMethod = updateHandlerType?.GetMethod("ClearCache", BindingFlags.Static | BindingFlags.Public);
        clearCacheMethod?.Invoke(null, new object?[] { null }); 
        
        isLoaded = false;
    }
}