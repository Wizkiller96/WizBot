using System.Collections.Immutable;

namespace WizBot.Medusa;

public sealed record ResolvedMedusa(
    WeakReference<MedusaAssemblyLoadContext> LoadContext,
    IImmutableList<ModuleInfo> ModuleInfos,
    IImmutableList<SnekInfo> SnekInfos,
    IMedusaStrings Strings,
    Dictionary<Type, TypeReader> TypeReaders,
    IReadOnlyCollection<ICustomBehavior> Execs)
{
    public IServiceProvider Services { get; set; } = null!;
}