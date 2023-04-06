using Ninject;

namespace NadekoBot.Extensions;

public static class NinjectIKernelExtensions
{
    public static IKernel AddSingleton<TImpl>(this IKernel kernel)
    {
        kernel.Bind<TImpl>().ToSelf().InSingletonScope();
        return kernel;
    }

    public static IKernel AddSingleton<TInterface, TImpl>(this IKernel kernel)
        where TImpl : TInterface
    {
        kernel.Bind<TInterface>().To<TImpl>().InSingletonScope();
        return kernel;
    }

    public static IKernel AddSingleton<TImpl>(this IKernel kernel, TImpl obj)
        => kernel.AddSingleton<TImpl, TImpl>(obj);

    public static IKernel AddSingleton<TInterface, TImpl>(this IKernel kernel, TImpl obj)
        where TImpl : TInterface
    {
        kernel.Bind<TInterface>().ToConstant(obj).InSingletonScope();
        return kernel;
    }

    public static IKernel AddSingleton<TImpl, TInterface>(
        this IKernel kernel,
        Func<Ninject.Activation.IContext, TImpl> factory)
        where TImpl : TInterface
    {
        kernel.Bind<TInterface>().ToMethod(factory).InSingletonScope();
        return kernel;
    }

    public static IKernel AddSingleton<TImpl>(
        this IKernel kernel,
        Func<Ninject.Activation.IContext, TImpl> factory)
        => kernel.AddSingleton<TImpl, TImpl>(factory);
}