using System.Runtime.CompilerServices;

namespace Nadeko.Medusa;

public class MedusaServiceProvider : IServiceProvider
{
    private readonly IServiceProvider _nadekoServices;
    private readonly IServiceProvider _medusaServices;

    public MedusaServiceProvider(IServiceProvider nadekoServices, IServiceProvider medusaServices)
    {
        _nadekoServices = nadekoServices;
        _medusaServices = medusaServices;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public object? GetService(Type serviceType)
    {
        if (!serviceType.Assembly.IsCollectible)
            return _nadekoServices.GetService(serviceType);

        return _medusaServices.GetService(serviceType);
    }
}