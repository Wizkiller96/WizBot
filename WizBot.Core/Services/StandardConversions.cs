﻿namespace WizBot.Core.Services
{
    public static class StandardConversions
    {
        public static double CelsiusToFahrenheit(double cel)
        {
            return cel * 1.8f + 32;
        }
    }
}