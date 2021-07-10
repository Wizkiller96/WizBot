using Newtonsoft.Json;

namespace NadekoBot
{
    // todo 3.3 check if saving embeds in db has IsEmbed field, to prevent rechecking and generating exceptions on every use
    public abstract class SmartText
    {
        public bool IsEmbed => this is SmartEmbedText;
        public bool IsPlainText => this is SmartPlainText;

        public static SmartText CreateFrom(string input)
        {
            if (string.IsNullOrWhiteSpace(input) || !input.Trim().StartsWith("{"))
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