#nullable disable
namespace WizBot.Services;

public static class StandardConversions
{
    public static double CelsiusToFahrenheit(double cel)
        => (cel * 1.8f) + 32;
}