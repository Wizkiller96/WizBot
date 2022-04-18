using System.Runtime.CompilerServices;

namespace WizBot.Medusa;

public class MedusaServiceProvider : IServiceProvider
{
    private readonly IServiceProvider _wizbotServices;
    private readonly IServiceProvider _medusaServices;

    public MedusaServiceProvider(IServiceProvider wizbotServices, IServiceProvider medusaServices)
    {
        _wizbotServices = wizbotServices;
        _medusaServices = medusaServices;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public object? GetService(Type serviceType)
    {
        if (!serviceType.Assembly.IsCollectible)
            return _wizbotServices.GetService(serviceType);

        return _medusaServices.GetService(serviceType);
    }
}