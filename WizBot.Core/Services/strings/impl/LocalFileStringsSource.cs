using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace WizBot.Core.Services
{
    /// <summary>
    /// Loads strings from the local default filepath <see cref="StringsPath"/>
    /// </summary>
    public class LocalFileStringsSource : IStringsSource
    {
        private const string StringsPath = "config/strings/responses";

        public Dictionary<string, Dictionary<string, string>> GetResponseStrings()
        {
            var outputDict = new Dictionary<string, Dictionary<string, string>>();
            foreach (var file in Directory.GetFiles(StringsPath))
            {
                var langDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(file));
                var localeName = GetLocaleName(file);
                outputDict[localeName] = langDict;
            }

            return outputDict;
        }

        private static string GetLocaleName(string fileName)
        {
            var dotIndex = fileName.IndexOf('.') + 1;
            var secondDotIndex = fileName.LastIndexOf('.');
            return fileName.Substring(dotIndex, secondDotIndex - dotIndex);
        }
    }
}