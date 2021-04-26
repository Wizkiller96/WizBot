using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace WizBot.Core.Services
{
    public class LocalBotStringsProvider : IBotStringsProvider
    {
        private const string StringsPath = @"_strings/responses";
        private Dictionary<string, IReadOnlyDictionary<string, string>> responseStrings;

        public LocalBotStringsProvider()
        {
            Reload();
        }

        public string GetText(string langName, string key)
        {
            if (responseStrings.TryGetValue(langName, out var langStrings)
                && langStrings.TryGetValue(key, out var text))
            {
                return text;
            }

            return null;
        }

        public void Reload()
        {
            var newResponseStrings = new Dictionary<string, IReadOnlyDictionary<string, string>>(); // lang:(name:value)
            foreach (var file in Directory.GetFiles(StringsPath))
            {
                var langDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(file));
                newResponseStrings.Add(BotStringsHelper.GetLocaleName(file).ToUpperInvariant(), langDict);
            }

            responseStrings = newResponseStrings;
        }
    }
}
