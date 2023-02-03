using Ninject.Modules;
using Ninject.Extensions.Conventions;
using System.Reflection;

namespace Nadeko.Medusa;

public sealed class MedusaIoCKernelModule : NinjectModule
{
    private Assembly _a;
    public override string Name { get; }

    public MedusaIoCKernelModule(string name, Assembly a)
    {
        Name = name;
        _a = a;
    }

    public override void Load()
    {
        // todo cehck for duplicate registrations with ninject.extensions.convention
        Kernel.Bind(conf =>
        {
            var transient = conf.From(_a)
                                .SelectAllClasses()
                                .WithAttribute<svcAttribute>(x => x.Lifetime == Lifetime.Transient);
            
            transient.BindAllInterfaces().Configure(x => x.InTransientScope());
            transient.BindToSelf().Configure(x => x.InTransientScope());

            var singleton = conf.From(_a)
                                .SelectAllClasses()
                                .WithAttribute<svcAttribute>(x => x.Lifetime == Lifetime.Singleton);
            
            singleton.BindAllInterfaces().Configure(x => x.InSingletonScope());
            singleton.BindToSelf().Configure(x => x.InSingletonScope());
        });
    }

    public override void Unload()
    {
        // todo implement unload
        // Kernel.Unbind();
    }

    public override void Dispose(bool disposing)
    {
        _a = null!;
        base.Dispose(disposing);
    }
}