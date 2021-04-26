namespace WizBot.Core.Services
{
    public static class BotStringsHelper
    {
        public static string GetLocaleName(string fileName)
        {
            var dotIndex = fileName.IndexOf('.') + 1;
            var secondDotINdex = fileName.LastIndexOf('.');
            return fileName.Substring(dotIndex, secondDotINdex - dotIndex);
        }
    }
}