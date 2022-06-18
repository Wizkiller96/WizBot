#nullable disable
namespace WizBot.Common;

/// <summary>
/// Classed marked with this attribute will not be added to the service provider 
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class DontAddToIocContainerAttribute : Attribute
{
    
}