using NadekoBot.Medusa;
using Ninject.Modules;
using System.Collections.Immutable;

namespace NadekoBot.Medusa;

public sealed record ResolvedMedusa(
    WeakReference<MedusaAssemblyLoadContext> LoadContext,
    IImmutableList<ModuleInfo> ModuleInfos,
    IImmutableList<SnekInfo> SnekInfos,
    IMedusaStrings Strings,
    Dictionary<Type, TypeReader> TypeReaders,
    IReadOnlyCollection<ICustomBehavior> Execs
)
{
    public required INinjectModule KernelModule { get; set; }
}