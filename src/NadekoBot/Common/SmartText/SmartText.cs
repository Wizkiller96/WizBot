using Newtonsoft.Json;

namespace NadekoBot
{
    public abstract class SmartText
    {
        public bool IsEmbed => this is SmartEmbedText;
        public bool IsPlainText => this is SmartPlainText;

        public static implicit operator SmartText(string input)
            => new SmartPlainText(input);
        
        public static SmartText CreateFrom(string input)
        {
            if (string.IsNullOrWhiteSpace(input) || !input.TrimStart().StartsWith("{"))
            {
                return new SmartPlainText(input);
            }

            try
            {
                var smartEmbedText = JsonConvert.DeserializeObject<SmartEmbedText>(input);

                smartEmbedText.NormalizeFields();

                if (!smartEmbedText.IsValid)
                {
                    return new SmartPlainText(input);
                }

                return smartEmbedText;
            }
            catch
            {
                return new SmartPlainText(input);
            }
        }
    }
}